using ContractWatcher.Common.Contracts;
using ContractWatcher.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace ContractWatcher.Core.Services.Sdk;

/// <summary>
/// Контракты для SDK. SDK видит только контракты интеграции, к которой относится его ключ
/// </summary>
public class SdkContractService(DataContext db)
{
    /// <summary>
    /// Последняя опубликованная версия контракта. Null — контракта нет в интеграции или ни одна версия не опубликована
    /// </summary>
    public async Task<PublishedContract?> GetPublishedAsync(
        Guid integrationId,
        string slug,
        CancellationToken cancellationToken
    )
    {
        var version = await db
            .ContractVersions.AsNoTracking()
            .Where(x => x.Contract.IntegrationId == integrationId && x.Contract.Slug == slug && x.PublishedAt != null)
            .OrderByDescending(x => x.Version)
            .Select(x => new {x.Contract.Slug ,x.Version, x.Rules  })
            .FirstOrDefaultAsync(cancellationToken);

        if (version is null)
            return null;

        return new PublishedContract { Slug = version.Slug ,ContractVersion = version.Version, Rules = version.Rules ,  };
    }
}
