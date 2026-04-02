namespace MsSqlRecordsCompare.Core.Config;

public class ExclusionConfig
{
    public required string TableName { get; init; }
    public required string ColumnName { get; init; }
    public string? Reason { get; init; }

    public bool IsWildcard => TableName == "*";

    public bool AppliesToTable(string tableName)
    {
        return IsWildcard || TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase);
    }
}
