using ContractWatcher.Core.Extensions;
using ContractWatcher.Core.Models.Auth;
using ContractWatcher.Core.Services.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ContractWatcher.Core.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting(AuthExtension.RateLimitPolicy)]
public class AuthController(AuthService authService) : ApiControllerBase
{
    /// <summary>
    /// Регистрация. Сразу выдаёт токены, отдельный вход не нужен
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthTokens>> Register(
        RegisterRequest request,
        [FromServices] IValidator<RegisterRequest> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailed(validation);

        var tokens = await authService.RegisterAsync(
            request.Email,
            request.Password,
            request.Name,
            cancellationToken
        );
        if (tokens is null)
            return Problem(title: "Email is already registered", statusCode: StatusCodes.Status409Conflict);

        return tokens;
    }

    /// <summary>
    /// Вход по email и паролю
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthTokens>> Login(
        LoginRequest request,
        [FromServices] IValidator<LoginRequest> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailed(validation);

        var tokens = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
        if (tokens is null)
            return Problem(title: "Invalid email or password", statusCode: StatusCodes.Status401Unauthorized);

        return tokens;
    }

    /// <summary>
    /// Обмен refresh-токена на новую пару токенов
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthTokens>> Refresh(
        RefreshTokenRequest request,
        [FromServices] IValidator<RefreshTokenRequest> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailed(validation);

        var tokens = await authService.RefreshAsync(request.RefreshToken, cancellationToken);
        if (tokens is null)
            return Problem(title: "Invalid refresh token", statusCode: StatusCodes.Status401Unauthorized);

        return tokens;
    }

    /// <summary>
    /// Выход: отзывает refresh-токен
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        RefreshTokenRequest request,
        [FromServices] IValidator<RefreshTokenRequest> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailed(validation);

        await authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Текущий пользователь: email и подтверждён ли он. Лимит защиты от подбора паролей не действует:
    /// фронт запрашивает это при каждом открытии и возврате на вкладку
    /// </summary>
    [Authorize]
    [DisableRateLimiting]
    [HttpGet("me")]
    public async Task<ActionResult<AccountResponse>> GetAccount(CancellationToken cancellationToken)
    {
        var account = await authService.GetAccountAsync(CurrentUserId, cancellationToken);
        if (account is null)
            return NotFound();

        return account;
    }

    /// <summary>
    /// Письмо со ссылкой подтверждения email: без подтверждения письма о неуспешных проверках не включаются.
    /// Уже подтверждён — 409, повтор раньше чем через минуту — 429
    /// </summary>
    [Authorize]
    [HttpPost("email-confirmation")]
    public async Task<IActionResult> RequestEmailConfirmation(
        [FromServices] EmailConfirmationService emailConfirmationService,
        CancellationToken cancellationToken
    )
    {
        var status = await emailConfirmationService.RequestAsync(CurrentUserId, cancellationToken);

        if (status == EmailConfirmationRequestStatus.AlreadyConfirmed)
            return Problem(title: "Email is already confirmed", statusCode: StatusCodes.Status409Conflict);

        if (status == EmailConfirmationRequestStatus.TooSoon)
            return Problem(
                title: "Confirmation email was sent recently, try again in a minute",
                statusCode: StatusCodes.Status429TooManyRequests
            );

        // Письмо поставлено в очередь, а не отправлено
        return Accepted();
    }

    /// <summary>
    /// Подтверждение email токеном из письма. Неизвестный, использованный или истёкший токен — 400
    /// </summary>
    [HttpPost("email-confirmation/confirm")]
    public async Task<IActionResult> ConfirmEmail(
        ConfirmEmailRequest request,
        [FromServices] IValidator<ConfirmEmailRequest> validator,
        [FromServices] EmailConfirmationService emailConfirmationService,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailed(validation);

        if (!await emailConfirmationService.ConfirmAsync(request.Token, cancellationToken))
            return Problem(
                title: "Confirmation link is invalid or expired",
                statusCode: StatusCodes.Status400BadRequest
            );

        return NoContent();
    }
}
