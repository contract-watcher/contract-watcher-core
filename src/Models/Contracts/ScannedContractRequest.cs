using ContractWatcher.Common.Contracts;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.Integrations;
using FluentValidation;

namespace ContractWatcher.Core.Models.Contracts;

/// <summary>
/// Контракт, который SDK построил по модели запроса endpoint. По JSON совпадает с PublishedContract из
/// ContractWatcher.Common. Номер версии из запроса не читается: его назначает Core
/// </summary>
/// <param name="Slug">Slug контракта в интеграции ключа</param>
public record ScannedContractRequest(string Slug, List<ContractRule> Rules);

/// <summary>
/// Правила те же, что при создании контракта и версии с фронта
/// </summary>
public class ScannedContractRequestValidator : AbstractValidator<ScannedContractRequest>
{
    public ScannedContractRequestValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(Contract.SlugMaxLength)
            .Matches(IntegrationRequestValidator.SlugPattern)
            .WithMessage("Slug may contain only lowercase latin letters, digits and hyphens");
        RuleFor(x => x.Rules).NotEmpty();
        RuleForEach(x => x.Rules)
            .NotNull()
            .ChildRules(rule =>
            {
                rule.RuleFor(r => r.FieldName).NotEmpty();
                // JsonStringEnumConverter принимает и числа, поэтому неизвестный тип может прийти как число
                rule.RuleFor(r => r.Type).IsInEnum();
            });
        RuleFor(x => x.Rules)
            .Must(rules => rules.Where(r => r?.FieldName is not null).GroupBy(r => r.FieldName).All(g => g.Count() == 1))
            .When(x => x.Rules is { Count: > 0 })
            .WithMessage("Field names must be unique");
    }
}
