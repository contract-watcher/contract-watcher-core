using ContractWatcher.Core.Extensions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace ContractWatcher.Core.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// ID текущего пользователя. Доступен только в эндпоинтах с [Authorize]
    /// </summary>
    protected Guid CurrentUserId => User.GetUserId();

    protected ActionResult ValidationFailed(ValidationResult validation) => ValidationFailed(validation.ToDictionary());

    /// <summary>
    /// 400 с ошибками по полям в том же формате, что и проверка запроса: имя поля — сообщения
    /// </summary>
    protected ActionResult ValidationFailed(IDictionary<string, string[]> errors) =>
        ValidationProblem(new ValidationProblemDetails(errors));

    /// <summary>
    /// Monitoring не ответил: фронт показывает title вместо пустого списка
    /// </summary>
    protected ActionResult MonitoringUnavailable() =>
        Problem(
            title: "Monitoring is unavailable, try again later",
            statusCode: StatusCodes.Status503ServiceUnavailable
        );

    /// <summary>
    /// Monitoring отклонил запрос, который сформировал Core: это ошибка Core, повтор не поможет
    /// </summary>
    protected ActionResult MonitoringRejected() =>
        Problem(title: "Monitoring rejected the request", statusCode: StatusCodes.Status500InternalServerError);
}
