using ContractWatcher.Common.Contracts;
using ContractWatcher.Common.Enums;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.Reports;
using FluentValidation.TestHelper;

// Fixtures
using static ContractWatcher.Core.Tests.Fixtures.ReportFixture;

namespace ContractWatcher.Core.Tests.Models.Reports;

public sealed class ReportRequestValidatorTests
{
    private readonly ReportRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenPassedReportIsValid_HasNoErrors()
    {
        // Act
        var result = _validator.TestValidate(CreatePassedReport());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenFailedReportIsValid_HasNoErrors()
    {
        // Arrange
        var report = CreateFailedReport(CreateTypeMismatch(), CreateRequiredFieldMissing(), CreateNullNotAllowed());

        // Act
        var result = _validator.TestValidate(report);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Payments")]
    [InlineData("pay ments")]
    [InlineData("платежи")]
    [InlineData("pay_ments")]
    public void Validate_WhenSlugIsInvalid_HasSlugError(string slug)
    {
        // Act
        var result = _validator.TestValidate(CreatePassedReport() with { Slug = slug });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Validate_WhenSlugIsTooLong_HasSlugError()
    {
        // Arrange
        var report = CreatePassedReport() with { Slug = new string('a', Contract.SlugMaxLength + 1) };

        // Act
        var result = _validator.TestValidate(report);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Validate_WhenReportIdIsEmpty_HasReportIdError()
    {
        // Act
        var result = _validator.TestValidate(CreatePassedReport() with { ReportId = Guid.Empty });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReportId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenContractVersionIsNotPositive_HasContractVersionError(int version)
    {
        // Act
        var result = _validator.TestValidate(CreatePassedReport() with { ContractVersion = version });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ContractVersion);
    }

    [Fact]
    public void Validate_WhenStatusIsMissing_HasStatusError()
    {
        // Act
        var result = _validator.TestValidate(CreatePassedReport() with { Status = null });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Validate_WhenStatusIsUnknownNumber_HasStatusError()
    {
        // Act
        var result = _validator.TestValidate(CreatePassedReport() with { Status = (ValidationStatus)7 });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Validate_WhenTimestampIsMissing_HasTimestampError()
    {
        // Act
        var result = _validator.TestValidate(CreatePassedReport() with { Timestamp = null });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Timestamp);
    }

    [Fact]
    public void Validate_WhenTimestampIsDefault_HasTimestampError()
    {
        // Arrange: SDK, который не заполнил время, присылает 0001-01-01, а не null
        var report = CreatePassedReport() with { Timestamp = default(DateTimeOffset) };

        // Act
        var result = _validator.TestValidate(report);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Timestamp).WithErrorMessage("Timestamp must be set");
    }

    [Fact]
    public void Validate_WhenViolationsAreMissing_HasViolationsError()
    {
        // Act
        var result = _validator.TestValidate(CreateFailedReport() with { Violations = null! });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Violations);
    }

    [Fact]
    public void Validate_WhenPassedReportHasViolations_HasViolationsError()
    {
        // Arrange
        var report = CreatePassedReport() with { Violations = [CreateTypeMismatch()] };

        // Act
        var result = _validator.TestValidate(report);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Violations)
            .WithErrorMessage("Passed report must not contain violations");
    }

    [Fact]
    public void Validate_WhenFailedReportHasNoViolations_HasViolationsError()
    {
        // Act
        var result = _validator.TestValidate(CreateFailedReport());

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Violations)
            .WithErrorMessage("Failed report must contain at least one violation");
    }

    [Fact]
    public void Validate_WhenViolationIsNull_HasViolationError()
    {
        // Act
        var result = _validator.TestValidate(CreateFailedReport() with { Violations = [null!] });

        // Assert
        result.ShouldHaveValidationErrorFor("Violations[0]");
    }

    [Fact]
    public void Validate_WhenViolationFieldNameIsEmpty_HasFieldNameError()
    {
        // Act
        var result = _validator.TestValidate(CreateFailedReport(CreateTypeMismatch(fieldName: "")));

        // Assert
        result.ShouldHaveValidationErrorFor("Violations[0].FieldName");
    }

    [Fact]
    public void Validate_WhenViolationTypeIsUnknownNumber_HasTypeError()
    {
        // Arrange
        var violation = new ValidationViolation
        {
            FieldName = FieldName,
            Type = (ViolationType)9,
            ExpectedType = ContractFieldType.Number,
        };

        // Act
        var result = _validator.TestValidate(CreateFailedReport(violation));

        // Assert
        result.ShouldHaveValidationErrorFor("Violations[0].Type");
    }

    [Fact]
    public void Validate_WhenExpectedTypeIsMissing_HasExpectedTypeError()
    {
        // Arrange
        var violation = new ValidationViolation { FieldName = FieldName, Type = ViolationType.RequiredFieldMissing };

        // Act
        var result = _validator.TestValidate(CreateFailedReport(violation));

        // Assert
        result.ShouldHaveValidationErrorFor("Violations[0].ExpectedType");
    }

    [Fact]
    public void Validate_WhenTypeMismatchHasNoActualType_HasActualTypeError()
    {
        // Act
        var result = _validator.TestValidate(CreateFailedReport(CreateTypeMismatch(actualType: null)));

        // Assert
        result.ShouldHaveValidationErrorFor("Violations[0].ActualType");
    }

    [Fact]
    public void Validate_WhenTypeMismatchHasSameActualType_HasActualTypeError()
    {
        // Arrange
        var violation = CreateTypeMismatch(expectedType: ContractFieldType.Number, actualType: ContractFieldType.Number);

        // Act
        var result = _validator.TestValidate(CreateFailedReport(violation));

        // Assert
        result
            .ShouldHaveValidationErrorFor("Violations[0].ActualType")
            .WithErrorMessage("Actual type must differ from expected type");
    }

    [Theory]
    [InlineData(ViolationType.RequiredFieldMissing)]
    [InlineData(ViolationType.NullNotAllowed)]
    public void Validate_WhenNotTypeMismatchHasActualType_HasActualTypeError(ViolationType type)
    {
        // Arrange
        var violation = new ValidationViolation
        {
            FieldName = FieldName,
            Type = type,
            ExpectedType = ContractFieldType.Number,
            ActualType = ContractFieldType.String,
        };

        // Act
        var result = _validator.TestValidate(CreateFailedReport(violation));

        // Assert
        result
            .ShouldHaveValidationErrorFor("Violations[0].ActualType")
            .WithErrorMessage("Actual type is allowed only for TypeMismatch");
    }
}
