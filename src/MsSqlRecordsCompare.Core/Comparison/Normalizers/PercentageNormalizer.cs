using System.Globalization;

namespace MsSqlRecordsCompare.Core.Comparison.Normalizers;

public class PercentageNormalizer : INormalizer
{
    public bool AreEqual(object? oldValue, object? newValue, string? tolerance)
    {
        if (IsNullOrDbNull(oldValue) && IsNullOrDbNull(newValue)) return true;
        if (IsNullOrDbNull(oldValue) || IsNullOrDbNull(newValue)) return false;

        if (!TryParsePercentage(oldValue, out var oldPct) || !TryParsePercentage(newValue, out var newPct))
            return false;

        var toleranceValue = string.IsNullOrWhiteSpace(tolerance)
            ? 0m
            : decimal.TryParse(tolerance, NumberStyles.Any, CultureInfo.InvariantCulture, out var t) ? t : 0m;

        return Math.Abs(oldPct - newPct) <= toleranceValue;
    }

    public string? Normalize(object? value)
    {
        if (IsNullOrDbNull(value)) return null;
        return TryParsePercentage(value, out var pct) ? pct.ToString(CultureInfo.InvariantCulture) : value?.ToString()?.Trim();
    }

    private static bool TryParsePercentage(object? value, out decimal result)
    {
        result = 0;
        if (value is decimal d) { result = NormalizeToDecimal(d); return true; }
        if (value is double dbl) { result = NormalizeToDecimal((decimal)dbl); return true; }
        if (value is float f) { result = NormalizeToDecimal((decimal)f); return true; }

        var str = value?.ToString()?.Trim();
        if (string.IsNullOrEmpty(str)) return false;

        bool hasPercentSign = str.EndsWith('%');
        str = str.TrimEnd('%').Trim();

        if (!decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return false;

        // If it had a % sign, it's already in display format (e.g., "15%")
        // Convert to decimal form (0.15)
        if (hasPercentSign)
            result = parsed / 100m;
        else
            result = NormalizeToDecimal(parsed);

        return true;
    }

    /// <summary>
    /// Normalizes to a decimal fraction. Values > 1 are assumed to be display format (15 → 0.15).
    /// Values ≤ 1 are assumed to already be decimal form (0.15).
    /// </summary>
    private static decimal NormalizeToDecimal(decimal value)
    {
        // If value is > 1, assume it's a display percentage (e.g., 15 = 15%)
        if (Math.Abs(value) > 1)
            return value / 100m;
        return value;
    }

    private static bool IsNullOrDbNull(object? value) => value is null or DBNull;
}
