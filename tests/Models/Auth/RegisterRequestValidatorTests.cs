using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.Auth;
using FluentValidation.TestHelper;

namespace ContractWatcher.Core.Tests.Models.Auth;

public sealed class RegisterRequestValidatorTests
{
    private const string Email = "owner@test.local";
    private const string Password = "Password123!";
    private const string Name = "Амир";

    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenRequestIsValid_HasNoErrors()
    {
        // Act
        var result = _validator.TestValidate(new RegisterRequest(Email, Password, Name));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("owner")]
    [InlineData("owner.test.local")]
    public void Validate_WhenEmailIsInvalid_HasEmailError(string email)
    {
        // Act
        var result = _validator.TestValidate(new RegisterRequest(email, Password, Name));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WhenEmailIsTooLong_HasEmailError()
    {
        // Arrange
        var email = new string('a', User.EmailMaxLength) + "@test.local";

        // Act
        var result = _validator.TestValidate(new RegisterRequest(email, Password, Name));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData(RegisterRequestValidator.PasswordMinLength)]
    [InlineData(RegisterRequestValidator.PasswordMaxLength)]
    public void Validate_WhenPasswordLengthIsOnBoundary_HasNoErrors(int length)
    {
        // Act
        var result = _validator.TestValidate(new RegisterRequest(Email, new string('a', length), Name));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(RegisterRequestValidator.PasswordMinLength - 1)]
    [InlineData(RegisterRequestValidator.PasswordMaxLength + 1)]
    public void Validate_WhenPasswordLengthIsOutOfRange_HasPasswordError(int length)
    {
        // Act
        var result = _validator.TestValidate(new RegisterRequest(Email, new string('a', length), Name));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_HasNameError()
    {
        // Act
        var result = _validator.TestValidate(new RegisterRequest(Email, Password, ""));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameIsTooLong_HasNameError()
    {
        // Arrange
        var name = new string('a', User.NameMaxLength + 1);

        // Act
        var result = _validator.TestValidate(new RegisterRequest(Email, Password, name));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
