namespace MsSqlRecordsCompare.Core.Comparison.Normalizers;

public interface INormalizer
{
    /// <summary>
    /// Compares two values and returns whether they match.
    /// </summary>
    /// <param name="oldValue">Value from the old/legacy system</param>
    /// <param name="newValue">Value from the new/modern system</param>
    /// <param name="tolerance">Optional tolerance from config</param>
    /// <returns>True if the values are considered equal</returns>
    bool AreEqual(object? oldValue, object? newValue, string? tolerance);

    /// <summary>
    /// Normalizes a value for display in the report.
    /// Returns the original value as string if no normalization is needed.
    /// </summary>
    string? Normalize(object? value);
}
