using ContractWatcher.Common.Enums;
using ContractWatcher.Core.Services.Sdk;

// Fixtures
using static ContractWatcher.Core.Tests.Fixtures.ReportFixture;

namespace ContractWatcher.Core.Tests.Services.Sdk;

public sealed class SdkContractServiceTests
{
    [Fact]
    public void HasSameRules_WhenRulesAreEqual_ReturnsTrue()
    {
        // Act
        var same = SdkContractService.HasSameRules([CreateRule()], [CreateRule()]);

        // Assert
        Assert.True(same);
    }

    [Fact]
    public void HasSameRules_WhenOnlyOrderDiffers_ReturnsTrue()
    {
        // Arrange
        var amount = CreateRule();
        var comment = CreateRule(fieldName: "comment", type: ContractFieldType.String);

        // Act
        var same = SdkContractService.HasSameRules([amount, comment], [comment, amount]);

        // Assert
        Assert.True(same);
    }

    [Fact]
    public void HasSameRules_WhenFieldIsAdded_ReturnsFalse()
    {
        // Act
        var same = SdkContractService.HasSameRules(
            [CreateRule()],
            [CreateRule(), CreateRule(fieldName: "comment", type: ContractFieldType.String)]
        );

        // Assert
        Assert.False(same);
    }

    [Fact]
    public void HasSameRules_WhenFieldIsRemoved_ReturnsFalse()
    {
        // Act
        var same = SdkContractService.HasSameRules(
            [CreateRule(), CreateRule(fieldName: "comment", type: ContractFieldType.String)],
            [CreateRule()]
        );

        // Assert
        Assert.False(same);
    }

    [Fact]
    public void HasSameRules_WhenFieldIsRenamed_ReturnsFalse()
    {
        // Act
        var same = SdkContractService.HasSameRules([CreateRule()], [CreateRule(fieldName: "sum")]);

        // Assert
        Assert.False(same);
    }

    [Fact]
    public void HasSameRules_WhenFieldNameCaseDiffers_ReturnsFalse()
    {
        // Act
        var same = SdkContractService.HasSameRules([CreateRule(fieldName: "amount")], [CreateRule(fieldName: "Amount")]);

        // Assert
        Assert.False(same);
    }

    [Fact]
    public void HasSameRules_WhenTypeDiffers_ReturnsFalse()
    {
        // Act
        var same = SdkContractService.HasSameRules([CreateRule()], [CreateRule(type: ContractFieldType.String)]);

        // Assert
        Assert.False(same);
    }

    [Fact]
    public void HasSameRules_WhenRequiredDiffers_ReturnsFalse()
    {
        // Act
        var same = SdkContractService.HasSameRules([CreateRule(required: true)], [CreateRule(required: false)]);

        // Assert
        Assert.False(same);
    }

    [Fact]
    public void HasSameRules_WhenNullableDiffers_ReturnsFalse()
    {
        // Act
        var same = SdkContractService.HasSameRules([CreateRule(nullable: false)], [CreateRule(nullable: true)]);

        // Assert
        Assert.False(same);
    }
}
