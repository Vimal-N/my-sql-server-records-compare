using System.Globalization;

namespace MsSqlRecordsCompare.Core.Comparison.Normalizers;

public class CurrencyNormalizer : NumericNormalizer
{
    public new bool AreEqual(object? oldValue, object? newValue, string? tolerance)
    {
        if (IsNullOrDbNull(oldValue) && IsNullOrDbNull(newValue)) return true;
        if (IsNullOrDbNull(oldValue) || IsNullOrDbNull(newValue)) return false;

        if (!TryParseCurrency(oldValue, out var oldDec) || !TryParseCurrency(newValue, out var newDec))
            return false;

        var toleranceValue = string.IsNullOrWhiteSpace(tolerance)
            ? 0.01m
            : decimal.TryParse(tolerance, NumberStyles.Any, CultureInfo.InvariantCulture, out var t) ? t : 0.01m;

        return Math.Abs(oldDec - newDec) <= toleranceValue;
    }

    public new string? Normalize(object? value)
    {
        if (IsNullOrDbNull(value)) return null;
        return TryParseCurrency(value, out var dec) ? dec.ToString("F2", CultureInfo.InvariantCulture) : value?.ToString()?.Trim();
    }

    private static bool TryParseCurrency(object? value, out decimal result)
    {
        if (TryParseDecimal(value, out result))
            return true;

        // Strip currency symbols and thousands separators
        var str = value?.ToString()?.Trim();
        if (string.IsNullOrEmpty(str)) { result = 0; return false; }

        str = str.Replace("$", "").Replace("£", "").Replace("€", "")
                 .Replace(",", "").Trim();

        return decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }
}
