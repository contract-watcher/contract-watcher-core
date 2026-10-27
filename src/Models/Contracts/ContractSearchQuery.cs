using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.Reports;
using FluentValidation;

namespace ContractWatcher.Core.Models.Contracts;

/// <summary>
/// Поиск и пагинация контрактов для выбора в фильтре истории
/// </summary>
public class ContractSearchQuery
{
    /// <summary>
    /// Начало названия, без учёта регистра. Пусто — все контракты
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Сколько контрактов пропустить
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// Сколько контрактов вернуть
    /// </summary>
    public int Limit { get; set; } = ReportHistoryQuery.DefaultLimit;
}

public class ContractSearchQueryValidator : AbstractValidator<ContractSearchQuery>
{
    public ContractSearchQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(Contract.NameMaxLength);
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).InclusiveBetween(1, ReportHistoryQuery.MaxLimit);
    }
}
