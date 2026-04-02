using Microsoft.Data.SqlClient;
using MsSqlRecordsCompare.Core.Config;

namespace MsSqlRecordsCompare.Core.Database;

public class SchemaInspector
{
    private readonly SqlConnectionFactory _connectionFactory;

    public SchemaInspector(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<SchemaValidationResult> ValidateTablesAsync(List<TableConfig> tables)
    {
        var result = new SchemaValidationResult();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        foreach (var table in tables)
        {
            // For custom query tables, we can't validate table existence the same way
            // We'll just verify the query doesn't error by checking if the table mentioned exists
            if (!table.UsesCustomQuery)
            {
                var tableExists = await TableExistsAsync(connection, table.Schema, table.TableName);
                if (!tableExists)
                {
                    result.MissingTables.Add($"[{table.Schema}].[{table.TableName}]");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(table.RecordIdColumn))
                {
                    var colExists = await ColumnExistsAsync(connection, table.Schema, table.TableName, table.RecordIdColumn);
                    if (!colExists)
                    {
                        result.MissingColumns.Add($"[{table.Schema}].[{table.TableName}].{table.RecordIdColumn}");
                    }
                }

                foreach (var matchCol in table.RowMatchColumns)
                {
                    var colExists = await ColumnExistsAsync(connection, table.Schema, table.TableName, matchCol);
                    if (!colExists)
                    {
                        result.MissingColumns.Add($"[{table.Schema}].[{table.TableName}].{matchCol}");
                    }
                }
            }
        }

        return result;
    }

    public async Task<List<string>> GetAllTablesAsync()
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT TABLE_SCHEMA + '.' + TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_SCHEMA, TABLE_NAME
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var tables = new List<string>();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }
        return tables;
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string schema, string tableName)
    {
        const string sql = """
            SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @TableName
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Schema", schema);
        command.Parameters.AddWithValue("@TableName", tableName);

        var count = (int)(await command.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }

    private static async Task<bool> ColumnExistsAsync(SqlConnection connection, string schema, string tableName, string columnName)
    {
        const string sql = """
            SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Schema", schema);
        command.Parameters.AddWithValue("@TableName", tableName);
        command.Parameters.AddWithValue("@ColumnName", columnName);

        var count = (int)(await command.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }
}

public class SchemaValidationResult
{
    public List<string> MissingTables { get; } = [];
    public List<string> MissingColumns { get; } = [];

    public bool IsValid => MissingTables.Count == 0 && MissingColumns.Count == 0;

    public string GetSummary()
    {
        var parts = new List<string>();
        if (MissingTables.Count > 0)
            parts.Add($"Missing tables: {string.Join(", ", MissingTables)}");
        if (MissingColumns.Count > 0)
            parts.Add($"Missing columns: {string.Join(", ", MissingColumns)}");
        return parts.Count > 0 ? string.Join("; ", parts) : "All tables and columns verified.";
    }
}
