using ContractWatcher.Common.Enums;
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
    // Сколько последних групп нарушений показывать в сводке
    private const int SummaryViolationGroupLimit = 5;

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

    /// <summary>
    /// Сводка по интеграции. Своей сводки в Monitoring пока нет, поэтому она собирается из его истории и групп:
    /// счётчики — это Total страниц Passed и Failed, последняя проверка — самая новая из двух
    /// </summary>
    public async Task<(ReportReadStatus Status, IntegrationSummaryResponse? Summary)> GetSummaryAsync(
        Guid userId,
        Guid integrationId,
        CancellationToken cancellationToken
    )
    {
        if (!await IntegrationExistsAsync(userId, integrationId, cancellationToken))
            return (ReportReadStatus.NotFound, null);

        // Запросы независимы, поэтому идут параллельно. DbContext в это время не используется
        var passedTask = monitoringClient.GetReportHistoryAsync(
            integrationId,
            new ReportHistoryQuery { Status = ValidationStatus.Passed, Limit = 1 },
            cancellationToken
        );
        var failedTask = monitoringClient.GetReportHistoryAsync(
            integrationId,
            new ReportHistoryQuery { Status = ValidationStatus.Failed, Limit = 1 },
            cancellationToken
        );
        var groupsTask = monitoringClient.GetViolationGroupsAsync(
            integrationId,
            new ViolationGroupsQuery { Limit = SummaryViolationGroupLimit },
            cancellationToken
        );

        var passed = await passedTask;
        if (passed.Status != MonitoringReadStatus.Found)
            return (Fail(passed.Status, passed.Error), null);

        var failed = await failedTask;
        if (failed.Status != MonitoringReadStatus.Found)
            return (Fail(failed.Status, failed.Error), null);

        var groups = await groupsTask;
        if (groups.Status != MonitoringReadStatus.Found)
            return (Fail(groups.Status, groups.Error), null);

        // История отсортирована от новых к старым, поэтому первый элемент каждой страницы — последний отчёт с этим результатом
        var lastReport = passed.Page!.Items.Concat(failed.Page!.Items).MaxBy(x => x.ReceivedAt);

        var contractIds = groups.Page!.Items.Select(x => x.ContractId).ToList();
        if (lastReport is not null)
            contractIds.Add(lastReport.ContractId);

        var contracts = await GetContractsAsync(integrationId, contractIds, cancellationToken);

        if (lastReport is not null && contracts.TryGetValue(lastReport.ContractId, out var lastContract))
            lastReport = lastReport with { ContractSlug = lastContract.Slug, ContractName = lastContract.Name };

        var latestGroups = groups
            .Page.Items.Select(x =>
                contracts.TryGetValue(x.ContractId, out var contract)
                    ? x with { ContractSlug = contract.Slug, ContractName = contract.Name }
                    : x
            )
            .ToList();

        return (
            ReportReadStatus.Found,
            new IntegrationSummaryResponse(
                passed.Page.Total + failed.Page.Total,
                passed.Page.Total,
                failed.Page.Total,
                groups.Page.Total,
                lastReport,
                latestGroups
            )
        );
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
