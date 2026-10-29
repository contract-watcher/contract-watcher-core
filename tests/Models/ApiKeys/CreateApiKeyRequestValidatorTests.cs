using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.ApiKeys;
using FluentValidation.TestHelper;

namespace ContractWatcher.Core.Tests.Models.ApiKeys;

public sealed class CreateApiKeyRequestValidatorTests
{
    private readonly CreateApiKeyRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenNameIsSet_HasNoErrors()
    {
        // Act
        var result = _validator.TestValidate(new CreateApiKeyRequest("prod"));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameIsEmpty_HasNameError(string name)
    {
        // Act
        var result = _validator.TestValidate(new CreateApiKeyRequest(name));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameIsTooLong_HasNameError()
    {
        // Act
        var result = _validator.TestValidate(new CreateApiKeyRequest(new string('a', ApiKey.NameMaxLength + 1)));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
