using ContractWatcher.Core.Data;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ContractWatcher.Core.Services.Contracts;

/// <summary>
/// Версии контракта. Доступны только владельцу проекта, к которому относится контракт
/// </summary>
public class ContractVersionService(DataContext db)
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

    private Task<bool> ContractExistsAsync(Guid userId, Guid contractId, CancellationToken cancellationToken) =>
        db.Contracts.AnyAsync(x => x.Id == contractId && x.Integration.Project.OwnerId == userId, cancellationToken);
    
    private static IQueryable<ContractVersionResponse> ToResponse(IQueryable<ContractVersion> versions) =>
        versions.Select(x => new ContractVersionResponse(
            x.Id,
            x.ContractId,
            x.Version,
            x.Schema,
            x.Comment,
            x.CreatedAt
        ));
}
