using ContractWatcher.Core.Models.Reports;
using FluentValidation.TestHelper;

namespace ContractWatcher.Core.Tests.Models.Reports;

public sealed class IntegrationSummaryQueryValidatorTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 21, 0, 0, 0, TimeSpan.Zero);

    private readonly IntegrationSummaryQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenBoundsAreMissing_HasNoErrors()
    {
        // Act
        var result = _validator.TestValidate(new IntegrationSummaryQuery());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyFromIsSet_HasNoErrors()
    {
        // Act
        var result = _validator.TestValidate(new IntegrationSummaryQuery { From = Start });

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyToIsSet_HasNoErrors()
    {
        // Act
        var result = _validator.TestValidate(new IntegrationSummaryQuery { To = Start });

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenToIsLaterThanFrom_HasNoErrors()
    {
        // Act
        var result = _validator.TestValidate(new IntegrationSummaryQuery { From = Start, To = Start.AddDays(1) });

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenToIsNotLaterThanFrom_HasToError(int toOffsetDays)
    {
        // Act
        var result = _validator.TestValidate(new IntegrationSummaryQuery { From = Start, To = Start.AddDays(toOffsetDays) });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.To).WithErrorMessage("'To' must be later than 'From'");
    }

    [Fact]
    public void Validate_WhenSameMomentHasDifferentOffsets_HasToError()
    {
        // Arrange: 03:00+03:00 и 00:00 UTC — один и тот же момент
        var query = new IntegrationSummaryQuery
        {
            From = new DateTimeOffset(2026, 10, 21, 3, 0, 0, TimeSpan.FromHours(3)),
            To = Start,
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.To);
    }
}
