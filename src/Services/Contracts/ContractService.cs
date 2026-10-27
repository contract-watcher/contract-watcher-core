using ContractWatcher.Core.Data;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Extensions;
using ContractWatcher.Core.Models.Contracts;
using ContractWatcher.Core.Models.Reports;
using Microsoft.EntityFrameworkCore;

namespace ContractWatcher.Core.Services.Contracts;

/// <summary>
/// Результат создания контракта
/// </summary>
public enum ContractCreateStatus
{
    Created,
    IntegrationNotFound,
    SlugTaken,
}

/// <summary>
/// Контракты интеграции. Доступны только владельцу проекта, к которому относится интеграция
/// </summary>
public class ContractService(DataContext db)
{
    private const string LikeEscape = @"\";

    /// <summary>
    /// Контракты интеграции. Null — интеграции нет или проект чужой
    /// </summary>
    public async Task<List<ContractResponse>?> GetListAsync(
        Guid userId,
        Guid integrationId,
        CancellationToken cancellationToken
    )
    {
        if (!await IntegrationExistsAsync(userId, integrationId, cancellationToken))
            return null;

        return await ToResponse(db.Contracts.Where(x => x.IntegrationId == integrationId).OrderBy(x => x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Страница контрактов для выбора в фильтре истории, по названию. Контракты из Core не удаляются, поэтому в списке
    /// есть все, по которым есть история. Null — интеграции нет или проект чужой
    /// </summary>
    public async Task<PageResponse<ContractSearchItemResponse>?> SearchAsync(
        Guid userId,
        Guid integrationId,
        ContractSearchQuery query,
        CancellationToken cancellationToken
    )
    {
        if (!await IntegrationExistsAsync(userId, integrationId, cancellationToken))
            return null;

        var contracts = db.Contracts.Where(x => x.IntegrationId == integrationId);

        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            contracts = contracts.Where(x => EF.Functions.ILike(x.Name, EscapeLike(search) + "%", LikeEscape));

        var total = await contracts.LongCountAsync(cancellationToken);
        var items = await contracts
            // Без учёта регистра, как и поиск: порядок не зависит от правил сортировки базы
            .OrderBy(x => x.Name.ToLower())
            // Одинаковые названия упорядочены по ID, чтобы страницы не пересекались
            .ThenBy(x => x.Id)
            .Skip(query.Offset)
            .Take(query.Limit)
            .Select(x => new ContractSearchItemResponse(x.Id, x.Slug, x.Name))
            .ToListAsync(cancellationToken);

        return new PageResponse<ContractSearchItemResponse>(items, total, query.Offset, query.Limit);
    }

    /// <summary>
    /// Контракт интеграции. Null — контракта нет или проект чужой
    /// </summary>
    public Task<ContractResponse?> GetAsync(
        Guid userId,
        Guid integrationId,
        Guid contractId,
        CancellationToken cancellationToken
    ) =>
        ToResponse(
                db.Contracts.Where(x =>
                    x.Id == contractId && x.IntegrationId == integrationId && x.Integration.Project.OwnerId == userId
                )
            )
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<(ContractCreateStatus Status, ContractResponse? Contract)> CreateAsync(
        Guid userId,
        Guid integrationId,
        CreateContractRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!await IntegrationExistsAsync(userId, integrationId, cancellationToken))
            return (ContractCreateStatus.IntegrationNotFound, null);

        if (
            await db.Contracts.AnyAsync(
                x => x.IntegrationId == integrationId && x.Slug == request.Slug,
                cancellationToken
            )
        )
            return (ContractCreateStatus.SlugTaken, null);

        var contract = new Contract
        {
            IntegrationId = integrationId,
            Slug = request.Slug,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
        };
        db.Contracts.Add(contract);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e) when (e.IsUniqueViolation())
        {
            // Тот же slug успели занять параллельным запросом
            return (ContractCreateStatus.SlugTaken, null);
        }

        // У нового контракта версий ещё нет
        var response = new ContractResponse(
            contract.Id,
            contract.IntegrationId,
            contract.Slug,
            contract.Name,
            contract.Description,
            null,
            contract.CreatedAt,
            contract.UpdatedAt
        );

        return (ContractCreateStatus.Created, response);
    }

    private Task<bool> IntegrationExistsAsync(Guid userId, Guid integrationId, CancellationToken cancellationToken) =>
        db.Integrations.AnyAsync(x => x.Id == integrationId && x.Project.OwnerId == userId, cancellationToken);

    // % и _ в поиске — обычные символы, а не шаблон. Символ экранирования передаётся в ILIKE явно:
    // без него Npgsql добавляет ESCAPE '' и экранирование не работает
    private static string EscapeLike(string value) =>
        value.Replace(LikeEscape, LikeEscape + LikeEscape).Replace("%", LikeEscape + "%").Replace("_", LikeEscape + "_");

    // Номер последней версии считается в том же SQL-запросе, без загрузки самих версий
    private static IQueryable<ContractResponse> ToResponse(IQueryable<Contract> contracts) =>
        contracts.Select(x => new ContractResponse(
            x.Id,
            x.IntegrationId,
            x.Slug,
            x.Name,
            x.Description,
            x.Versions.Max(v => (int?)v.Version),
            x.CreatedAt,
            x.UpdatedAt
        ));
}
