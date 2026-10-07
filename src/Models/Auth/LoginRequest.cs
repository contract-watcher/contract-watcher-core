using FluentValidation;

namespace ContractWatcher.Core.Models.Auth;

/// <summary>
/// Запрос на вход
/// </summary>
public record LoginRequest(string Email, string Password);

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    // Правила сложности пароля здесь не проверяем: они для регистрации, а при входе пароль просто сверяется
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
