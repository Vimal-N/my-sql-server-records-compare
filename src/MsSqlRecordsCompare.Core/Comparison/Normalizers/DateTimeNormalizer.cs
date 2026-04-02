using System.Globalization;

namespace MsSqlRecordsCompare.Core.Comparison.Normalizers;

public class DateTimeNormalizer : DateNormalizer
{
    public new bool AreEqual(object? oldValue, object? newValue, string? tolerance)
    {
        if (IsNullOrDbNull(oldValue) && IsNullOrDbNull(newValue)) return true;
        if (IsNullOrDbNull(oldValue) || IsNullOrDbNull(newValue)) return false;

        if (!TryParseDate(oldValue, out var oldDt) || !TryParseDate(newValue, out var newDt))
            return false;

        var toleranceSeconds = 0.0;
        if (!string.IsNullOrWhiteSpace(tolerance) &&
            double.TryParse(tolerance, NumberStyles.Any, CultureInfo.InvariantCulture, out var t))
        {
            toleranceSeconds = t;
        }

        return Math.Abs((oldDt - newDt).TotalSeconds) <= toleranceSeconds;
    }

    public new string? Normalize(object? value)
    {
        if (IsNullOrDbNull(value)) return null;
        return TryParseDate(value, out var dt) ? dt.ToString("yyyy-MM-dd HH:mm:ss") : value?.ToString()?.Trim();
    }
}
