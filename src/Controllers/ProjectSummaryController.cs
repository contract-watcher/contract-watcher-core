using ContractWatcher.Core.Models.Reports;
using ContractWatcher.Core.Services.Reports;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractWatcher.Core.Controllers;

/// <summary>
/// Сводка по проверкам всех интеграций проекта. Данные хранит Monitoring, Core проверяет доступ и дополняет ответ
/// данными интеграций
/// </summary>
[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}/integration-summaries")]
public class ProjectSummaryController(ReportService reportService) : ApiControllerBase
{
    /// <summary>
    /// Страница сводок интеграций проекта или выбранных из них за период [from, to), без границ — за всё время.
    /// Порядок: больше неуспешных проверок — выше. Интеграция без проверок за период — с нулями. Параметры, отклонённые
    /// Monitoring, — 400 с ошибками по параметрам. Monitoring недоступен — 503, отклонил запрос Core — 500
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PageResponse<ProjectSummaryItemResponse>>> Get(
        Guid projectId,
        [FromQuery] ProjectSummaryQuery query,
        [FromServices] IValidator<ProjectSummaryQuery> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailed(validation);

        var (status, page, invalidParameters) = await reportService.GetProjectSummaryAsync(
            CurrentUserId,
            projectId,
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

        return page!;
    }
}
