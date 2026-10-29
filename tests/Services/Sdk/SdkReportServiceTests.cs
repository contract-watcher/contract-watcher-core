using ContractWatcher.Common.Enums;
using ContractWatcher.Core.Services.Sdk;

// Fixtures
using static ContractWatcher.Core.Tests.Fixtures.ReportFixture;

namespace ContractWatcher.Core.Tests.Services.Sdk;

public sealed class SdkReportServiceTests
{
    [Fact]
    public void CheckViolations_WhenThereAreNoViolations_ReturnsNoErrors()
    {
        // Act
        var errors = SdkReportService.CheckViolations([], [CreateRule()]);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void CheckViolations_WhenTypeMismatchMatchesRule_ReturnsNoErrors()
    {
        // Arrange
        var violation = CreateTypeMismatch(expectedType: ContractFieldType.Number);

        // Act
        var errors = SdkReportService.CheckViolations([violation], [CreateRule(type: ContractFieldType.Number)]);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void CheckViolations_WhenFieldIsNotInVersion_ReturnsFieldNameError()
    {
        // Act
        var errors = SdkReportService.CheckViolations([CreateTypeMismatch(fieldName: "comment")], [CreateRule()]);

        // Assert
        var error = Assert.Single(errors);
        Assert.Equal("Violations[0].FieldName", error.Key);
        Assert.Contains("'comment'", Assert.Single(error.Value));
    }

    [Fact]
    public void CheckViolations_WhenFieldNameDiffersInCase_ReturnsFieldNameError()
    {
        // Act
        var errors = SdkReportService.CheckViolations([CreateTypeMismatch(fieldName: "Amount")], [CreateRule()]);

        // Assert
        Assert.Equal(["Violations[0].FieldName"], errors.Keys);
    }

    [Fact]
    public void CheckViolations_WhenFieldIsNotInVersion_DoesNotCheckTypes()
    {
        // Arrange
        var violation = CreateRequiredFieldMissing(fieldName: "comment", expectedType: ContractFieldType.String);

        // Act
        var errors = SdkReportService.CheckViolations([violation], [CreateRule()]);

        // Assert
        Assert.Equal(["Violations[0].FieldName"], errors.Keys);
    }

    [Fact]
    public void CheckViolations_WhenExpectedTypeDiffersFromRule_ReturnsExpectedTypeError()
    {
        // Arrange
        var violation = CreateTypeMismatch(expectedType: ContractFieldType.String, actualType: ContractFieldType.Number);

        // Act
        var errors = SdkReportService.CheckViolations([violation], [CreateRule(type: ContractFieldType.Number)]);

        // Assert
        var error = Assert.Single(errors);
        Assert.Equal("Violations[0].ExpectedType", error.Key);
        Assert.Contains("Number", Assert.Single(error.Value));
    }

    [Fact]
    public void CheckViolations_WhenRequiredFieldIsMissing_ReturnsNoErrors()
    {
        // Act
        var errors = SdkReportService.CheckViolations([CreateRequiredFieldMissing()], [CreateRule(required: true)]);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void CheckViolations_WhenOptionalFieldIsReportedMissing_ReturnsTypeError()
    {
        // Act
        var errors = SdkReportService.CheckViolations([CreateRequiredFieldMissing()], [CreateRule(required: false)]);

        // Assert
        var error = Assert.Single(errors);
        Assert.Equal("Violations[0].Type", error.Key);
        Assert.Contains("optional", Assert.Single(error.Value));
    }

    [Fact]
    public void CheckViolations_WhenNonNullableFieldIsNull_ReturnsNoErrors()
    {
        // Act
        var errors = SdkReportService.CheckViolations([CreateNullNotAllowed()], [CreateRule(nullable: false)]);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void CheckViolations_WhenNullableFieldIsReportedNull_ReturnsTypeError()
    {
        // Act
        var errors = SdkReportService.CheckViolations([CreateNullNotAllowed()], [CreateRule(nullable: true)]);

        // Assert
        var error = Assert.Single(errors);
        Assert.Equal("Violations[0].Type", error.Key);
        Assert.Contains("nullable", Assert.Single(error.Value));
    }

    [Fact]
    public void CheckViolations_WhenViolationBreaksSeveralRules_ReturnsAllErrors()
    {
        // Arrange
        var violation = CreateRequiredFieldMissing(expectedType: ContractFieldType.String);

        // Act
        var errors = SdkReportService.CheckViolations(
            [violation],
            [CreateRule(type: ContractFieldType.Number, required: false)]
        );

        // Assert
        Assert.Equal(["Violations[0].ExpectedType", "Violations[0].Type"], errors.Keys.Order());
    }

    [Fact]
    public void CheckViolations_WhenOnlySecondViolationIsWrong_ReturnsErrorWithItsIndex()
    {
        // Arrange
        var rules = new[] { CreateRule(), CreateRule(fieldName: "comment", type: ContractFieldType.String, nullable: true) };
        var violations = new[] { CreateTypeMismatch(), CreateNullNotAllowed(fieldName: "comment", ContractFieldType.String) };

        // Act
        var errors = SdkReportService.CheckViolations([.. violations], [.. rules]);

        // Assert
        Assert.Equal(["Violations[1].Type"], errors.Keys);
    }

    [Fact]
    public void CheckViolations_WhenSameFieldIsReportedTwice_ChecksEachViolation()
    {
        // Arrange
        var violations = new[] { CreateTypeMismatch(), CreateTypeMismatch(expectedType: ContractFieldType.Boolean) };

        // Act
        var errors = SdkReportService.CheckViolations([.. violations], [CreateRule()]);

        // Assert
        Assert.Equal(["Violations[1].ExpectedType"], errors.Keys);
    }
}
