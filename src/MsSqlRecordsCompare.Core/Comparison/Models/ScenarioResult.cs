namespace MsSqlRecordsCompare.Core.Comparison.Models;

public class ScenarioResult
{
    public required string Scenario { get; init; }
    public required string OldRecordId { get; init; }
    public required string NewRecordId { get; init; }
    public List<TableResult> TableResults { get; init; } = [];

    public bool Passed => TableResults.All(t => !t.HasMismatches && t.Error == null);
    public int TotalMismatches => TableResults.Sum(t => t.MismatchCount);
}
