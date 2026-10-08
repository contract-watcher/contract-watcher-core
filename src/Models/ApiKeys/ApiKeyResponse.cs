using ContractWatcher.Core.Data.Domain;

namespace ContractWatcher.Core.Models.ApiKeys;

/// <summary>
/// Ключ интеграции в списке. Сам ключ не отдаётся — только маска
/// </summary>
public record ApiKeyResponse(
    Guid Id,
    string Name,
    string Mask,
    bool IsRevoked,
    DateTime CreatedAt,
    DateTime? LastUsedAt,
    DateTime? RevokedAt
)
{
    public const string MaskPrefix = "••••";

    public static ApiKeyResponse From(ApiKey apiKey) =>
        new(
            apiKey.Id,
            apiKey.Name,
            MaskPrefix + apiKey.KeyHint,
            apiKey.RevokedAt is not null,
            apiKey.CreatedAt,
            apiKey.LastUsedAt,
            apiKey.RevokedAt
        );
}

