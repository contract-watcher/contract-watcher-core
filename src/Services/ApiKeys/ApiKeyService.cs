using System.Buffers.Text;
using System.Security.Cryptography;
using ContractWatcher.Core.Data;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.ApiKeys;
using ContractWatcher.Core.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace ContractWatcher.Core.Services.ApiKeys;

/// <summary>
/// Ключи интеграции, с которыми SDK обращается к Core. Доступны только владельцу проекта
/// </summary>
public class ApiKeyService(DataContext db, TimeProvider timeProvider)
{
    private const string KeyPrefix = "cw_";
    private const int KeySizeBytes = 32;
    private const int KeyHintLength = 4;

    /// <summary>
    /// Ключи интеграции. Null — интеграции нет или проект чужой
    /// </summary>
    public async Task<List<ApiKeyResponse>?> GetListAsync(
        Guid userId,
        Guid integrationId,
        CancellationToken cancellationToken
    )
    {
        if (!await IntegrationExistsAsync(userId, integrationId, cancellationToken))
            return null;

        var keys = await db
            .ApiKeys.AsNoTracking()
            .Where(x => x.IntegrationId == integrationId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return keys.ConvertAll(ApiKeyResponse.From);
    }

    /// <summary>
    /// Выпускает ключ. В базу пишутся только хеш и последние символы. Null — интеграции нет или проект чужой
    /// </summary>
    public async Task<CreatedApiKeyResponse?> CreateAsync(
        Guid userId,
        Guid integrationId,
        string name,
        CancellationToken cancellationToken
    )
    {
        if (!await IntegrationExistsAsync(userId, integrationId, cancellationToken))
            return null;

        var key = KeyPrefix + Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(KeySizeBytes));
        var apiKey = new ApiKey
        {
            IntegrationId = integrationId,
            Name = name.Trim(),
            KeyHash = TokenService.HashToken(key),
            KeyHint = key[^KeyHintLength..],
        };
        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedApiKeyResponse.From(apiKey, key);
    }

    /// <summary>
    /// Отзывает ключ. Повторный отзыв не меняет дату. False — ключа нет или проект чужой
    /// </summary>
    public async Task<bool> RevokeAsync(
        Guid userId,
        Guid integrationId,
        Guid keyId,
        CancellationToken cancellationToken
    )
    {
        var apiKey = await db.ApiKeys.SingleOrDefaultAsync(
            x => x.Id == keyId && x.IntegrationId == integrationId && x.Integration.Project.OwnerId == userId,
            cancellationToken
        );
        if (apiKey is null)
            return false;

        if (apiKey.RevokedAt is null)
        {
            apiKey.RevokedAt = timeProvider.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    private Task<bool> IntegrationExistsAsync(Guid userId, Guid integrationId, CancellationToken cancellationToken) =>
        db.Integrations.AnyAsync(x => x.Id == integrationId && x.Project.OwnerId == userId, cancellationToken);
}
