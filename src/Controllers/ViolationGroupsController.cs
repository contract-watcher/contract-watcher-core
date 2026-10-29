using ContractWatcher.Core.Models.Reports;
using ContractWatcher.Core.Services.Reports;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractWatcher.Core.Controllers;

/// <summary>
/// Группы одинаковых нарушений интеграции. Группирует Monitoring, Core проверяет доступ и дополняет ответ данными контракта
/// </summary>
[ApiController]
[Authorize]
[Route("api/integrations/{integrationId:guid}/violation-groups")]
public class ViolationGroupsController(ReportService reportService) : ApiControllerBase
{
    /// <summary>
    /// Группы нарушений. Monitoring недоступен — 503
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PageResponse<ViolationGroupResponse>>> GetList(
        Guid integrationId,
        [FromQuery] ViolationGroupsQuery query,
        [FromServices] IValidator<ViolationGroupsQuery> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailed(validation);

        var (status, page) = await reportService.GetViolationGroupsAsync(
            CurrentUserId,
            integrationId,
            query,
            cancellationToken
        );

        if (status == ReportReadStatus.MonitoringUnavailable)
            return MonitoringUnavailable();

        if (status == ReportReadStatus.MonitoringRejected)
            return MonitoringRejected();

        if (status == ReportReadStatus.NotFound)
            return NotFound();

        return page!;
    }

    /// <summary>
    /// Группа нарушений по ID. Monitoring недоступен — 503
    /// </summary>
    [HttpGet("{groupId:guid}")]
    public async Task<ActionResult<ViolationGroupResponse>> Get(
        Guid integrationId,
        Guid groupId,
        CancellationToken cancellationToken
    )
    {
        var (status, group) = await reportService.GetViolationGroupAsync(
            CurrentUserId,
            integrationId,
            groupId,
            cancellationToken
        );

        if (status == ReportReadStatus.MonitoringUnavailable)
            return MonitoringUnavailable();

        if (status == ReportReadStatus.MonitoringRejected)
            return MonitoringRejected();

        if (status == ReportReadStatus.NotFound)
            return NotFound();

        return group!;
    }
}
