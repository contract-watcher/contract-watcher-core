using ContractWatcher.Core.Models.Reports;
using ContractWatcher.Core.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractWatcher.Core.Controllers;

/// <summary>
/// Сводка по проверкам интеграции. Данные хранит Monitoring, Core проверяет доступ и дополняет ответ данными контракта
/// </summary>
[ApiController]
[Authorize]
[Route("api/integrations/{integrationId:guid}/summary")]
public class IntegrationSummaryController(ReportService reportService) : ApiControllerBase
{
    /// <summary>
    /// Счётчики проверок и групп, последняя проверка и последние группы нарушений. Monitoring недоступен — 503
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IntegrationSummaryResponse>> Get(
        Guid integrationId,
        CancellationToken cancellationToken
    )
    {
        var (status, summary) = await reportService.GetSummaryAsync(CurrentUserId, integrationId, cancellationToken);

        if (status == ReportReadStatus.MonitoringUnavailable)
            return MonitoringUnavailable();

        if (status == ReportReadStatus.NotFound)
            return NotFound();

        return summary!;
    }
}
