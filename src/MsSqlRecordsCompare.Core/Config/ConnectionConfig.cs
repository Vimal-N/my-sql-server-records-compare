namespace MsSqlRecordsCompare.Core.Config;

public class ConnectionConfig
{
    public required string ServerName { get; init; }
    public required string DatabaseName { get; init; }
    public string? UserName { get; init; }
    public string? Password { get; init; }
    public int CommandTimeout { get; init; } = 120;
    public string? ReportOutputPath { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ServerName))
            throw new ConfigValidationException("ServerName cannot be empty in ConnectionConfig.");

        if (string.IsNullOrWhiteSpace(DatabaseName))
            throw new ConfigValidationException("DatabaseName cannot be empty in ConnectionConfig.");

        if (CommandTimeout <= 0)
            throw new ConfigValidationException("CommandTimeout must be a positive number.");
    }
}
