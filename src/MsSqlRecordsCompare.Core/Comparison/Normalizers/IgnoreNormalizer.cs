namespace MsSqlRecordsCompare.Core.Comparison.Normalizers;

public class IgnoreNormalizer : INormalizer
{
    public bool AreEqual(object? oldValue, object? newValue, string? tolerance) => true;

    public string? Normalize(object? value)
    {
        if (value is null or DBNull) return null;
        return value.ToString()?.Trim();
    }
}
