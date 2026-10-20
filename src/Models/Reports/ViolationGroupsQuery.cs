using FluentValidation;

namespace ContractWatcher.Core.Models.Reports;

/// <summary>
/// Пагинация групп нарушений
/// </summary>
public class ViolationGroupsQuery
{
    /// <summary>
    /// Сколько групп пропустить
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// Сколько групп вернуть
    /// </summary>
    public int Limit { get; set; } = ReportHistoryQuery.DefaultLimit;
}

public class ViolationGroupsQueryValidator : AbstractValidator<ViolationGroupsQuery>
{
    public ViolationGroupsQueryValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).InclusiveBetween(1, ReportHistoryQuery.MaxLimit);
    }
}
