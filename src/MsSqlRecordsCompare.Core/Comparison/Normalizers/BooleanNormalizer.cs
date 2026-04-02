namespace MsSqlRecordsCompare.Core.Comparison.Normalizers;

public class BooleanNormalizer : INormalizer
{
    private static readonly HashSet<string> TrueValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "true", "1", "yes", "y", "on"
    };

    private static readonly HashSet<string> FalseValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "false", "0", "no", "n", "off"
    };

    public bool AreEqual(object? oldValue, object? newValue, string? tolerance)
    {
        if (IsNullOrDbNull(oldValue) && IsNullOrDbNull(newValue)) return true;
        if (IsNullOrDbNull(oldValue) || IsNullOrDbNull(newValue)) return false;

        var oldBool = ParseBoolean(oldValue);
        var newBool = ParseBoolean(newValue);

        if (oldBool == null || newBool == null) return false;

        return oldBool == newBool;
    }

    public string? Normalize(object? value)
    {
        if (IsNullOrDbNull(value)) return null;
        var parsed = ParseBoolean(value);
        return parsed?.ToString() ?? value?.ToString()?.Trim();
    }

    private static bool? ParseBoolean(object? value)
    {
        if (value is bool b) return b;

        var str = value?.ToString()?.Trim();
        if (string.IsNullOrEmpty(str)) return null;

        if (TrueValues.Contains(str)) return true;
        if (FalseValues.Contains(str)) return false;
        return null;
    }

    private static bool IsNullOrDbNull(object? value) => value is null or DBNull;
}
