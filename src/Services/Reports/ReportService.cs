using ContractWatcher.Core.Data;
using ContractWatcher.Core.Models.Reports;
using ContractWatcher.Core.Services.Monitoring;
using Microsoft.EntityFrameworkCore;

namespace ContractWatcher.Core.Services.Reports;

/// <summary>
/// Результат чтения истории проверок
/// </summary>
public enum ReportReadStatus
{
    Found,

    /// <summary>
    /// Интеграции нет, проект чужой или в Monitoring нет такого отчёта или группы
    /// </summary>
    NotFound,

    /// <summary>
    /// Monitoring недоступен
    /// </summary>
    MonitoringUnavailable,
}

/// <summary>
/// История проверок и группы нарушений из Monitoring. Доступны только владельцу проекта, к которому относится интеграция.
/// У Monitoring нет авторизации пользователей, поэтому доступ проверяет Core, а ответ дополняется данными контракта
/// </summary>
public class ReportService(DataContext db, MonitoringClient monitoringClient, ILogger<ReportService> logger)
{
    public async Task<(ReportReadStatus Status, PageResponse<ReportHistoryItemResponse>? Page)> GetHistoryAsync(
        Guid userId,
        Guid integrationId,
        ReportHistoryQuery query,
        CancellationToken cancellationToken
    )
    {
        if (!await IntegrationExistsAsync(userId, integrationId, cancellationToken))
            return (ReportReadStatus.NotFound, null);

        var (status, page, error) = await monitoringClient.GetReportHistoryAsync(integrationId, query, cancellationToken);
        if (status != MonitoringReadStatus.Found)
            return (Fail(status, error), null);

        var contracts = await GetContractsAsync(integrationId, page!.Items.Select(x => x.ContractId), cancellationToken);
        var items = page
            .Items.Select(x =>
                contracts.TryGetValue(x.ContractId, out var contract)
                    ? x with { ContractSlug = contract.Slug, ContractName = contract.Name }
                    : x
            )
            .ToList();

        return (ReportReadStatus.Found, page with { Items = items });
    }

    public async Task<(ReportReadStatus Status, ReportDetailsResponse? Report)> GetReportAsync(
        Guid userId,
        Guid integrationId,
        Guid reportId,
        CancellationToken cancellationToken
    )
    {
        if (!await IntegrationExistsAsync(userId, integrationId, cancellationToken))
            return (ReportReadStatus.NotFound, null);

        var (status, report, error) = await monitoringClient.GetReportAsync(integrationId, reportId, cancellationToken);
        if (status != MonitoringReadStatus.Found)
            return (Fail(status, error), null);

        var contracts = await GetContractsAsync(integrationId, [report!.ContractId], cancellationToken);
        return contracts.TryGetValue(report.ContractId, out var contract)
            ? (ReportReadStatus.Found, report with { ContractSlug = contract.Slug, ContractName = contract.Name })
            : (ReportReadStatus.Found, report);
    }

    public async Task<(ReportReadStatus Status, PageResponse<ViolationGroupResponse>? Page)> GetViolationGroupsAsync(
        Guid userId,
        Guid integrationId,
        ViolationGroupsQuery query,
        CancellationToken cancellationToken
    )
    {
        if (!await IntegrationExistsAsync(userId, integrationId, cancellationToken))
            return (ReportReadStatus.NotFound, null);

        var (status, page, error) = await monitoringClient.GetViolationGroupsAsync(
            integrationId,
            query,
            cancellationToken
        );
        if (status != MonitoringReadStatus.Found)
            return (Fail(status, error), null);

        var contracts = await GetContractsAsync(integrationId, page!.Items.Select(x => x.ContractId), cancellationToken);
        var items = page
            .Items.Select(x =>
                contracts.TryGetValue(x.ContractId, out var contract)
                    ? x with { ContractSlug = contract.Slug, ContractName = contract.Name }
                    : x
            )
            .ToList();

        return (ReportReadStatus.Found, page with { Items = items });
    }

    public async Task<(ReportReadStatus Status, ViolationGroupResponse? Group)> GetViolationGroupAsync(
        Guid userId,
        Guid integrationId,
        Guid groupId,
        CancellationToken cancellationToken
    )
    {
        if (!await IntegrationExistsAsync(userId, integrationId, cancellationToken))
            return (ReportReadStatus.NotFound, null);

        var (status, group, error) = await monitoringClient.GetViolationGroupAsync(
            integrationId,
            groupId,
            cancellationToken
        );
        if (status != MonitoringReadStatus.Found)
            return (Fail(status, error), null);

        var contracts = await GetContractsAsync(integrationId, [group!.ContractId], cancellationToken);
        return contracts.TryGetValue(group.ContractId, out var contract)
            ? (ReportReadStatus.Found, group with { ContractSlug = contract.Slug, ContractName = contract.Name })
            : (ReportReadStatus.Found, group);
    }

    private ReportReadStatus Fail(MonitoringReadStatus status, string? error)
    {
        if (status == MonitoringReadStatus.NotFound)
            return ReportReadStatus.NotFound;

        logger.LogWarning("Reading from Monitoring failed: {Error}", error);
        return ReportReadStatus.MonitoringUnavailable;
    }

    private Task<bool> IntegrationExistsAsync(Guid userId, Guid integrationId, CancellationToken cancellationToken) =>
        db.Integrations.AnyAsync(x => x.Id == integrationId && x.Project.OwnerId == userId, cancellationToken);

    // Slug и название контрактов одним запросом. Удалённого из Core контракта в словаре не будет
    private async Task<Dictionary<Guid, (string Slug, string Name)>> GetContractsAsync(
        Guid integrationId,
        IEnumerable<Guid> contractIds,
        CancellationToken cancellationToken
    )
    {
        var ids = contractIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var contracts = await db
            .Contracts.Where(x => x.IntegrationId == integrationId && ids.Contains(x.Id))
            .Select(x => new { x.Id, x.Slug, x.Name })
            .ToListAsync(cancellationToken);

        return contracts.ToDictionary(x => x.Id, x => (x.Slug, x.Name));
    }
}
