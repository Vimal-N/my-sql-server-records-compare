namespace MsSqlRecordsCompare.Core.Comparison.Models;

public class ColumnMismatch
{
    public required string ColumnName { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public required string CompareRule { get; init; }
    public string? Tolerance { get; init; }
}
