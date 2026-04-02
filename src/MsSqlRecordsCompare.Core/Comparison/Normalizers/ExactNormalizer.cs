namespace MsSqlRecordsCompare.Core.Comparison.Normalizers;

public class ExactNormalizer : INormalizer
{
    public bool AreEqual(object? oldValue, object? newValue, string? tolerance)
    {
        var oldStr = Normalize(oldValue);
        var newStr = Normalize(newValue);

        if (oldStr == null && newStr == null) return true;
        if (oldStr == null || newStr == null) return false;

        return string.Equals(oldStr, newStr, StringComparison.Ordinal);
    }

    public string? Normalize(object? value)
    {
        if (value is null or DBNull) return null;
        return value.ToString()?.Trim();
    }
}
