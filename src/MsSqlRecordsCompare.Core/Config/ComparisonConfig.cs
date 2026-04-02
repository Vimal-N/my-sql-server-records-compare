namespace MsSqlRecordsCompare.Core.Config;

public class ComparisonConfig
{
    public required ConnectionConfig Connection { get; init; }
    public required string SelectedTableSet { get; init; }
    public required List<TableConfig> Tables { get; init; }
    public List<ExclusionConfig> Exclusions { get; init; } = [];
    public List<ColumnRuleConfig> ColumnRules { get; init; } = [];
    public List<ComparisonPair> ComparisonPairs { get; init; } = [];
    public List<string> AvailableTableSets { get; init; } = [];

    public bool IsColumnExcluded(string tableName, string columnName)
    {
        return Exclusions.Any(e =>
            e.AppliesToTable(tableName) &&
            e.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));
    }

    public string GetCompareRule(string tableName, string columnName)
    {
        // 1. Specific table + specific column
        var rule = ColumnRules.FirstOrDefault(r =>
            !r.IsTableWildcard && !r.IsColumnWildcard &&
            r.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase) &&
            r.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));

        // 2. Wildcard table + specific column
        rule ??= ColumnRules.FirstOrDefault(r =>
            r.IsTableWildcard && !r.IsColumnWildcard &&
            r.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));

        // 3. Specific table + wildcard column
        rule ??= ColumnRules.FirstOrDefault(r =>
            !r.IsTableWildcard && r.IsColumnWildcard &&
            r.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));

        // 4. Default
        return rule?.CompareRule ?? "exact";
    }

    public string? GetTolerance(string tableName, string columnName)
    {
        var rule = ColumnRules.FirstOrDefault(r =>
            (r.IsTableWildcard || r.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)) &&
            (r.IsColumnWildcard || r.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase)));

        return rule?.Tolerance;
    }

    public List<ExclusionConfig> GetExclusionsForTable(string tableName)
    {
        return Exclusions.Where(e => e.AppliesToTable(tableName)).ToList();
    }

    public void Validate()
    {
        Connection.Validate();

        if (Tables.Count == 0)
            throw new ConfigValidationException(
                $"No tables configured in table set '{SelectedTableSet}'.");

        foreach (var table in Tables)
            table.Validate();

        foreach (var rule in ColumnRules)
            rule.Validate();
    }
}
