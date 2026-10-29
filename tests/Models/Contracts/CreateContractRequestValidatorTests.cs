using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.Contracts;
using FluentValidation.TestHelper;

namespace ContractWatcher.Core.Tests.Models.Contracts;

public sealed class CreateContractRequestValidatorTests
{
    private readonly CreateContractRequestValidator _validator = new();

    [Theory]
    [InlineData("payments", null)]
    [InlineData("payments-v2", "Платежи через ЮKassa")]
    [InlineData("0-9", "")]
    public void Validate_WhenRequestIsValid_HasNoErrors(string slug, string? description)
    {
        // Act
        var result = _validator.TestValidate(new CreateContractRequest(slug, "Платежи", description));

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
        var result = _validator.TestValidate(new CreateContractRequest(slug, "Платежи", null));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Validate_WhenSlugIsTooLong_HasSlugError()
    {
        // Arrange
        var request = new CreateContractRequest(new string('a', Contract.SlugMaxLength + 1), "Платежи", null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_HasNameError()
    {
        // Act
        var result = _validator.TestValidate(new CreateContractRequest("payments", " ", null));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameIsTooLong_HasNameError()
    {
        // Arrange
        var request = new CreateContractRequest("payments", new string('a', Contract.NameMaxLength + 1), null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenDescriptionIsTooLong_HasDescriptionError()
    {
        // Arrange
        var request = new CreateContractRequest(
            "payments",
            "Платежи",
            new string('a', Contract.DescriptionMaxLength + 1)
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}
