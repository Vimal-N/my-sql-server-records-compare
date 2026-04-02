namespace MsSqlRecordsCompare.Core.Comparison.Normalizers;

public class ContainsNormalizer : INormalizer
{
    public bool AreEqual(object? oldValue, object? newValue, string? tolerance)
    {
        if (IsNullOrDbNull(oldValue) && IsNullOrDbNull(newValue)) return true;
        if (IsNullOrDbNull(oldValue) || IsNullOrDbNull(newValue)) return false;

        var oldStr = oldValue?.ToString()?.Trim() ?? "";
        var newStr = newValue?.ToString()?.Trim() ?? "";

        // New value should contain old value
        return newStr.Contains(oldStr, StringComparison.OrdinalIgnoreCase);
    }

    public string? Normalize(object? value)
    {
        if (IsNullOrDbNull(value)) return null;
        return value!.ToString()?.Trim();
    }

    private static bool IsNullOrDbNull(object? value) => value is null or DBNull;
}
