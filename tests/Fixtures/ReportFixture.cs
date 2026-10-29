using ContractWatcher.Common.Contracts;
using ContractWatcher.Common.Enums;
using ContractWatcher.Core.Models.Reports;

namespace ContractWatcher.Core.Tests.Fixtures;

public static class ReportFixture
{
    public const string FieldName = "amount";

    public static DateTimeOffset Timestamp => new(2026, 10, 26, 22, 0, 0, TimeSpan.FromHours(3));

    public static ReportRequest CreatePassedReport() =>
        new("payments", Guid.NewGuid(), 1, ValidationStatus.Passed, Timestamp, []);

    public static ReportRequest CreateFailedReport(params ValidationViolation[] violations) =>
        new("payments", Guid.NewGuid(), 1, ValidationStatus.Failed, Timestamp, [.. violations]);

    public static ValidationViolation CreateTypeMismatch(
        string fieldName = FieldName,
        ContractFieldType expectedType = ContractFieldType.Number,
        ContractFieldType? actualType = ContractFieldType.String
    ) =>
        new()
        {
            FieldName = fieldName,
            Type = ViolationType.TypeMismatch,
            ExpectedType = expectedType,
            ActualType = actualType,
        };

    public static ValidationViolation CreateRequiredFieldMissing(
        string fieldName = FieldName,
        ContractFieldType expectedType = ContractFieldType.Number
    ) =>
        new()
        {
            FieldName = fieldName,
            Type = ViolationType.RequiredFieldMissing,
            ExpectedType = expectedType,
        };

    public static ValidationViolation CreateNullNotAllowed(
        string fieldName = FieldName,
        ContractFieldType expectedType = ContractFieldType.Number
    ) =>
        new()
        {
            FieldName = fieldName,
            Type = ViolationType.NullNotAllowed,
            ExpectedType = expectedType,
        };

    public static ContractRule CreateRule(
        string fieldName = FieldName,
        ContractFieldType type = ContractFieldType.Number,
        bool required = true,
        bool nullable = false
    ) =>
        new()
        {
            FieldName = fieldName,
            Type = type,
            Required = required,
            Nullable = nullable,
        };
}
