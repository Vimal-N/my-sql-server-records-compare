namespace MsSqlRecordsCompare.Core.Database.Models;

public class TableRecord
{
    public required string TableName { get; init; }
    public required string RecordId { get; init; }
    public Dictionary<string, object?> Columns { get; init; } = new();
    public string? RowMatchKey { get; set; }
}
