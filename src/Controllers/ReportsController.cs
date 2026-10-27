using ContractWatcher.Core.Models.Reports;
using ContractWatcher.Core.Services.Reports;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractWatcher.Core.Controllers;

/// <summary>
/// История проверок интеграции. Данные хранит Monitoring, Core проверяет доступ и дополняет ответ данными контракта
/// </summary>
[ApiController]
[Authorize]
[Route("api/integrations/{integrationId:guid}/reports")]
public class ReportsController(ReportService reportService) : ApiControllerBase
{
    /// <summary>
    /// История проверок, новые первыми. Фильтры применяются до пагинации. Monitoring недоступен — 503
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PageResponse<ReportHistoryItemResponse>>> GetList(
        Guid integrationId,
        [FromQuery] ReportHistoryQuery query,
        [FromServices] IValidator<ReportHistoryQuery> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailed(validation);

        var (status, page) = await reportService.GetHistoryAsync(
            CurrentUserId,
            integrationId,
            query,
            cancellationToken
        );

        if (status == ReportReadStatus.MonitoringUnavailable)
            return MonitoringUnavailable();

        if (status == ReportReadStatus.NotFound)
            return NotFound();

        return page!;
    }

    /// <summary>
    /// Подробности отчёта с нарушениями. Monitoring недоступен — 503
    /// </summary>
    [HttpGet("{reportId:guid}")]
    public async Task<ActionResult<ReportDetailsResponse>> Get(
        Guid integrationId,
        Guid reportId,
        CancellationToken cancellationToken
    )
    {
        var (status, report) = await reportService.GetReportAsync(
            CurrentUserId,
            integrationId,
            reportId,
            cancellationToken
        );

        if (status == ReportReadStatus.MonitoringUnavailable)
            return MonitoringUnavailable();
        
        if (status == ReportReadStatus.NotFound)
            return NotFound();

        return report!;
    }
}
