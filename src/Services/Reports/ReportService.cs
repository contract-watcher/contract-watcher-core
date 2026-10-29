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
    /// Monitoring отклонил параметры запроса. Бывает только у истории и сводки, вместе с ошибками по параметрам
    /// </summary>
    InvalidRequest,

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

    /// <summary>
    /// История проверок. InvalidParameters — ошибки по параметрам запроса, если Monitoring их отклонил
    /// </summary>
    public async Task<(
        ReportReadStatus Status,
        PageResponse<ReportHistoryItemResponse>? Page,
        Dictionary<string, string[]>? InvalidParameters
    )> GetHistoryAsync(Guid userId, Guid integrationId, ReportHistoryQuery query, CancellationToken cancellationToken)
    {
        if (!await IntegrationExistsAsync(userId, integrationId, cancellationToken))
            return (ReportReadStatus.NotFound, null, null);

        var history = await monitoringClient.GetReportHistoryAsync(integrationId, query, cancellationToken);
        if (history.Status != MonitoringReadStatus.Found)
            return (
                Fail(history.Status, history.Error, passInvalidRequest: true),
                null,
                ToQueryErrors(history.InvalidParameters)
            );

        var page = history.Value!;
        var contracts = await GetContractsAsync(integrationId, page.Items.Select(x => x.ContractId), cancellationToken);
        var items = page
            .Items.Select(x =>
                contracts.TryGetValue(x.ContractId, out var contract)
                    ? x with { ContractSlug = contract.Slug, ContractName = contract.Name }
                    : x
            )
            .ToList();

        return (ReportReadStatus.Found, page with { Items = items }, null);
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

        var read = await monitoringClient.GetReportAsync(integrationId, reportId, cancellationToken);
        if (read.Status != MonitoringReadStatus.Found)
            return (Fail(read.Status, read.Error), null);

        var report = read.Value!;
        var contracts = await GetContractsAsync(integrationId, [report.ContractId], cancellationToken);
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

        var groups = await monitoringClient.GetViolationGroupsAsync(integrationId, query, cancellationToken);
        if (groups.Status != MonitoringReadStatus.Found)
            return (Fail(groups.Status, groups.Error), null);

        var page = groups.Value!;
        var contracts = await GetContractsAsync(integrationId, page.Items.Select(x => x.ContractId), cancellationToken);
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

        var read = await monitoringClient.GetViolationGroupAsync(integrationId, groupId, cancellationToken);
        if (read.Status != MonitoringReadStatus.Found)
            return (Fail(read.Status, read.Error), null);

        var group = read.Value!;
        var contracts = await GetContractsAsync(integrationId, [group.ContractId], cancellationToken);
        return contracts.TryGetValue(group.ContractId, out var contract)
            ? (ReportReadStatus.Found, group with { ContractSlug = contract.Slug, ContractName = contract.Name })
            : (ReportReadStatus.Found, group);
    }

    /// <summary>
    /// Сводка по интеграции. Счётчики и время последнего отчёта за выбранный период считает Monitoring одним снимком.
    /// Групп в его сводке нет, поэтому они берутся первой страницей списка групп — за всё время, без учёта периода.
    /// InvalidParameters — ошибки по параметрам запроса, если Monitoring их отклонил
    /// </summary>
    public async Task<(
        ReportReadStatus Status,
        IntegrationSummaryResponse? Summary,
        Dictionary<string, string[]>? InvalidParameters
    )> GetSummaryAsync(
        Guid userId,
        Guid integrationId,
        IntegrationSummaryQuery query,
        CancellationToken cancellationToken
    )
    {
        if (!await IntegrationExistsAsync(userId, integrationId, cancellationToken))
            return (ReportReadStatus.NotFound, null, null);

        // Запросы независимы, поэтому идут параллельно. DbContext в это время не используется
        var summaryTask = monitoringClient.GetSummaryAsync(integrationId, query, cancellationToken);
        var groupsTask = monitoringClient.GetViolationGroupsAsync(
            integrationId,
            new ViolationGroupsQuery { Limit = SummaryViolationGroupLimit },
            cancellationToken
        );

        var summary = await summaryTask;
        if (summary.Status != MonitoringReadStatus.Found)
            return (
                Fail(summary.Status, summary.Error, passInvalidRequest: true),
                null,
                ToQueryErrors(summary.InvalidParameters)
            );

        var groups = await groupsTask;
        if (groups.Status != MonitoringReadStatus.Found)
            return (Fail(groups.Status, groups.Error), null, null);

        var groupPage = groups.Value!;
        var contracts = await GetContractsAsync(
            integrationId,
            groupPage.Items.Select(x => x.ContractId),
            cancellationToken
        );
        var latestGroups = groupPage
            .Items.Select(x =>
                contracts.TryGetValue(x.ContractId, out var contract)
                    ? x with { ContractSlug = contract.Slug, ContractName = contract.Name }
                    : x
            )
            .ToList();

        var counters = summary.Value!;
        return (
            ReportReadStatus.Found,
            new IntegrationSummaryResponse(
                counters.TotalReports,
                counters.PassedReports,
                counters.FailedReports,
                counters.LastReceivedAt,
                groupPage.Total,
                latestGroups
            ),
            null
        );
    }

    /// <summary>
    /// Сводки по всем интеграциям проекта за период: больше неуспешных проверок — выше. Счётчики, фильтр, сортировку и
    /// страницу считает Monitoring одним снимком по всему набору интеграций проекта из Core.
    /// InvalidParameters — ошибки по параметрам запроса, если Monitoring их отклонил
    /// </summary>
    public async Task<(
        ReportReadStatus Status,
        PageResponse<ProjectSummaryItemResponse>? Page,
        Dictionary<string, string[]>? InvalidParameters
    )> GetProjectSummaryAsync(
        Guid userId,
        Guid projectId,
        ProjectSummaryQuery query,
        CancellationToken cancellationToken
    )
    {
        var integrations = await db
            .Integrations.Where(x => x.ProjectId == projectId && x.Project.OwnerId == userId)
            .Select(x => new { x.Id, x.Slug, x.Name })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (integrations.Count == 0)
        {
            if (!await db.Projects.AnyAsync(x => x.Id == projectId && x.OwnerId == userId, cancellationToken))
                return (ReportReadStatus.NotFound, null, null);

            // Без интеграций сводок нет, и Monitoring не нужен
            return (
                ReportReadStatus.Found,
                new PageResponse<ProjectSummaryItemResponse>([], 0, query.Offset, query.Limit),
                null
            );
        }

        var summaries = await monitoringClient.GetIntegrationSummariesAsync(
            [.. integrations.Keys],
            query,
            cancellationToken
        );
        if (summaries.Status != MonitoringReadStatus.Found)
            return (
                Fail(summaries.Status, summaries.Error, passInvalidRequest: true),
                null,
                ToQueryErrors(summaries.InvalidParameters)
            );

        var page = summaries.Value!;
        // Monitoring возвращает только переданные ему интеграции, поэтому каждая есть в словаре
        var items = page
            .Items.Select(x =>
            {
                var integration = integrations[x.IntegrationId];
                return new ProjectSummaryItemResponse(
                    x.IntegrationId,
                    integration.Slug,
                    integration.Name,
                    x.TotalReports,
                    x.PassedReports,
                    x.FailedReports,
                    x.LastReceivedAt
                );
            })
            .ToList();

        return (
            ReportReadStatus.Found,
            new PageResponse<ProjectSummaryItemResponse>(items, page.Total, page.Offset, page.Limit),
            null
        );
    }

    // Ошибки параметров передаются фронту только из истории и сводки. В остальных запросах отказ Monitoring в
    // параметрах — сбой обмена: их формирует Core или проверяет по тем же правилам
    private ReportReadStatus Fail(MonitoringReadStatus status, string? error, bool passInvalidRequest = false)
    {
        if (status == MonitoringReadStatus.NotFound)
            return ReportReadStatus.NotFound;

        if (status == MonitoringReadStatus.InvalidRequest && passInvalidRequest)
        {
            // Core проверяет те же правила, поэтому отказ Monitoring значит, что правила разошлись
            logger.LogWarning("Monitoring rejected request parameters: {Error}", error);
            return ReportReadStatus.InvalidRequest;
        }

        logger.LogWarning("Reading from Monitoring failed: {Error}", error);
        return ReportReadStatus.MonitoringUnavailable;
    }

    // Параметры Monitoring называются так же, как параметры запроса Core. Ключи приводятся к виду собственных ошибок
    // валидации Core: «contractId» → «ContractId»
    private static Dictionary<string, string[]>? ToQueryErrors(Dictionary<string, string[]>? invalidParameters) =>
        invalidParameters?.ToDictionary(
            x => x.Key.Length == 0 ? x.Key : char.ToUpperInvariant(x.Key[0]) + x.Key[1..],
            x => x.Value
        );

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
