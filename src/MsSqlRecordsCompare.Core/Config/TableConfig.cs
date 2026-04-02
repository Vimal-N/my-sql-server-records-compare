namespace MsSqlRecordsCompare.Core.Config;

public class TableConfig
{
    public required string TableName { get; init; }
    public string Schema { get; init; } = "dbo";
    public string? RecordIdColumn { get; init; }
    public List<string> RowMatchColumns { get; init; } = [];
    public string? CustomQuery { get; init; }

    public string FullTableName => $"[{Schema}].[{TableName}]";

    public bool UsesCustomQuery => !string.IsNullOrWhiteSpace(CustomQuery);

    public string GetQuery()
    {
        if (UsesCustomQuery)
            return CustomQuery!;

        if (string.IsNullOrWhiteSpace(RecordIdColumn))
            throw new InvalidOperationException(
                $"Table '{TableName}' must have either RecordIDColumn or CustomQuery configured.");

        return $"SELECT * FROM [{Schema}].[{TableName}] WHERE [{RecordIdColumn}] = @RecordID";
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(TableName))
            throw new ConfigValidationException("TableName cannot be empty.");

        if (string.IsNullOrWhiteSpace(RecordIdColumn) && string.IsNullOrWhiteSpace(CustomQuery))
            throw new ConfigValidationException(
                $"Table '{TableName}' must have either RecordIDColumn or CustomQuery.");

        if (UsesCustomQuery)
        {
            ValidateCustomQuery();
        }
    }

    private void ValidateCustomQuery()
    {
        var query = CustomQuery!.Trim();

        if (!query.Contains("@RecordID", StringComparison.OrdinalIgnoreCase))
            throw new ConfigValidationException(
                $"CustomQuery for table '{TableName}' must contain @RecordID placeholder.");

        var upperQuery = query.ToUpperInvariant();
        string[] blockedKeywords = ["INSERT ", "UPDATE ", "DELETE ", "DROP ", "ALTER ", "TRUNCATE ", "EXEC ", "EXECUTE "];
        foreach (var keyword in blockedKeywords)
        {
            if (upperQuery.Contains(keyword, StringComparison.Ordinal))
                throw new ConfigValidationException(
                    $"CustomQuery for table '{TableName}' must be a SELECT statement. Found blocked keyword: {keyword.Trim()}");
        }

        if (!upperQuery.TrimStart().StartsWith("SELECT", StringComparison.Ordinal))
            throw new ConfigValidationException(
                $"CustomQuery for table '{TableName}' must start with SELECT.");
    }
}
