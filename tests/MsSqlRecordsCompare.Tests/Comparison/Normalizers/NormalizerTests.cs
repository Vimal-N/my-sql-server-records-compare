using FluentAssertions;
using MsSqlRecordsCompare.Core.Comparison.Normalizers;

namespace MsSqlRecordsCompare.Tests.Comparison.Normalizers;

public class ExactNormalizerTests
{
    private readonly ExactNormalizer _sut = new();

    [Fact] public void NullNull_Match() => _sut.AreEqual(null, null, null).Should().BeTrue();
    [Fact] public void NullValue_NoMatch() => _sut.AreEqual(null, "abc", null).Should().BeFalse();
    [Fact] public void ValueNull_NoMatch() => _sut.AreEqual("abc", null, null).Should().BeFalse();
    [Fact] public void DbNullDbNull_Match() => _sut.AreEqual(DBNull.Value, DBNull.Value, null).Should().BeTrue();
    [Fact] public void DbNullNull_Match() => _sut.AreEqual(DBNull.Value, null, null).Should().BeTrue();
    [Fact] public void SameString_Match() => _sut.AreEqual("hello", "hello", null).Should().BeTrue();
    [Fact] public void DifferentCase_NoMatch() => _sut.AreEqual("Hello", "hello", null).Should().BeFalse();
    [Fact] public void TrimsWhitespace() => _sut.AreEqual("  hello  ", "hello", null).Should().BeTrue();
    [Fact] public void EmptyStrings_Match() => _sut.AreEqual("", "", null).Should().BeTrue();
    [Fact] public void DifferentStrings_NoMatch() => _sut.AreEqual("abc", "def", null).Should().BeFalse();
    [Fact] public void IntegerValues_Match() => _sut.AreEqual(42, 42, null).Should().BeTrue();
    [Fact] public void IntVsString_Match() => _sut.AreEqual(42, "42", null).Should().BeTrue();
}

public class ExactCaseInsensitiveNormalizerTests
{
    private readonly ExactCaseInsensitiveNormalizer _sut = new();

    [Fact] public void SameCase_Match() => _sut.AreEqual("hello", "hello", null).Should().BeTrue();
    [Fact] public void DifferentCase_Match() => _sut.AreEqual("Hello", "hello", null).Should().BeTrue();
    [Fact] public void MixedCase_Match() => _sut.AreEqual("HeLLo WoRLD", "hello world", null).Should().BeTrue();
    [Fact] public void Different_NoMatch() => _sut.AreEqual("abc", "def", null).Should().BeFalse();
    [Fact] public void NullNull_Match() => _sut.AreEqual(null, null, null).Should().BeTrue();
}

public class NumericNormalizerTests
{
    private readonly NumericNormalizer _sut = new();

    [Fact] public void SameDecimals_Match() => _sut.AreEqual(1234.56m, 1234.56m, null).Should().BeTrue();
    [Fact] public void DifferentDecimals_NoMatch() => _sut.AreEqual(1234.56m, 1234.57m, null).Should().BeFalse();
    [Fact] public void WithinTolerance_Match() => _sut.AreEqual(1234.56m, 1234.57m, "0.01").Should().BeTrue();
    [Fact] public void BeyondTolerance_NoMatch() => _sut.AreEqual(1234.56m, 1234.58m, "0.01").Should().BeFalse();
    [Fact] public void IntAndDecimal_Match() => _sut.AreEqual(100, 100.0m, null).Should().BeTrue();
    [Fact] public void StringNumbers_Match() => _sut.AreEqual("1234.56", "1234.56", null).Should().BeTrue();
    [Fact] public void NullNull_Match() => _sut.AreEqual(null, null, null).Should().BeTrue();
    [Fact] public void NullValue_NoMatch() => _sut.AreEqual(null, 42m, null).Should().BeFalse();
    [Fact] public void ZeroTolerance_ExactMatch() => _sut.AreEqual(1.0m, 1.0m, "0").Should().BeTrue();
    [Fact] public void DoubleValues_Match() => _sut.AreEqual(3.14, 3.14, null).Should().BeTrue();
}

public class CurrencyNormalizerTests
{
    private readonly CurrencyNormalizer _sut = new();

