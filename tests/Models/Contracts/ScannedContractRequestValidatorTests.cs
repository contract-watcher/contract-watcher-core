using ContractWatcher.Common.Contracts;
using ContractWatcher.Common.Enums;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.Contracts;
using FluentValidation.TestHelper;

// Fixtures
using static ContractWatcher.Core.Tests.Fixtures.ReportFixture;

namespace ContractWatcher.Core.Tests.Models.Contracts;

public sealed class ScannedContractRequestValidatorTests
{
    private const string Slug = "payments";

    private readonly ScannedContractRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenRequestIsValid_HasNoErrors()
    {
        // Arrange
        var request = new ScannedContractRequest(
            Slug,
            [CreateRule(), CreateRule(fieldName: "comment", type: ContractFieldType.String, required: false, nullable: true)]
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Payments")]
    [InlineData("pay ments")]
    [InlineData("pay_ments")]
    [InlineData("платежи")]
    public void Validate_WhenSlugIsInvalid_HasSlugError(string slug)
    {
        // Act
        var result = _validator.TestValidate(new ScannedContractRequest(slug, [CreateRule()]));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Validate_WhenSlugIsTooLong_HasSlugError()
    {
        // Arrange
        var request = new ScannedContractRequest(new string('a', Contract.SlugMaxLength + 1), [CreateRule()]);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Validate_WhenRulesAreEmpty_HasRulesError()
    {
        // Act
        var result = _validator.TestValidate(new ScannedContractRequest(Slug, []));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Rules);
    }

    [Fact]
    public void Validate_WhenRulesAreMissing_HasRulesError()
    {
        // Act
        var result = _validator.TestValidate(new ScannedContractRequest(Slug, null!));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Rules);
    }

    [Fact]
    public void Validate_WhenRuleIsNull_HasRuleError()
    {
        // Act
        var result = _validator.TestValidate(new ScannedContractRequest(Slug, [CreateRule(), null!]));

        // Assert
        result.ShouldHaveValidationErrorFor("Rules[1]");
    }

    [Fact]
    public void Validate_WhenFieldNameIsEmpty_HasFieldNameError()
    {
        // Act
        var result = _validator.TestValidate(new ScannedContractRequest(Slug, [CreateRule(fieldName: "")]));

        // Assert
        result.ShouldHaveValidationErrorFor("Rules[0].FieldName");
    }

    [Fact]
    public void Validate_WhenTypeIsUnknownNumber_HasTypeError()
    {
        // Act
        var result = _validator.TestValidate(
            new ScannedContractRequest(Slug, [CreateRule(type: (ContractFieldType)42)])
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
        var result = _validator.TestValidate(new ScannedContractRequest(Slug, rules));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Rules).WithErrorMessage("Field names must be unique");
    }
}
