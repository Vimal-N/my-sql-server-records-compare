using System.Globalization;

namespace MsSqlRecordsCompare.Core.Comparison.Normalizers;

public class DateNormalizer : INormalizer
{
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy", "M/d/yyyy",
        "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss",
        "MM/dd/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss",
        "yyyyMMdd"
    ];

    public bool AreEqual(object? oldValue, object? newValue, string? tolerance)
    {
        if (IsNullOrDbNull(oldValue) && IsNullOrDbNull(newValue)) return true;
        if (IsNullOrDbNull(oldValue) || IsNullOrDbNull(newValue)) return false;

        if (!TryParseDate(oldValue, out var oldDate) || !TryParseDate(newValue, out var newDate))
            return false;

        // Compare date portion only
        return oldDate.Date == newDate.Date;
    }

    public string? Normalize(object? value)
    {
        if (IsNullOrDbNull(value)) return null;
        return TryParseDate(value, out var date) ? date.ToString("yyyy-MM-dd") : value?.ToString()?.Trim();
    }

    protected static bool TryParseDate(object? value, out DateTime result)
    {
        result = default;
        if (value is DateTime dt) { result = dt; return true; }
        if (value is DateTimeOffset dto) { result = dto.DateTime; return true; }
        if (value is DateOnly d) { result = d.ToDateTime(TimeOnly.MinValue); return true; }

        var str = value?.ToString()?.Trim();
        if (string.IsNullOrEmpty(str)) return false;

        return DateTime.TryParseExact(str, DateFormats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out result) ||
            DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    protected static bool IsNullOrDbNull(object? value) => value is null or DBNull;
}
