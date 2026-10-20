using ContractWatcher.Common.Enums;
using FluentValidation;

namespace ContractWatcher.Core.Models.Reports;

/// <summary>
/// Фильтры и пагинация истории проверок. Фильтрует и считает Monitoring, до пагинации
/// </summary>
public class ReportHistoryQuery
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;

    /// <summary>
    /// Нижняя граница времени приёма отчёта, включительно
    /// </summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// Верхняя граница времени приёма отчёта, исключительно
    /// </summary>
    public DateTimeOffset? To { get; set; }

    /// <summary>
    /// Результат проверки. Null — все результаты
    /// </summary>
    public ValidationStatus? Status { get; set; }

    /// <summary>
    /// Сколько отчётов пропустить
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// Сколько отчётов вернуть
    /// </summary>
    public int Limit { get; set; } = DefaultLimit;
}

/// <summary>
/// Те же ограничения, что у Monitoring: запрос, прошедший проверку, Monitoring не отклонит
/// </summary>
public class ReportHistoryQueryValidator : AbstractValidator<ReportHistoryQuery>
{
    public ReportHistoryQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThan(x => x.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("'To' must be later than 'From'");
        // Enum из query-строки принимает и числа, поэтому неизвестное значение может прийти как число
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).InclusiveBetween(1, ReportHistoryQuery.MaxLimit);
    }
}
