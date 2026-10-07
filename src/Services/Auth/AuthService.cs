using ContractWatcher.Core.Data;
using ContractWatcher.Core.Data.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ContractWatcher.Core.Services.Auth;

/// <summary>
/// Регистрация, вход, обновление и отзыв токенов
/// </summary>
public class AuthService(
    DataContext db,
    IPasswordHasher<User> passwordHasher,
    TokenService tokenService,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider
)
{
    // Пароль проверяется и для несуществующего email, чтобы по времени ответа нельзя было понять, зарегистрирован ли он
    private static readonly Lazy<string> DummyPasswordHash = new(() =>
        new PasswordHasher<User>().HashPassword(CreateDummyUser(), "dummy-password")
    );

    /// <summary>
    /// Регистрирует пользователя и сразу выдаёт токены. Null — email уже занят
    /// </summary>
    public async Task<AuthTokens?> RegisterAsync(
        string email,
        string password,
        string name,
        CancellationToken cancellationToken
    )
    {
        var normalizedEmail = NormalizeEmail(email);

        if (await db.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
            return null;

        var user = new User
        {
            Email = normalizedEmail,
            Name = name.Trim(),
            PasswordHash = string.Empty,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        db.Users.Add(user);

        var tokens = IssueTokens(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e)
            when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Тот же email успели зарегистрировать параллельным запросом — это тоже «email уже занят»
            return null;
        }

        return tokens;
    }

    /// <summary>
    /// Проверяет email и пароль и выдаёт токены. Null — неверный email или пароль
    /// </summary>
    public async Task<AuthTokens?> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            _ = passwordHasher.VerifyHashedPassword(CreateDummyUser(), DummyPasswordHash.Value, password);
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
            return null;

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = passwordHasher.HashPassword(user, password);

        var tokens = IssueTokens(user);
        await db.SaveChangesAsync(cancellationToken);

        return tokens;
    }

    /// <summary>
    /// Меняет refresh-токен на новую пару токенов. Старый токен отзывается. Null — токен недействителен
    /// </summary>
    public async Task<AuthTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = TokenService.HashToken(refreshToken);
        var stored = await db
            .RefreshTokens.Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (stored is null)
            return null;

        var now = GetUtcNow();

        if (stored.RevokedAt is not null)
        {
            // Отозванный токен предъявили повторно — вероятно, его украли. Завершаем все сессии пользователя
            await RevokeActiveAsync(db.RefreshTokens.Where(x => x.UserId == stored.UserId), now, cancellationToken);
            return null;
        }

        if (stored.ExpiresAt <= now)
            return null;

        // Отзываем атомарно: из двух одновременных запросов с одним токеном пройдёт только один
        var revoked = await RevokeActiveAsync(db.RefreshTokens.Where(x => x.Id == stored.Id), now, cancellationToken);
        if (revoked == 0)
            return null;

        var tokens = IssueTokens(stored.User);
        await db.SaveChangesAsync(cancellationToken);

        return tokens;
    }

    /// <summary>
    /// Отзывает refresh-токен при выходе. Повторный выход или неизвестный токен — не ошибка
    /// </summary>
    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = TokenService.HashToken(refreshToken);
        await RevokeActiveAsync(db.RefreshTokens.Where(x => x.TokenHash == tokenHash), GetUtcNow(), cancellationToken);
    }

    private AuthTokens IssueTokens(User user)
    {
        var (accessToken, accessTokenExpiresAt) = tokenService.CreateAccessToken(user);
        var refreshToken = TokenService.GenerateRefreshToken();
        var refreshTokenExpiresAt = GetUtcNow().AddDays(options.Value.RefreshTokenLifetimeDays);

        db.RefreshTokens.Add(
            new RefreshToken
            {
                User = user,
                TokenHash = TokenService.HashToken(refreshToken),
                ExpiresAt = refreshTokenExpiresAt,
            }
        );

        return new AuthTokens(accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt);
    }

    // ExecuteUpdate идёт мимо TimestampInterceptor, поэтому UpdatedAt ставим сами
    private static Task<int> RevokeActiveAsync(
        IQueryable<RefreshToken> tokens,
        DateTime now,
        CancellationToken cancellationToken
    ) =>
        tokens
            .Where(x => x.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.RevokedAt, now).SetProperty(x => x.UpdatedAt, now),
                cancellationToken
            );

    private DateTime GetUtcNow() => timeProvider.GetUtcNow().UtcDateTime;

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static User CreateDummyUser() =>
        new()
        {
            Email = string.Empty,
            PasswordHash = string.Empty,
            Name = string.Empty,
        };
}
