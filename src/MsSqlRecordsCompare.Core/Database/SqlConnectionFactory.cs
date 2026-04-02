using Microsoft.Data.SqlClient;

namespace MsSqlRecordsCompare.Core.Database;

public class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string serverName, string databaseName, int commandTimeout = 120,
        string? userName = null, string? password = null)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = serverName,
            InitialCatalog = databaseName,
            TrustServerCertificate = true,
            CommandTimeout = commandTimeout
        };

        if (!string.IsNullOrEmpty(userName))
        {
            builder.UserID = userName;
            builder.Password = password ?? string.Empty;
            builder.IntegratedSecurity = false;
        }
        else
        {
            builder.IntegratedSecurity = true;
        }

        _connectionString = builder.ConnectionString;
        CommandTimeout = commandTimeout;
    }

    public int CommandTimeout { get; }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task<string> TestConnectionAsync()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var command = new SqlCommand("SELECT SUSER_SNAME()", connection);
        var userName = await command.ExecuteScalarAsync();
        return userName?.ToString() ?? "Unknown";
    }
}
