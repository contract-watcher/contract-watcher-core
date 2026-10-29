using ContractWatcher.Core.Models.Notifications;
using FluentValidation.TestHelper;

namespace ContractWatcher.Core.Tests.Models.Notifications;

public sealed class NotificationSettingsRequestValidatorTests
{
    private readonly NotificationSettingsRequestValidator _validator = new();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_WhenEmailEnabledIsSet_HasNoErrors(bool emailEnabled)
    {
        // Act
        var result = _validator.TestValidate(new NotificationSettingsRequest(emailEnabled));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenEmailEnabledIsMissing_HasEmailEnabledError()
    {
        // Arrange: без поля в запросе письма не должны выключиться молча
        var request = new NotificationSettingsRequest(null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EmailEnabled);
    }
}
