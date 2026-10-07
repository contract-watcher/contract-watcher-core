using ContractWatcher.Core.Data.Domain;
using FluentValidation;

namespace ContractWatcher.Core.Models.Auth;

/// <summary>
/// Запрос на регистрацию
/// </summary>
public record RegisterRequest(string Email, string Password, string Name);

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(User.EmailMaxLength);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(PasswordMinLength).MaximumLength(PasswordMaxLength);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(User.NameMaxLength);
    }
}
