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

    protected ActionResult ValidationFailed(ValidationResult validation) =>
        ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
}
