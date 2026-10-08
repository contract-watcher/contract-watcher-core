using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.Integrations;
using FluentValidation;

namespace ContractWatcher.Core.Models.Contracts;

/// <summary>
/// Запрос на создание контракта
/// </summary>
public record CreateContractRequest(string Slug, string Name, string? Description);

public class CreateContractRequestValidator : AbstractValidator<CreateContractRequest>
{
    public CreateContractRequestValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(Contract.SlugMaxLength)
            .Matches(IntegrationRequestValidator.SlugPattern)
            .WithMessage("Slug may contain only lowercase latin letters, digits and hyphens");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Contract.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(Contract.DescriptionMaxLength);
    }
}
