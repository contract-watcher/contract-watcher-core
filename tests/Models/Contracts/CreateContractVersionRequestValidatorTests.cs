using ContractWatcher.Common.Contracts;
using ContractWatcher.Common.Enums;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.Contracts;
using FluentValidation.TestHelper;

// Fixtures
using static ContractWatcher.Core.Tests.Fixtures.ReportFixture;

namespace ContractWatcher.Core.Tests.Models.Contracts;

public sealed class CreateContractVersionRequestValidatorTests
{
    private readonly CreateContractVersionRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenRequestIsValid_HasNoErrors()
    {
        // Arrange
        var request = new CreateContractVersionRequest(
            [CreateRule(), CreateRule(fieldName: "comment", type: ContractFieldType.String, required: false, nullable: true)],
            "Первая версия"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenRulesAreEmpty_HasRulesError()
    {
        // Act
        var result = _validator.TestValidate(new CreateContractVersionRequest([], null));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Rules);
    }

    [Fact]
    public void Validate_WhenRulesAreMissing_HasRulesError()
    {
        // Act
        var result = _validator.TestValidate(new CreateContractVersionRequest(null!, null));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Rules);
    }

    [Fact]
    public void Validate_WhenRuleIsNull_HasRuleError()
    {
        // Act
        var result = _validator.TestValidate(new CreateContractVersionRequest([CreateRule(), null!], null));

        // Assert
        result.ShouldHaveValidationErrorFor("Rules[1]");
    }

    [Fact]
    public void Validate_WhenFieldNameIsEmpty_HasFieldNameError()
    {
        // Act
        var result = _validator.TestValidate(new CreateContractVersionRequest([CreateRule(fieldName: "")], null));

        // Assert
        result.ShouldHaveValidationErrorFor("Rules[0].FieldName");
    }

    [Fact]
    public void Validate_WhenTypeIsUnknownNumber_HasTypeError()
    {
        // Act
        var result = _validator.TestValidate(
            new CreateContractVersionRequest([CreateRule(type: (ContractFieldType)42)], null)
        );

        // Assert
        result.ShouldHaveValidationErrorFor("Rules[0].Type");
    }

    [Fact]
    public void Validate_WhenFieldNamesRepeat_HasRulesError()
    {
        // Arrange
        List<ContractRule> rules = [CreateRule(), CreateRule(type: ContractFieldType.String)];

        // Act
        var result = _validator.TestValidate(new CreateContractVersionRequest(rules, null));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Rules).WithErrorMessage("Field names must be unique");
    }

    [Fact]
    public void Validate_WhenFieldNamesDifferInCase_HasNoErrors()
    {
        // Arrange
        List<ContractRule> rules = [CreateRule(fieldName: "amount"), CreateRule(fieldName: "Amount")];

        // Act
        var result = _validator.TestValidate(new CreateContractVersionRequest(rules, null));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenCommentIsTooLong_HasCommentError()
    {
        // Arrange
        var request = new CreateContractVersionRequest(
            [CreateRule()],
            new string('a', ContractVersion.CommentMaxLength + 1)
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Comment);
    }
}
