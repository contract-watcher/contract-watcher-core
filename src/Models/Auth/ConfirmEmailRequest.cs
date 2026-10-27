using FluentValidation;

namespace ContractWatcher.Core.Models.Auth;

/// <summary>
/// Подтверждение email токеном из письма
/// </summary>
public record ConfirmEmailRequest(string Token);

public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(100);
    }
}
