namespace MsSqlRecordsCompare.Core.Comparison.Models;

public class TableResult
{
    public required string TableName { get; init; }
    public required string Schema { get; init; }
    public int OldRowCount { get; init; }
    public int NewRowCount { get; init; }
    public List<RowComparisonResult> RowResults { get; init; } = [];
    public List<UnmatchedRow> UnmatchedOldRows { get; init; } = [];
    public List<UnmatchedRow> UnmatchedNewRows { get; init; } = [];
    public List<ExcludedColumn> ExcludedColumns { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public string? Error { get; init; }

    public bool HasMismatches => RowResults.Any(r => r.Mismatches.Count > 0) ||
                                  UnmatchedOldRows.Count > 0 ||
                                  UnmatchedNewRows.Count > 0;

    public int MismatchCount => RowResults.Sum(r => r.Mismatches.Count) +
                                 UnmatchedOldRows.Count +
                                 UnmatchedNewRows.Count;
}

public class RowComparisonResult
{
    public string? MatchKey { get; init; }
    public List<ColumnMismatch> Mismatches { get; init; } = [];
    public List<ColumnMatch> Matches { get; init; } = [];
}

public class ColumnMatch
{
    public required string ColumnName { get; init; }
    public string? Value { get; init; }
    public required string CompareRule { get; init; }
}

public class UnmatchedRow
{
    public required string Description { get; init; }
    public Dictionary<string, object?> Columns { get; init; } = new();
}

public class ExcludedColumn
{
    public required string ColumnName { get; init; }
    public string? Reason { get; init; }
}
