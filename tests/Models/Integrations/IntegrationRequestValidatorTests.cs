using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.Integrations;
using FluentValidation.TestHelper;

namespace ContractWatcher.Core.Tests.Models.Integrations;

public sealed class IntegrationRequestValidatorTests
{
    private readonly IntegrationRequestValidator _validator = new();

    [Theory]
    [InlineData("yookassa", null)]
    [InlineData("yookassa-2", "Платёжный шлюз")]
    public void Validate_WhenRequestIsValid_HasNoErrors(string slug, string? description)
    {
        // Act
        var result = _validator.TestValidate(new IntegrationRequest(slug, "ЮKassa", description));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("YooKassa")]
    [InlineData("yoo kassa")]
    [InlineData("yoo_kassa")]
    [InlineData("юкасса")]
    public void Validate_WhenSlugIsInvalid_HasSlugError(string slug)
    {
        // Act
        var result = _validator.TestValidate(new IntegrationRequest(slug, "ЮKassa", null));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Validate_WhenSlugIsTooLong_HasSlugError()
    {
        // Arrange
        var request = new IntegrationRequest(new string('a', Integration.SlugMaxLength + 1), "ЮKassa", null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_HasNameError()
    {
        // Act
        var result = _validator.TestValidate(new IntegrationRequest("yookassa", "", null));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameIsTooLong_HasNameError()
    {
        // Arrange
        var request = new IntegrationRequest("yookassa", new string('a', Integration.NameMaxLength + 1), null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenDescriptionIsTooLong_HasDescriptionError()
    {
        // Arrange
        var request = new IntegrationRequest(
            "yookassa",
            "ЮKassa",
            new string('a', Integration.DescriptionMaxLength + 1)
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}
