using MsSqlRecordsCompare.Core.Comparison.Models;
using MsSqlRecordsCompare.Core.Config;
using MsSqlRecordsCompare.Core.Database;

namespace MsSqlRecordsCompare.Core.Comparison;

public class ComparisonEngine
{
    private readonly ComparisonConfig _config;
    private readonly RecordDataReader _dataReader;
    private readonly RowMatcher _rowMatcher = new();
    private readonly ColumnComparer _columnComparer;
    private readonly IProgress<string>? _progress;

    public ComparisonEngine(
        ComparisonConfig config,
        RecordDataReader dataReader,
        IProgress<string>? progress = null)
    {
        _config = config;
        _dataReader = dataReader;
        _columnComparer = new ColumnComparer(config);
        _progress = progress;
    }

    public async Task<ComparisonResult> RunAsync(
        string configFile, string userName, List<ComparisonPair>? overridePairs = null)
    {
        var pairs = overridePairs ?? _config.ComparisonPairs;

        var result = new ComparisonResult
        {
            ConfigFile = configFile,
            TableSet = _config.SelectedTableSet,
            ServerName = _config.Connection.ServerName,
            DatabaseName = _config.Connection.DatabaseName,
            UserName = userName,
            RunTimestamp = DateTime.Now
        };

        foreach (var pair in pairs)
        {
            _progress?.Report($"Comparing scenario: {pair.Scenario} ({pair.OldRecordId} → {pair.NewRecordId})");
            var scenarioResult = await CompareScenarioAsync(pair);
            result.Scenarios.Add(scenarioResult);
        }

        return result;
    }

    private async Task<ScenarioResult> CompareScenarioAsync(ComparisonPair pair)
    {
        var scenarioResult = new ScenarioResult
        {
            Scenario = pair.Scenario,
            OldRecordId = pair.OldRecordId,
            NewRecordId = pair.NewRecordId
        };

        foreach (var table in _config.Tables)
        {
            _progress?.Report($"  Reading {table.FullTableName}...");
            var tableResult = await CompareTableAsync(table, pair.OldRecordId, pair.NewRecordId);
            scenarioResult.TableResults.Add(tableResult);
        }

        return scenarioResult;
    }

    private async Task<TableResult> CompareTableAsync(
        TableConfig table, string oldRecordId, string newRecordId)
    {
        List<Database.Models.TableRecord> oldRows;
        List<Database.Models.TableRecord> newRows;

        try
        {
            oldRows = await _dataReader.ReadRecordsAsync(table, oldRecordId);
            newRows = await _dataReader.ReadRecordsAsync(table, newRecordId);
        }
        catch (Exception ex) when (ex is TimeoutException or Microsoft.Data.SqlClient.SqlException)
        {
            return new TableResult
            {
                TableName = table.TableName,
                Schema = table.Schema,
                Error = ex.Message
            };
        }

        // Get excluded columns for this table
        var exclusions = _config.GetExclusionsForTable(table.TableName);
        var excludedColumns = exclusions.Select(e => new ExcludedColumn
        {
            ColumnName = e.ColumnName,
            Reason = e.Reason
        }).ToList();

        // Handle no data scenarios
        if (oldRows.Count == 0 && newRows.Count == 0)
        {
            return new TableResult
            {
                TableName = table.TableName,
                Schema = table.Schema,
                OldRowCount = 0,
                NewRowCount = 0,
                ExcludedColumns = excludedColumns
            };
        }

        // Match rows
        var matchResult = _rowMatcher.Match(oldRows, newRows, table.RowMatchColumns);

        // Compare matched pairs
        var rowResults = new List<RowComparisonResult>();
        foreach (var pair in matchResult.MatchedPairs)
        {
            var rowResult = _columnComparer.Compare(pair.OldRow, pair.NewRow, table.TableName);
            rowResults.Add(rowResult);
        }

        // Build unmatched row descriptions
        var unmatchedOld = matchResult.UnmatchedOldRows.Select(r => new UnmatchedRow
        {
            Description = $"Missing in new system (key: {r.RowMatchKey ?? "N/A"})",
            Columns = r.Columns
        }).ToList();

        var unmatchedNew = matchResult.UnmatchedNewRows.Select(r => new UnmatchedRow
        {
            Description = $"Extra in new system (key: {r.RowMatchKey ?? "N/A"})",
            Columns = r.Columns
        }).ToList();

        return new TableResult
        {
            TableName = table.TableName,
            Schema = table.Schema,
            OldRowCount = oldRows.Count,
            NewRowCount = newRows.Count,
            RowResults = rowResults,
            UnmatchedOldRows = unmatchedOld,
            UnmatchedNewRows = unmatchedNew,
            ExcludedColumns = excludedColumns,
            Warnings = matchResult.Warnings
        };
    }
}
