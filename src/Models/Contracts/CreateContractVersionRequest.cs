using ContractWatcher.Common.Contracts;
using ContractWatcher.Core.Data.Domain;
using FluentValidation;

namespace ContractWatcher.Core.Models.Contracts;

/// <summary>
/// Запрос на создание версии контракта
/// </summary>
public record CreateContractVersionRequest(List<ContractRule> Rules, string? Comment);

public class CreateContractVersionRequestValidator : AbstractValidator<CreateContractVersionRequest>
{
    public CreateContractVersionRequestValidator()
    {
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
        RuleFor(x => x.Comment).MaximumLength(ContractVersion.CommentMaxLength);
    }
}
