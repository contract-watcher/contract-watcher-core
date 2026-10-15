using ContractWatcher.Common.Contracts;
using ContractWatcher.Common.Enums;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.Integrations;
using FluentValidation;

namespace ContractWatcher.Core.Models.Reports;

/// <summary>
/// Отчёт проверки от SDK: поля ValidationReport из ContractWatcher.Common и slug проверенного контракта
/// </summary>
/// <param name="Slug">Slug контракта в интеграции ключа</param>
/// <param name="Status">Nullable, чтобы отсутствующий в запросе статус не превратился в Passed</param>
public record ReportRequest(
    string Slug,
    Guid ReportId,
    int ContractVersion,
    ValidationStatus? Status,
    List<ValidationViolation> Violations
);

/// <summary>
/// Проверяет сам запрос, без сверки с правилами контракта. Правила совпадают с приёмом отчётов в Monitoring
/// </summary>
public class ReportRequestValidator : AbstractValidator<ReportRequest>
{
    public ReportRequestValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(Contract.SlugMaxLength)
            .Matches(IntegrationRequestValidator.SlugPattern)
            .WithMessage("Slug may contain only lowercase latin letters, digits and hyphens");
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.ContractVersion).GreaterThan(0);
        // JsonStringEnumConverter принимает и числа, поэтому неизвестное значение может прийти как число
        RuleFor(x => x.Status).NotNull().IsInEnum();
        RuleFor(x => x.Violations).NotNull();
        RuleFor(x => x.Violations)
            .Empty()
            .When(x => x.Status == ValidationStatus.Passed)
            .WithMessage("Passed report must not contain violations");
        RuleFor(x => x.Violations)
            .NotEmpty()
            .When(x => x.Status == ValidationStatus.Failed && x.Violations is not null)
            .WithMessage("Failed report must contain at least one violation");
        RuleForEach(x => x.Violations)
            .NotNull()
            .ChildRules(violation =>
            {
                violation.RuleFor(v => v.FieldName).NotEmpty();
                violation.RuleFor(v => v.Type).IsInEnum();
                violation.RuleFor(v => v.ExpectedType).NotNull().IsInEnum();
                // Фактический тип есть только у несовпадения типов
                violation
                    .RuleFor(v => v.ActualType)
                    .NotNull()
                    .IsInEnum()
                    .NotEqual(v => v.ExpectedType)
                    .WithMessage("Actual type must differ from expected type")
                    .When(v => v.Type == ViolationType.TypeMismatch);
                violation
                    .RuleFor(v => v.ActualType)
                    .Null()
                    .When(v => v.Type != ViolationType.TypeMismatch)
                    .WithMessage("Actual type is allowed only for TypeMismatch");
            });
    }
}
