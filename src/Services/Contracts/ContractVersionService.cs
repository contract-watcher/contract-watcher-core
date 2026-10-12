using ContractWatcher.Core.Data;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Extensions;
using ContractWatcher.Core.Models.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ContractWatcher.Core.Services.Contracts;

/// <summary>
/// Результат создания версии контракта
/// </summary>
public enum ContractVersionCreateStatus
{
    Created,
    ContractNotFound,
    VersionConflict,
}

/// <summary>
/// Версии контракта. Доступны только владельцу проекта, к которому относится контракт
/// </summary>
public class ContractVersionService(DataContext db, TimeProvider timeProvider)
{
    /// <summary>
    /// Версии контракта по возрастанию номера. Null — контракта нет или проект чужой
    /// </summary>
    public async Task<List<ContractVersionResponse>?> GetListAsync(
        Guid userId,
        Guid contractId,
        CancellationToken cancellationToken
    )
    {
        if (!await ContractExistsAsync(userId, contractId, cancellationToken))
            return null;

        return await ToResponse(db.ContractVersions.Where(x => x.ContractId == contractId).OrderBy(x => x.Version))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Версия контракта по номеру. Null — версии нет или проект чужой
    /// </summary>
    public Task<ContractVersionResponse?> GetAsync(
        Guid userId,
        Guid contractId,
        int version,
        CancellationToken cancellationToken
    ) =>
        ToResponse(
                db.ContractVersions.Where(x =>
                    x.ContractId == contractId
                    && x.Version == version
                    && x.Contract.Integration.Project.OwnerId == userId
                )
            )
            .SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Создание версии. Номер — следующий после последнего
    /// </summary>
    public async Task<(ContractVersionCreateStatus Status, ContractVersionResponse? Version)> CreateAsync(
        Guid userId,
        Guid contractId,
        CreateContractVersionRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!await ContractExistsAsync(userId, contractId, cancellationToken))
            return (ContractVersionCreateStatus.ContractNotFound, null);

        var lastVersion =
            await db.ContractVersions.Where(x => x.ContractId == contractId).MaxAsync(x => (int?)x.Version, cancellationToken)
            ?? 0;

        var version = new ContractVersion
        {
            ContractId = contractId,
            Version = lastVersion + 1,
            Rules = request.Rules,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
        };
        db.ContractVersions.Add(version);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e) when (e.IsUniqueViolation())
        {
            // Тот же номер успели занять параллельным запросом
            return (ContractVersionCreateStatus.VersionConflict, null);
        }

        return (ContractVersionCreateStatus.Created, ToResponse(version));
    }

    /// <summary>
    /// Публикует версию — после этого её получает SDK. Повторная публикация не меняет дату. Null — версии нет или проект чужой
    /// </summary>
    public async Task<ContractVersionResponse?> PublishAsync(
        Guid userId,
        Guid contractId,
        int version,
        CancellationToken cancellationToken
    )
    {
        var contractVersion = await db.ContractVersions.SingleOrDefaultAsync(
            x =>
                x.ContractId == contractId
                && x.Version == version
                && x.Contract.Integration.Project.OwnerId == userId,
            cancellationToken
        );
        if (contractVersion is null)
            return null;

        if (contractVersion.PublishedAt is null)
        {
            contractVersion.PublishedAt = timeProvider.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(cancellationToken);
        }

        return ToResponse(contractVersion);
    }

    private Task<bool> ContractExistsAsync(Guid userId, Guid contractId, CancellationToken cancellationToken) =>
        db.Contracts.AnyAsync(x => x.Id == contractId && x.Integration.Project.OwnerId == userId, cancellationToken);

    private static ContractVersionResponse ToResponse(ContractVersion version) =>
        new(
            version.Id,
            version.ContractId,
            version.Version,
            version.Rules,
            version.Comment,
            version.CreatedAt,
            version.PublishedAt
        );

    private static IQueryable<ContractVersionResponse> ToResponse(IQueryable<ContractVersion> versions) =>
        versions.Select(x => new ContractVersionResponse(
            x.Id,
            x.ContractId,
            x.Version,
            x.Rules,
            x.Comment,
            x.CreatedAt,
            x.PublishedAt
        ));
}
