namespace MsSqlRecordsCompare.Core.Config;

public class ColumnRuleConfig
{
    public required string TableName { get; init; }
    public required string ColumnName { get; init; }
    public required string CompareRule { get; init; }
    public string? Tolerance { get; init; }

    public bool IsTableWildcard => TableName == "*";
    public bool IsColumnWildcard => ColumnName == "*";

    public static readonly string[] ValidRules =
    [
        "exact", "exact-ci", "currency", "date", "datetime",
        "numeric", "percentage", "fuzzy", "boolean", "contains", "ignore"
    ];

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(CompareRule))
            throw new ConfigValidationException(
                $"CompareRule cannot be empty for {TableName}.{ColumnName}.");

        if (!ValidRules.Contains(CompareRule.ToLowerInvariant()))
            throw new ConfigValidationException(
                $"Invalid CompareRule '{CompareRule}' for {TableName}.{ColumnName}. " +
                $"Valid rules: {string.Join(", ", ValidRules)}");
    }
}
