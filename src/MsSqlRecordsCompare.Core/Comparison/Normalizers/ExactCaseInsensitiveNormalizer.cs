namespace MsSqlRecordsCompare.Core.Comparison.Normalizers;

public class ExactCaseInsensitiveNormalizer : INormalizer
{
    public bool AreEqual(object? oldValue, object? newValue, string? tolerance)
    {
        var oldStr = Normalize(oldValue);
        var newStr = Normalize(newValue);

        if (oldStr == null && newStr == null) return true;
        if (oldStr == null || newStr == null) return false;

        return string.Equals(oldStr, newStr, StringComparison.OrdinalIgnoreCase);
    }

    public string? Normalize(object? value)
    {
        if (value is null or DBNull) return null;
        return value.ToString()?.Trim();
    }
}
