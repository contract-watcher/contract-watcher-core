using ContractWatcher.Core.Models.Notifications;
using ContractWatcher.Core.Services.Notifications;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractWatcher.Core.Controllers;

/// <summary>
/// Уведомления интеграции: письма владельцу проекта о неуспешных проверках
/// </summary>
[ApiController]
[Authorize]
[Route("api/integrations/{integrationId:guid}/notifications")]
public class IntegrationNotificationsController(NotificationSettingsService notificationSettingsService)
    : ApiControllerBase
{
    /// <summary>
    /// Настройки уведомлений интеграции
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<NotificationSettingsResponse>> Get(
        Guid integrationId,
        CancellationToken cancellationToken
    )
    {
        var settings = await notificationSettingsService.GetAsync(CurrentUserId, integrationId, cancellationToken);
        if (settings is null)
            return NotFound();

        return settings;
    }

    /// <summary>
    /// Включение и выключение писем о неуспешных проверках. Учитываются отчёты, принятые после включения.
    /// Email владельца не подтверждён — 409
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<NotificationSettingsResponse>> Update(
        Guid integrationId,
        NotificationSettingsRequest request,
        [FromServices] IValidator<NotificationSettingsRequest> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailed(validation);

        var (status, settings) = await notificationSettingsService.UpdateAsync(
            CurrentUserId,
            integrationId,
            request,
            cancellationToken
        );

        if (status == NotificationSettingsSaveStatus.NotFound)
            return NotFound();

        if (status == NotificationSettingsSaveStatus.EmailNotConfirmed)
            return Problem(
                title: "Email is not confirmed, confirm it before enabling notifications",
                statusCode: StatusCodes.Status409Conflict
            );

        return settings!;
    }
}
