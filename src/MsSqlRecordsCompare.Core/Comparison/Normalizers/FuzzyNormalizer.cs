using System.Globalization;
using FuzzySharp;

namespace MsSqlRecordsCompare.Core.Comparison.Normalizers;

public class FuzzyNormalizer : INormalizer
{
    public bool AreEqual(object? oldValue, object? newValue, string? tolerance)
    {
        if (IsNullOrDbNull(oldValue) && IsNullOrDbNull(newValue)) return true;
        if (IsNullOrDbNull(oldValue) || IsNullOrDbNull(newValue)) return false;

        var oldStr = oldValue?.ToString()?.Trim() ?? "";
        var newStr = newValue?.ToString()?.Trim() ?? "";

        if (oldStr == newStr) return true;

        var threshold = 90; // default 0.90 = 90%
        if (!string.IsNullOrWhiteSpace(tolerance) &&
            double.TryParse(tolerance, NumberStyles.Any, CultureInfo.InvariantCulture, out var t))
        {
            // Support both 0.90 and 90 formats
            threshold = t <= 1.0 ? (int)(t * 100) : (int)t;
        }

        var ratio = Fuzz.Ratio(oldStr, newStr);
        return ratio >= threshold;
    }

    public string? Normalize(object? value)
    {
        if (IsNullOrDbNull(value)) return null;
        return value!.ToString()?.Trim();
    }

    private static bool IsNullOrDbNull(object? value) => value is null or DBNull;
}
