namespace MsSqlRecordsCompare.Core.Config;

public class ComparisonPair
{
    public required string Scenario { get; init; }
    public required string OldRecordId { get; init; }
    public required string NewRecordId { get; init; }
}
