using System.Security.Claims;
using System.Text.Encodings.Web;
using ContractWatcher.Core.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContractWatcher.Core.Services.Auth;

/// <summary>
/// Аутентификация SDK по ключу интеграции из заголовка X-Api-Key.
/// В контекст запроса попадают ID интеграции и ID ключа
/// </summary>
public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    DataContext db,
    TimeProvider timeProvider
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";
    public const string IntegrationIdClaim = "integration_id";
    public const string ApiKeyIdClaim = "api_key_id";

    // LastUsedAt нужен для статуса «SDK подключён», минутной точности хватает — не пишем в базу на каждый запрос
    private static readonly TimeSpan LastUsedUpdateInterval = TimeSpan.FromMinutes(5);

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Без заголовка схема просто не срабатывает, ответ 401 даст авторизация
        if (!Request.Headers.TryGetValue(HeaderName, out var header))
            return AuthenticateResult.NoResult();

        // Сам ключ не попадает ни в сообщения об ошибках, ни в логи
        var key = header.ToString();
        if (string.IsNullOrWhiteSpace(key))
            return AuthenticateResult.Fail("API key is empty");

        var keyHash = TokenService.HashToken(key);
        var apiKey = await db
            .ApiKeys.AsNoTracking()
            .Where(x => x.KeyHash == keyHash)
            .Select(x => new
            {
                x.Id,
                x.IntegrationId,
                x.RevokedAt,
                x.LastUsedAt,
            })
            .SingleOrDefaultAsync(Context.RequestAborted);

        if (apiKey is null || apiKey.RevokedAt is not null)
            return AuthenticateResult.Fail("API key is invalid or revoked");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (apiKey.LastUsedAt is null || apiKey.LastUsedAt <= now - LastUsedUpdateInterval)
            await UpdateLastUsedAtAsync(apiKey.Id, now);

        var identity = new ClaimsIdentity(
            [
                new Claim(IntegrationIdClaim, apiKey.IntegrationId.ToString()),
                new Claim(ApiKeyIdClaim, apiKey.Id.ToString()),
            ],
            SchemeName
        );

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    // Условие повторяется в UPDATE: из нескольких одновременных запросов запишет только первый.
    // UpdatedAt не трогаем — использование ключа не меняет сам ключ
    private Task<int> UpdateLastUsedAtAsync(Guid apiKeyId, DateTime now)
    {
        var threshold = now - LastUsedUpdateInterval;

        return db
            .ApiKeys.Where(x => x.Id == apiKeyId && (x.LastUsedAt == null || x.LastUsedAt <= threshold))
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.LastUsedAt, now), Context.RequestAborted);
    }
}
