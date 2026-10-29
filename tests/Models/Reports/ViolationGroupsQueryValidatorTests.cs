using ContractWatcher.Core.Models.Reports;
using FluentValidation.TestHelper;

namespace ContractWatcher.Core.Tests.Models.Reports;

public sealed class ViolationGroupsQueryValidatorTests
{
    private readonly ViolationGroupsQueryValidator _validator = new();

    [Theory]
    [InlineData(0, 1)]
    [InlineData(100, ReportHistoryQuery.MaxLimit)]
    public void Validate_WhenPagingIsInRange_HasNoErrors(int offset, int limit)
    {
        // Act
        var result = _validator.TestValidate(new ViolationGroupsQuery { Offset = offset, Limit = limit });

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOffsetIsNegative_HasOffsetError()
    {
        // Act
        var result = _validator.TestValidate(new ViolationGroupsQuery { Offset = -1 });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Offset);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(ReportHistoryQuery.MaxLimit + 1)]
    public void Validate_WhenLimitIsOutOfRange_HasLimitError(int limit)
    {
        // Act
        var result = _validator.TestValidate(new ViolationGroupsQuery { Limit = limit });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Limit);
    }
}
