using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.Contracts;
using ContractWatcher.Core.Models.Reports;
using FluentValidation.TestHelper;

namespace ContractWatcher.Core.Tests.Models.Contracts;

public sealed class ContractSearchQueryValidatorTests
{
    private readonly ContractSearchQueryValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("pay")]
    public void Validate_WhenSearchIsShort_HasNoErrors(string? search)
    {
        // Act
        var result = _validator.TestValidate(new ContractSearchQuery { Search = search });

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenSearchHasMaxNameLength_HasNoErrors()
    {
        // Act
        var result = _validator.TestValidate(new ContractSearchQuery { Search = new string('a', Contract.NameMaxLength) });

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenSearchIsLongerThanName_HasSearchError()
    {
        // Arrange
        var query = new ContractSearchQuery { Search = new string('a', Contract.NameMaxLength + 1) };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Search);
    }

    [Fact]
    public void Validate_WhenOffsetIsNegative_HasOffsetError()
    {
        // Act
        var result = _validator.TestValidate(new ContractSearchQuery { Offset = -1 });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Offset);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(ReportHistoryQuery.MaxLimit + 1)]
    public void Validate_WhenLimitIsOutOfRange_HasLimitError(int limit)
    {
        // Act
        var result = _validator.TestValidate(new ContractSearchQuery { Limit = limit });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Limit);
    }
}
