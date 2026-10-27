using FluentValidation;

namespace ContractWatcher.Core.Models.Reports;

/// <summary>
/// Период сводки. Границы рассчитывает фронт, обычно начало первого дня и начало дня после последнего в поясе
/// браузера. Без границ — за всё время
/// </summary>
public class IntegrationSummaryQuery
{
    /// <summary>
    /// Нижняя граница времени приёма отчёта, включительно
    /// </summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// Верхняя граница времени приёма отчёта, исключительно
    /// </summary>
    public DateTimeOffset? To { get; set; }
}

/// <summary>
/// Те же ограничения, что у Monitoring: запрос, прошедший проверку, Monitoring не отклонит
/// </summary>
public class IntegrationSummaryQueryValidator : AbstractValidator<IntegrationSummaryQuery>
{
    public IntegrationSummaryQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThan(x => x.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("'To' must be later than 'From'");
    }
}
