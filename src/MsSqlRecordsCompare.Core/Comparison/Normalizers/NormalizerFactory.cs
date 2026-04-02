namespace MsSqlRecordsCompare.Core.Comparison.Normalizers;

public static class NormalizerFactory
{
    private static readonly ExactNormalizer Exact = new();
    private static readonly ExactCaseInsensitiveNormalizer ExactCi = new();
    private static readonly CurrencyNormalizer Currency = new();
    private static readonly DateNormalizer Date = new();
    private static readonly DateTimeNormalizer DateTime = new();
    private static readonly NumericNormalizer Numeric = new();
    private static readonly PercentageNormalizer Percentage = new();
    private static readonly BooleanNormalizer Boolean = new();
    private static readonly FuzzyNormalizer Fuzzy = new();
    private static readonly ContainsNormalizer Contains = new();
    private static readonly IgnoreNormalizer Ignore = new();

    public static INormalizer GetNormalizer(string rule)
    {
        return rule.ToLowerInvariant() switch
        {
            "exact" => Exact,
            "exact-ci" => ExactCi,
            "currency" => Currency,
            "date" => Date,
            "datetime" => DateTime,
            "numeric" => Numeric,
            "percentage" => Percentage,
            "boolean" => Boolean,
            "fuzzy" => Fuzzy,
            "contains" => Contains,
            "ignore" => Ignore,
            _ => throw new ArgumentException($"Unknown comparison rule: '{rule}'")
        };
    }
}
