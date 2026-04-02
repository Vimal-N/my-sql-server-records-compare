using MsSqlRecordsCompare.Core.Database.Models;

namespace MsSqlRecordsCompare.Core.Comparison.Models;

public class RowMatchResult
{
    public List<MatchedRowPair> MatchedPairs { get; init; } = [];
    public List<TableRecord> UnmatchedOldRows { get; init; } = [];
    public List<TableRecord> UnmatchedNewRows { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
}

public class MatchedRowPair
{
    public required TableRecord OldRow { get; init; }
    public required TableRecord NewRow { get; init; }
    public string? MatchKey { get; init; }
}
