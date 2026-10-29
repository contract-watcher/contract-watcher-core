using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.Projects;
using FluentValidation.TestHelper;

namespace ContractWatcher.Core.Tests.Models.Projects;

public sealed class CreateProjectRequestValidatorTests
{
    private readonly CreateProjectRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenNameIsSet_HasNoErrors()
    {
        // Act
        var result = _validator.TestValidate(new CreateProjectRequest("Магазин"));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameIsEmpty_HasNameError(string name)
    {
        // Act
        var result = _validator.TestValidate(new CreateProjectRequest(name));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameIsTooLong_HasNameError()
    {
        // Act
        var result = _validator.TestValidate(new CreateProjectRequest(new string('a', Project.NameMaxLength + 1)));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
