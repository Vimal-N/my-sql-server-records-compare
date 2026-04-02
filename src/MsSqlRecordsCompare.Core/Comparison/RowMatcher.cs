using MsSqlRecordsCompare.Core.Comparison.Models;
using MsSqlRecordsCompare.Core.Database.Models;

namespace MsSqlRecordsCompare.Core.Comparison;

public class RowMatcher
{
    public RowMatchResult Match(
        List<TableRecord> oldRows,
        List<TableRecord> newRows,
        List<string> rowMatchColumns)
    {
        if (rowMatchColumns.Count == 0)
            return MatchSingleRow(oldRows, newRows);

        return MatchByKey(oldRows, newRows, rowMatchColumns);
    }

    private static RowMatchResult MatchSingleRow(List<TableRecord> oldRows, List<TableRecord> newRows)
    {
        var result = new RowMatchResult();

        if (oldRows.Count == 0 && newRows.Count == 0)
            return result;

        if (oldRows.Count == 0)
        {
            result.UnmatchedNewRows.AddRange(newRows);
            return result;
        }

        if (newRows.Count == 0)
        {
            result.UnmatchedOldRows.AddRange(oldRows);
            return result;
        }

        if (oldRows.Count > 1)
            result.Warnings.Add($"Expected single row but found {oldRows.Count} rows in old record. Comparing first row only.");

        if (newRows.Count > 1)
            result.Warnings.Add($"Expected single row but found {newRows.Count} rows in new record. Comparing first row only.");

        result.MatchedPairs.Add(new MatchedRowPair
        {
            OldRow = oldRows[0],
            NewRow = newRows[0]
        });

        return result;
    }

    private static RowMatchResult MatchByKey(
        List<TableRecord> oldRows,
        List<TableRecord> newRows,
        List<string> rowMatchColumns)
    {
        var result = new RowMatchResult();

        // Build key → rows lookup for both sides
        var oldByKey = GroupByKey(oldRows, rowMatchColumns);
        var newByKey = GroupByKey(newRows, rowMatchColumns);

        // Check for duplicate keys
        foreach (var (key, rows) in oldByKey)
        {
            if (rows.Count > 1)
                result.Warnings.Add($"Duplicate key '{key}' found in old record ({rows.Count} rows). Comparing first match only.");
        }
        foreach (var (key, rows) in newByKey)
        {
            if (rows.Count > 1)
                result.Warnings.Add($"Duplicate key '{key}' found in new record ({rows.Count} rows). Comparing first match only.");
        }

        // Match rows
        var matchedNewKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, oldRowList) in oldByKey)
        {
            if (newByKey.TryGetValue(key, out var newRowList))
            {
                result.MatchedPairs.Add(new MatchedRowPair
                {
                    OldRow = oldRowList[0],
                    NewRow = newRowList[0],
                    MatchKey = key
                });
                matchedNewKeys.Add(key);
            }
            else
            {
                result.UnmatchedOldRows.Add(oldRowList[0]);
            }
        }

        // Find unmatched new rows
        foreach (var (key, newRowList) in newByKey)
        {
            if (!matchedNewKeys.Contains(key))
            {
                result.UnmatchedNewRows.Add(newRowList[0]);
            }
        }

        return result;
    }

    private static Dictionary<string, List<TableRecord>> GroupByKey(
        List<TableRecord> rows, List<string> keyColumns)
    {
        var grouped = new Dictionary<string, List<TableRecord>>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var key = BuildKey(row, keyColumns);
            row.RowMatchKey = key;

            if (!grouped.TryGetValue(key, out var list))
            {
                list = [];
                grouped[key] = list;
            }
            list.Add(row);
        }

        return grouped;
    }

    private static string BuildKey(TableRecord row, List<string> keyColumns)
    {
        var parts = keyColumns.Select(col =>
        {
            var value = row.Columns
                .FirstOrDefault(c => c.Key.Equals(col, StringComparison.OrdinalIgnoreCase))
                .Value;
            return value is null or DBNull ? "<NULL>" : value.ToString()?.Trim() ?? "";
        });

        return string.Join("||", parts);
    }
}
