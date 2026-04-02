using MsSqlRecordsCompare.Core.Comparison.Models;
using MsSqlRecordsCompare.Core.Comparison.Normalizers;
using MsSqlRecordsCompare.Core.Config;
using MsSqlRecordsCompare.Core.Database.Models;

namespace MsSqlRecordsCompare.Core.Comparison;

public class ColumnComparer
{
    private readonly ComparisonConfig _config;

    public ColumnComparer(ComparisonConfig config)
    {
        _config = config;
    }

    public RowComparisonResult Compare(TableRecord oldRow, TableRecord newRow, string tableName)
    {
        var result = new RowComparisonResult
        {
            MatchKey = oldRow.RowMatchKey
        };

        // Get all column names from both rows
        var allColumns = oldRow.Columns.Keys
            .Union(newRow.Columns.Keys, StringComparer.OrdinalIgnoreCase)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var column in allColumns)
        {
            // Skip excluded columns
            if (_config.IsColumnExcluded(tableName, column))
                continue;

            var rule = _config.GetCompareRule(tableName, column);
            var tolerance = _config.GetTolerance(tableName, column);

            var oldValue = GetColumnValue(oldRow, column);
            var newValue = GetColumnValue(newRow, column);

            var normalizer = NormalizerFactory.GetNormalizer(rule);

            if (normalizer.AreEqual(oldValue, newValue, tolerance))
            {
                result.Matches.Add(new ColumnMatch
                {
                    ColumnName = column,
                    Value = normalizer.Normalize(oldValue),
                    CompareRule = rule
                });
            }
            else
            {
                result.Mismatches.Add(new ColumnMismatch
                {
                    ColumnName = column,
                    OldValue = FormatValue(oldValue),
                    NewValue = FormatValue(newValue),
                    CompareRule = rule,
                    Tolerance = tolerance
                });
            }
        }

        return result;
    }

    private static object? GetColumnValue(TableRecord row, string column)
    {
        var entry = row.Columns
            .FirstOrDefault(c => c.Key.Equals(column, StringComparison.OrdinalIgnoreCase));
        return entry.Value;
    }

    private static string? FormatValue(object? value)
    {
        if (value is null or DBNull) return "<NULL>";
        var str = value.ToString()?.Trim();
        return string.IsNullOrEmpty(str) ? "<EMPTY>" : str;
    }
}
