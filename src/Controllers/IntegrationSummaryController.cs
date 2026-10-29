using ContractWatcher.Core.Models.Reports;
using ContractWatcher.Core.Services.Reports;
using FluentValidation;
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
    /// Счётчики проверок и последняя проверка за период [from, to), без границ — за всё время. Группы нарушений —
    /// за всё время. Параметры, отклонённые Monitoring, — 400 с ошибками по параметрам. Monitoring недоступен — 503
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IntegrationSummaryResponse>> Get(
        Guid integrationId,
        [FromQuery] IntegrationSummaryQuery query,
        [FromServices] IValidator<IntegrationSummaryQuery> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailed(validation);

        var (status, summary, invalidParameters) = await reportService.GetSummaryAsync(
            CurrentUserId,
            integrationId,
            query,
            cancellationToken
        );

        if (status == ReportReadStatus.InvalidRequest)
            return ValidationFailed(invalidParameters!);

        if (status == ReportReadStatus.MonitoringUnavailable)
            return MonitoringUnavailable();

        if (status == ReportReadStatus.MonitoringRejected)
            return MonitoringRejected();

        if (status == ReportReadStatus.NotFound)
            return NotFound();

        return summary!;
    }
}
