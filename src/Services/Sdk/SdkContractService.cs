using ContractWatcher.Common.Contracts;
using ContractWatcher.Core.Data;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Extensions;
using ContractWatcher.Core.Models.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ContractWatcher.Core.Services.Sdk;

/// <summary>
/// Результат публикации контракта, построенного SDK
/// </summary>
public enum SdkContractPublishStatus
{
    Created,
    Unchanged,
    Conflict,
}

/// <summary>
/// Контракты для SDK. SDK видит только контракты интеграции, к которой относится его ключ
/// </summary>
public class SdkContractService(DataContext db, TimeProvider timeProvider)
{
    /// <summary>
    /// Комментарий к версиям, созданным по сканированию: на фронте их видно в списке версий
    /// </summary>
    public const string ScannedComment = "Создано сканированием SDK";

    private const int PublishAttempts = 2;

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

    /// <summary>
    /// Публикация контракта, который SDK построил по коду. Контракта с таким slug нет — он создаётся с версией 1.
    /// Правила отличаются от последней опубликованной версии — создаётся следующая версия. Новая версия сразу
    /// опубликована. Правила те же — ничего не создаётся: SDK сканирует при каждом запуске приложения.
    /// Возвращает версию, по которой SDK проверяет запросы и отправляет отчёты
    /// </summary>
    public async Task<(SdkContractPublishStatus Status, PublishedContract? Contract)> PublishScannedAsync(
        Guid integrationId,
        ScannedContractRequest request,
        CancellationToken cancellationToken
    )
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await PublishScannedOnceAsync(integrationId, request, cancellationToken);
            }
            catch (DbUpdateException e) when (e.IsUniqueViolation())
            {
                // Тот же slug или номер версии успел занять параллельный запрос: например, несколько экземпляров
                // приложения сканируют при одновременном запуске. Повтор видит их контракт и, если правила те же,
                // возвращает его без изменений
                if (attempt == PublishAttempts)
                    return (SdkContractPublishStatus.Conflict, null);

                db.ChangeTracker.Clear();
            }
        }
    }

    private async Task<(SdkContractPublishStatus Status, PublishedContract? Contract)> PublishScannedOnceAsync(
        Guid integrationId,
        ScannedContractRequest request,
        CancellationToken cancellationToken
    )
    {
        var contractId = await db
            .Contracts.Where(x => x.IntegrationId == integrationId && x.Slug == request.Slug)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        var version = new ContractVersion
        {
            Version = 1,
            Rules = request.Rules,
            Comment = ScannedComment,
            PublishedAt = timeProvider.GetUtcNow().UtcDateTime,
        };

        if (contractId is null)
        {
            db.Contracts.Add(
                new Contract
                {
                    IntegrationId = integrationId,
                    Slug = request.Slug,
                    // Названия в коде нет
                    Name = request.Slug,
                    Versions = [version],
                }
            );
            await db.SaveChangesAsync(cancellationToken);

            return (SdkContractPublishStatus.Created, ToPublished(request.Slug, version));
        }

        var published = await db
            .ContractVersions.AsNoTracking()
            .Where(x => x.ContractId == contractId && x.PublishedAt != null)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);
        if (published is not null && HasSameRules(published.Rules, request.Rules))
            return (SdkContractPublishStatus.Unchanged, ToPublished(request.Slug, published));

        // Номер — после последней версии, в том числе неопубликованной версии с фронта
        var lastVersion =
            await db.ContractVersions.Where(x => x.ContractId == contractId).MaxAsync(x => (int?)x.Version, cancellationToken)
            ?? 0;

        version.ContractId = contractId.Value;
        version.Version = lastVersion + 1;
        db.ContractVersions.Add(version);
        await db.SaveChangesAsync(cancellationToken);

        return (SdkContractPublishStatus.Created, ToPublished(request.Slug, version));
    }

    // Порядок правил не важен: при сканировании он зависит только от порядка свойств в модели.
    // Имена полей в версии уникальны, поэтому достаточно найти каждое правило с тем же именем
    internal static bool HasSameRules(List<ContractRule> published, List<ContractRule> scanned) =>
        published.Count == scanned.Count
        && scanned.All(rule =>
            published.Any(x =>
                x.FieldName == rule.FieldName
                && x.Type == rule.Type
                && x.Required == rule.Required
                && x.Nullable == rule.Nullable
            )
        );

    private static PublishedContract ToPublished(string slug, ContractVersion version) =>
        new()
        {
            Slug = slug,
            ContractVersion = version.Version,
            Rules = version.Rules,
        };
}
