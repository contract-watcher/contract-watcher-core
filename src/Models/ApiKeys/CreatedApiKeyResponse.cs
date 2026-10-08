using ContractWatcher.Core.Data.Domain;

namespace ContractWatcher.Core.Models.ApiKeys;

/// <summary>
/// Только что выпущенный ключ. Полный ключ (Key) отдаётся один раз — в этом ответе, потом его не получить
/// </summary>
public record CreatedApiKeyResponse(Guid Id, string Name, string Key, string Mask, DateTime CreatedAt)
{
    public static CreatedApiKeyResponse From(ApiKey apiKey, string key) =>
        new(apiKey.Id, apiKey.Name, key, ApiKeyResponse.MaskPrefix + apiKey.KeyHint, apiKey.CreatedAt);
}