    [Fact] public void SameAmounts_Match() => _sut.AreEqual(1234.56m, 1234.56m, null).Should().BeTrue();
    [Fact] public void WithinDefaultTolerance_Match() => _sut.AreEqual(1234.56m, 1234.57m, null).Should().BeTrue();
    [Fact] public void BeyondDefaultTolerance_NoMatch() => _sut.AreEqual(1234.56m, 1234.58m, null).Should().BeFalse();
    [Fact] public void DollarSign_Match() => _sut.AreEqual("$1,234.56", 1234.56m, null).Should().BeTrue();
    [Fact] public void EuroSign_Match() => _sut.AreEqual("€1,234.56", 1234.56m, null).Should().BeTrue();
    [Fact] public void PoundSign_Match() => _sut.AreEqual("£1,234.56", 1234.56m, null).Should().BeTrue();
    [Fact] public void WithThousandsSeparator() => _sut.AreEqual("1,234.56", "1234.56", null).Should().BeTrue();
    [Fact] public void NullNull_Match() => _sut.AreEqual(null, null, null).Should().BeTrue();
    [Fact] public void CustomTolerance() => _sut.AreEqual(100.00m, 100.05m, "0.10").Should().BeTrue();

    [Fact]
    public void Normalize_FormatsToTwoDecimals()
    {
        _sut.Normalize(1234.5m).Should().Be("1234.50");
        _sut.Normalize("$1,234.56").Should().Be("1234.56");
    }
}

public class DateNormalizerTests
{
    private readonly DateNormalizer _sut = new();

    [Fact] public void SameDates_Match() =>
        _sut.AreEqual(new DateTime(2026, 3, 15), new DateTime(2026, 3, 15), null).Should().BeTrue();

    [Fact] public void SameDateDifferentTime_Match() =>
        _sut.AreEqual(new DateTime(2026, 3, 15, 10, 30, 0), new DateTime(2026, 3, 15, 14, 45, 0), null).Should().BeTrue();

    [Fact] public void DifferentDates_NoMatch() =>
        _sut.AreEqual(new DateTime(2026, 3, 15), new DateTime(2026, 3, 16), null).Should().BeFalse();

    [Fact] public void StringDates_Match() =>
        _sut.AreEqual("2026-03-15", "2026-03-15", null).Should().BeTrue();

    [Fact] public void DifferentFormats_Match() =>
        _sut.AreEqual("2026-03-15", "03/15/2026", null).Should().BeTrue();

    [Fact] public void NullNull_Match() => _sut.AreEqual(null, null, null).Should().BeTrue();
    [Fact] public void NullValue_NoMatch() => _sut.AreEqual(null, DateTime.Now, null).Should().BeFalse();

    [Fact]
    public void Normalize_FormatsToIso()
    {
        _sut.Normalize(new DateTime(2026, 3, 15)).Should().Be("2026-03-15");
    }
}

public class DateTimeNormalizerTests
{
    private readonly DateTimeNormalizer _sut = new();

    [Fact] public void ExactSameDateTime_Match() =>
        _sut.AreEqual(new DateTime(2026, 3, 15, 10, 30, 0), new DateTime(2026, 3, 15, 10, 30, 0), null).Should().BeTrue();

    [Fact] public void DifferentTime_NoMatch() =>
        _sut.AreEqual(new DateTime(2026, 3, 15, 10, 30, 0), new DateTime(2026, 3, 15, 10, 31, 0), null).Should().BeFalse();

    [Fact] public void WithinTolerance_Match() =>
        _sut.AreEqual(new DateTime(2026, 3, 15, 10, 30, 0), new DateTime(2026, 3, 15, 10, 30, 5), "10").Should().BeTrue();

    [Fact] public void BeyondTolerance_NoMatch() =>
        _sut.AreEqual(new DateTime(2026, 3, 15, 10, 30, 0), new DateTime(2026, 3, 15, 10, 31, 0), "10").Should().BeFalse();
}

public class PercentageNormalizerTests
{
    private readonly PercentageNormalizer _sut = new();

    [Fact] public void DecimalAndPercent_Match() => _sut.AreEqual(0.15m, "15%", null).Should().BeTrue();
    [Fact] public void SameDecimal_Match() => _sut.AreEqual(0.15m, 0.15m, null).Should().BeTrue();
    [Fact] public void IntegerPercent_Match() => _sut.AreEqual(15m, "15%", null).Should().BeTrue();
    [Fact] public void Different_NoMatch() => _sut.AreEqual(0.15m, "20%", null).Should().BeFalse();
    [Fact] public void NullNull_Match() => _sut.AreEqual(null, null, null).Should().BeTrue();

    [Fact]
    public void Normalize_ConvertsToDecimalForm()
    {
        _sut.Normalize("15%").Should().Be("0.15");
        _sut.Normalize(0.15m).Should().Be("0.15");
    }
}

