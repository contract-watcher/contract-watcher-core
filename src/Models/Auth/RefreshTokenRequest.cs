using FluentValidation;

namespace ContractWatcher.Core.Models.Auth;

/// <summary>
/// Запрос с refresh-токеном: для обновления токенов и выхода
/// </summary>
public record RefreshTokenRequest(string RefreshToken);

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
