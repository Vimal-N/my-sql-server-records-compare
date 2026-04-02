using System.Globalization;

namespace MsSqlRecordsCompare.Core.Comparison.Normalizers;

public class NumericNormalizer : INormalizer
{
    public bool AreEqual(object? oldValue, object? newValue, string? tolerance)
    {
        if (IsNullOrDbNull(oldValue) && IsNullOrDbNull(newValue)) return true;
        if (IsNullOrDbNull(oldValue) || IsNullOrDbNull(newValue)) return false;

        if (!TryParseDecimal(oldValue, out var oldDec) || !TryParseDecimal(newValue, out var newDec))
            return false;

        var toleranceValue = ParseTolerance(tolerance);
        return Math.Abs(oldDec - newDec) <= toleranceValue;
    }

    public string? Normalize(object? value)
    {
        if (IsNullOrDbNull(value)) return null;
        return TryParseDecimal(value, out var dec) ? dec.ToString(CultureInfo.InvariantCulture) : value?.ToString()?.Trim();
    }

    private static decimal ParseTolerance(string? tolerance)
    {
        if (string.IsNullOrWhiteSpace(tolerance)) return 0m;
        return decimal.TryParse(tolerance, NumberStyles.Any, CultureInfo.InvariantCulture, out var t) ? t : 0m;
    }

    protected static bool TryParseDecimal(object? value, out decimal result)
    {
        result = 0;
        if (value is decimal d) { result = d; return true; }
        if (value is double dbl) { result = (decimal)dbl; return true; }
        if (value is float f) { result = (decimal)f; return true; }
        if (value is int i) { result = i; return true; }
        if (value is long l) { result = l; return true; }
        if (value is short s) { result = s; return true; }
        if (value is byte b) { result = b; return true; }

        var str = value?.ToString()?.Trim();
        if (string.IsNullOrEmpty(str)) return false;

        return decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    protected static bool IsNullOrDbNull(object? value) => value is null or DBNull;
}
