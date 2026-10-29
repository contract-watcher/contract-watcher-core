using ContractWatcher.Core.Models.Auth;
using FluentValidation.TestHelper;

namespace ContractWatcher.Core.Tests.Models.Auth;

/// <summary>
/// Вход, обновление токенов и подтверждение email: обязательные поля без правил формата
/// </summary>
public sealed class AuthRequestValidatorTests
{
    [Fact]
    public void LoginValidate_WhenFieldsAreSet_HasNoErrors()
    {
        // Act
        var result = new LoginRequestValidator().TestValidate(new LoginRequest("owner@test.local", "short"));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LoginValidate_WhenFieldsAreEmpty_HasEmailAndPasswordErrors()
    {
        // Act
        var result = new LoginRequestValidator().TestValidate(new LoginRequest("", ""));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void RefreshTokenValidate_WhenTokenIsSet_HasNoErrors()
    {
        // Act
        var result = new RefreshTokenRequestValidator().TestValidate(new RefreshTokenRequest("token"));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RefreshTokenValidate_WhenTokenIsEmpty_HasRefreshTokenError()
    {
        // Act
        var result = new RefreshTokenRequestValidator().TestValidate(new RefreshTokenRequest(""));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }

    [Fact]
    public void ConfirmEmailValidate_WhenTokenIsSet_HasNoErrors()
    {
        // Act
        var result = new ConfirmEmailRequestValidator().TestValidate(new ConfirmEmailRequest(new string('a', 100)));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void ConfirmEmailValidate_WhenTokenLengthIsInvalid_HasTokenError(int length)
    {
        // Act
        var result = new ConfirmEmailRequestValidator().TestValidate(new ConfirmEmailRequest(new string('a', length)));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }
}
