using ContractWatcher.Common.Enums;
using ContractWatcher.Core.Models.Reports;
using FluentValidation.TestHelper;

namespace ContractWatcher.Core.Tests.Models.Reports;

public sealed class ReportHistoryQueryValidatorTests
{
    private readonly ReportHistoryQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenQueryIsDefault_HasNoErrors()
    {
        // Act
        var result = _validator.TestValidate(new ReportHistoryQuery());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenAllFiltersAreSet_HasNoErrors()
    {
        // Arrange
        var query = new ReportHistoryQuery
        {
            ContractId = [Guid.NewGuid(), Guid.NewGuid()],
            From = new DateTimeOffset(2026, 10, 21, 0, 0, 0, TimeSpan.Zero),
            To = new DateTimeOffset(2026, 10, 22, 0, 0, 0, TimeSpan.Zero),
            Status = ValidationStatus.Failed,
            Offset = 40,
            Limit = ReportHistoryQuery.MaxLimit,
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenContractIdHasMaxValues_HasNoErrors()
    {
        // Arrange
        var query = new ReportHistoryQuery
        {
            ContractId = [.. Enumerable.Range(0, ReportHistoryQuery.MaxContractIds).Select(_ => Guid.NewGuid())],
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenContractIdHasTooManyValues_HasContractIdError()
    {
        // Arrange
        var query = new ReportHistoryQuery
        {
            ContractId = [.. Enumerable.Range(0, ReportHistoryQuery.MaxContractIds + 1).Select(_ => Guid.NewGuid())],
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ContractId);
    }

    [Fact]
    public void Validate_WhenRepeatedContractIdExceedsMax_HasContractIdError()
    {
        // Arrange: Monitoring считает вхождения до удаления повторов, поэтому 51 одинаковый ID — тоже ошибка
        var contractId = Guid.NewGuid();
        var query = new ReportHistoryQuery
        {
            ContractId = [.. Enumerable.Repeat(contractId, ReportHistoryQuery.MaxContractIds + 1)],
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ContractId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    public void Validate_WhenToIsNotLaterThanFrom_HasToError(int toOffsetMinutes)
    {
        // Arrange
        var from = new DateTimeOffset(2026, 10, 21, 0, 0, 0, TimeSpan.Zero);
        var query = new ReportHistoryQuery { From = from, To = from.AddMinutes(toOffsetMinutes) };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.To);
    }

    [Fact]
    public void Validate_WhenBoundsHaveDifferentOffsets_ComparesMoments()
    {
        // Arrange: 03:00+03:00 — это 00:00 UTC, то есть раньше 01:00 UTC
        var query = new ReportHistoryQuery
        {
            From = new DateTimeOffset(2026, 10, 21, 3, 0, 0, TimeSpan.FromHours(3)),
            To = new DateTimeOffset(2026, 10, 21, 1, 0, 0, TimeSpan.Zero),
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenStatusIsUnknownNumber_HasStatusError()
    {
        // Act
        var result = _validator.TestValidate(new ReportHistoryQuery { Status = (ValidationStatus)7 });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Validate_WhenOffsetIsNegative_HasOffsetError()
    {
        // Act
        var result = _validator.TestValidate(new ReportHistoryQuery { Offset = -1 });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Offset);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(ReportHistoryQuery.MaxLimit + 1)]
    public void Validate_WhenLimitIsOutOfRange_HasLimitError(int limit)
    {
        // Act
        var result = _validator.TestValidate(new ReportHistoryQuery { Limit = limit });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Limit);
    }
}
