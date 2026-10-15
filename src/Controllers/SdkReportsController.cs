using ContractWatcher.Core.Extensions;
using ContractWatcher.Core.Models.Reports;
using ContractWatcher.Core.Services.Sdk;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractWatcher.Core.Controllers;

/// <summary>
/// Приём отчётов проверки от SDK. Доступ — по ключу интеграции (X-Api-Key), JWT пользователя здесь не принимается
/// </summary>
[ApiController]
[Authorize(Policy = AuthExtension.SdkPolicy)]
[Route("api/sdk/reports")]
public class SdkReportsController(SdkReportService sdkReportService) : ControllerBase
{
    /// <summary>
    /// Приём отчёта. Контракт ищется по slug только в интеграции ключа. Нет контракта — 404
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Create(
        ReportRequest request,
        [FromServices] IValidator<ReportRequest> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));

        var status = await sdkReportService.AcceptAsync(User.GetIntegrationId(), request, cancellationToken);
        if (status == SdkReportAcceptStatus.ContractNotFound)
            return NotFound();

        return Accepted();
    }
}
