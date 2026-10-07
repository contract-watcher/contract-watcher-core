using ContractWatcher.Core.Extensions;
using ContractWatcher.Core.Models.Auth;
using ContractWatcher.Core.Services.Auth;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ContractWatcher.Core.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting(AuthExtension.RateLimitPolicy)]
public class AuthController(AuthService authService) : ControllerBase
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

    private ActionResult ValidationFailed(ValidationResult validation) =>
        ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
}