public class BooleanNormalizerTests
{
    private readonly BooleanNormalizer _sut = new();

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, "true")]
    [InlineData(true, "True")]
    [InlineData(true, "1")]
    [InlineData(true, "yes")]
    [InlineData(true, "Y")]
    [InlineData(true, "on")]
    public void TrueVariants_Match(object old, object @new) =>
        _sut.AreEqual(old, @new, null).Should().BeTrue();

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, "false")]
    [InlineData(false, "0")]
    [InlineData(false, "no")]
    [InlineData(false, "N")]
    [InlineData(false, "off")]
    public void FalseVariants_Match(object old, object @new) =>
        _sut.AreEqual(old, @new, null).Should().BeTrue();

    [Theory]
    [InlineData(true, false)]
    [InlineData("yes", "no")]
    [InlineData("1", "0")]
    [InlineData("Y", "N")]
    public void TrueVsFalse_NoMatch(object old, object @new) =>
        _sut.AreEqual(old, @new, null).Should().BeFalse();

    [Fact] public void NullNull_Match() => _sut.AreEqual(null, null, null).Should().BeTrue();
    [Fact] public void NullValue_NoMatch() => _sut.AreEqual(null, true, null).Should().BeFalse();
}

public class FuzzyNormalizerTests
{
    private readonly FuzzyNormalizer _sut = new();

    [Fact] public void ExactMatch_Match() => _sut.AreEqual("John Smith", "John Smith", null).Should().BeTrue();
    [Fact] public void HighSimilarity_Match() => _sut.AreEqual("John Smith", "John Smth", "0.80").Should().BeTrue();
    [Fact] public void LowSimilarity_NoMatch() => _sut.AreEqual("John", "Jane", "0.90").Should().BeFalse();
    [Fact] public void NullNull_Match() => _sut.AreEqual(null, null, null).Should().BeTrue();
    [Fact] public void IntegerThreshold_Match() => _sut.AreEqual("John Smith", "John Smth", "80").Should().BeTrue();
}

public class ContainsNormalizerTests
{
    private readonly ContainsNormalizer _sut = new();

    [Fact] public void NewContainsOld_Match() => _sut.AreEqual("AUTH-9912", "AUTH-9912-NEW", null).Should().BeTrue();
    [Fact] public void Exact_Match() => _sut.AreEqual("hello", "hello", null).Should().BeTrue();
    [Fact] public void NewDoesNotContainOld_NoMatch() => _sut.AreEqual("ABC", "DEF", null).Should().BeFalse();
    [Fact] public void CaseInsensitive() => _sut.AreEqual("auth", "AUTH-9912", null).Should().BeTrue();
    [Fact] public void NullNull_Match() => _sut.AreEqual(null, null, null).Should().BeTrue();
}

public class IgnoreNormalizerTests
{
    private readonly IgnoreNormalizer _sut = new();

    [Fact] public void AlwaysMatch() => _sut.AreEqual("anything", "different", null).Should().BeTrue();
    [Fact] public void NullsMatch() => _sut.AreEqual(null, null, null).Should().BeTrue();
    [Fact] public void MixedMatch() => _sut.AreEqual(null, "value", null).Should().BeTrue();
}

public class NormalizerFactoryTests
{
    [Theory]
    [InlineData("exact", typeof(ExactNormalizer))]
    [InlineData("exact-ci", typeof(ExactCaseInsensitiveNormalizer))]
    [InlineData("currency", typeof(CurrencyNormalizer))]
    [InlineData("date", typeof(DateNormalizer))]
    [InlineData("datetime", typeof(DateTimeNormalizer))]
    [InlineData("numeric", typeof(NumericNormalizer))]
    [InlineData("percentage", typeof(PercentageNormalizer))]
    [InlineData("boolean", typeof(BooleanNormalizer))]
    [InlineData("fuzzy", typeof(FuzzyNormalizer))]
    [InlineData("contains", typeof(ContainsNormalizer))]
    [InlineData("ignore", typeof(IgnoreNormalizer))]
    public void GetNormalizer_ReturnsCorrectType(string rule, Type expectedType)
    {
        NormalizerFactory.GetNormalizer(rule).Should().BeOfType(expectedType);
    }

    [Fact]
    public void GetNormalizer_CaseInsensitive()
    {
        NormalizerFactory.GetNormalizer("Currency").Should().BeOfType<CurrencyNormalizer>();
        NormalizerFactory.GetNormalizer("EXACT").Should().BeOfType<ExactNormalizer>();
    }

    [Fact]
    public void GetNormalizer_UnknownRule_Throws()
    {
        var act = () => NormalizerFactory.GetNormalizer("unknown");
        act.Should().Throw<ArgumentException>().WithMessage("*unknown*");
    }
}
