namespace MsSqlRecordsCompare.Core.Comparison.Models;

public class ComparisonResult
{
    public required string ConfigFile { get; init; }
    public required string TableSet { get; init; }
    public required string ServerName { get; init; }
    public required string DatabaseName { get; init; }
    public required string UserName { get; init; }
    public required DateTime RunTimestamp { get; init; }
    public List<ScenarioResult> Scenarios { get; init; } = [];

    public int TotalScenarios => Scenarios.Count;
    public int PassedScenarios => Scenarios.Count(s => s.Passed);
    public int FailedScenarios => Scenarios.Count(s => !s.Passed);
    public int TotalMismatches => Scenarios.Sum(s => s.TotalMismatches);
}
