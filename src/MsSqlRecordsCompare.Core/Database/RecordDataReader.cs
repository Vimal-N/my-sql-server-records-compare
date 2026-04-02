using Microsoft.Data.SqlClient;
using MsSqlRecordsCompare.Core.Config;
using MsSqlRecordsCompare.Core.Database.Models;

namespace MsSqlRecordsCompare.Core.Database;

public class RecordDataReader
{
    private readonly SqlConnectionFactory _connectionFactory;

    public RecordDataReader(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<TableRecord>> ReadRecordsAsync(TableConfig tableConfig, string recordId)
    {
        var query = tableConfig.GetQuery();
        var records = new List<TableRecord>();

        try
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(query, connection);
            command.CommandTimeout = _connectionFactory.CommandTimeout;
            command.Parameters.AddWithValue("@RecordID", recordId);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var columns = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    columns[columnName] = value;
                }

                records.Add(new TableRecord
                {
                    TableName = tableConfig.TableName,
                    RecordId = recordId,
                    Columns = columns
                });
            }
        }
        catch (SqlException ex) when (ex.Number == -2) // Timeout
        {
            throw new TimeoutException(
                $"Query timed out for table '{tableConfig.TableName}' with RecordID '{recordId}'. " +
                $"Consider increasing CommandTimeout (current: {_connectionFactory.CommandTimeout}s).", ex);
        }

        return records;
    }
}
