using FluentValidation;

namespace ContractWatcher.Core.Models.Reports;

/// <summary>
/// Период, фильтр и страница сводок по интеграциям проекта. Границы периода — как у сводки интеграции
/// </summary>
public class ProjectSummaryQuery
{
    /// <summary>
    /// Нижняя граница времени приёма отчёта, включительно
    /// </summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// Верхняя граница времени приёма отчёта, исключительно
    /// </summary>
    public DateTimeOffset? To { get; set; }

    /// <summary>
    /// Только интеграции с неуспешными проверками за период
    /// </summary>
    public bool OnlyFailed { get; set; }

    /// <summary>
    /// Сколько интеграций пропустить
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// Сколько интеграций вернуть
    /// </summary>
    public int Limit { get; set; } = ReportHistoryQuery.DefaultLimit;
}

/// <summary>
/// Те же ограничения, что у Monitoring: запрос, прошедший проверку, Monitoring не отклонит
/// </summary>
public class ProjectSummaryQueryValidator : AbstractValidator<ProjectSummaryQuery>
{
    public ProjectSummaryQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThan(x => x.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("'To' must be later than 'From'");
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).InclusiveBetween(1, ReportHistoryQuery.MaxLimit);
    }
}
