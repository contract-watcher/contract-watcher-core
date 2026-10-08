using ContractWatcher.Core.Data.Domain;
using FluentValidation;

namespace ContractWatcher.Core.Models.Integrations;

/// <summary>
/// Запрос на создание или изменение интеграции
/// </summary>
public record IntegrationRequest(string Slug, string Name, string? Description);

public class IntegrationRequestValidator : AbstractValidator<IntegrationRequest>
{
    public const string SlugPattern = "^[a-z0-9-]+$";

    public IntegrationRequestValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(Integration.SlugMaxLength)
            .Matches(SlugPattern)
            .WithMessage("Slug may contain only lowercase latin letters, digits and hyphens");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Integration.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(Integration.DescriptionMaxLength);
    }
}
