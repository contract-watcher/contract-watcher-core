using ContractWatcher.Core.Models.Reports;
using FluentValidation.TestHelper;

namespace ContractWatcher.Core.Tests.Models.Reports;

public sealed class ProjectSummaryQueryValidatorTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 21, 0, 0, 0, TimeSpan.Zero);

    private readonly ProjectSummaryQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenQueryIsDefault_HasNoErrors()
    {
        // Act
        var result = _validator.TestValidate(new ProjectSummaryQuery());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenAllParametersAreSet_HasNoErrors()
    {
        // Arrange
        var query = new ProjectSummaryQuery
        {
            From = Start,
            To = Start.AddDays(1),
            OnlyFailed = true,
            Offset = 100,
            Limit = ReportHistoryQuery.MaxLimit,
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIntegrationIdsAreAtLimit_HasNoErrors()
    {
        // Arrange
        var query = new ProjectSummaryQuery
        {
            IntegrationId = [.. Enumerable.Range(0, ProjectSummaryQuery.MaxIntegrationIds).Select(_ => Guid.NewGuid())],
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIntegrationIdsExceedLimit_HasIntegrationIdError()
    {
        // Arrange
        var query = new ProjectSummaryQuery
        {
            IntegrationId = [.. Enumerable.Range(0, ProjectSummaryQuery.MaxIntegrationIds + 1).Select(_ => Guid.NewGuid())],
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.IntegrationId)
            .WithErrorMessage($"At most {ProjectSummaryQuery.MaxIntegrationIds} 'integrationId' values are allowed");
    }

    [Fact]
    public void Validate_WhenOnlyOneBoundIsSet_HasNoErrors()
    {
        // Act
        var fromOnly = _validator.TestValidate(new ProjectSummaryQuery { From = Start });
        var toOnly = _validator.TestValidate(new ProjectSummaryQuery { To = Start });

        // Assert
        fromOnly.ShouldNotHaveAnyValidationErrors();
        toOnly.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenToIsNotLaterThanFrom_HasToError(int toOffsetDays)
    {
        // Act
        var result = _validator.TestValidate(new ProjectSummaryQuery { From = Start, To = Start.AddDays(toOffsetDays) });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.To).WithErrorMessage("'To' must be later than 'From'");
    }

    [Fact]
    public void Validate_WhenBoundsAreSameMomentInDifferentOffsets_HasToError()
    {
        // Arrange
        var query = new ProjectSummaryQuery { From = Start, To = Start.ToOffset(TimeSpan.FromHours(3)) };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.To);
    }

    [Fact]
    public void Validate_WhenOffsetIsNegative_HasOffsetError()
    {
        // Act
        var result = _validator.TestValidate(new ProjectSummaryQuery { Offset = -1 });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Offset);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(ReportHistoryQuery.MaxLimit + 1)]
    public void Validate_WhenLimitIsOutOfRange_HasLimitError(int limit)
    {
        // Act
        var result = _validator.TestValidate(new ProjectSummaryQuery { Limit = limit });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Limit);
    }
}
