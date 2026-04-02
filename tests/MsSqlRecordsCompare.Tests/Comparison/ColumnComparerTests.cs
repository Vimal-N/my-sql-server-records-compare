using FluentAssertions;
using MsSqlRecordsCompare.Core.Comparison;
using MsSqlRecordsCompare.Core.Config;
using MsSqlRecordsCompare.Core.Database.Models;

namespace MsSqlRecordsCompare.Tests.Comparison;

public class ColumnComparerTests
{
    private static ComparisonConfig CreateConfig(
        List<ExclusionConfig>? exclusions = null,
        List<ColumnRuleConfig>? columnRules = null)
    {
        return new ComparisonConfig
        {
            Connection = new ConnectionConfig { ServerName = "S", DatabaseName = "D" },
            SelectedTableSet = "Tables",
            Tables = [new TableConfig { TableName = "Order", RecordIdColumn = "ID" }],
            Exclusions = exclusions ?? [],
            ColumnRules = columnRules ?? []
        };
    }

    private static TableRecord CreateRow(Dictionary<string, object?> columns)
    {
        return new TableRecord { TableName = "Order", RecordId = "1", Columns = columns };
    }

    [Fact]
    public void Compare_AllMatch_NoMismatches()
    {
        var config = CreateConfig();
        var comparer = new ColumnComparer(config);

        var old = CreateRow(new() { ["Name"] = "John", ["Age"] = 30 });
        var @new = CreateRow(new() { ["Name"] = "John", ["Age"] = 30 });

        var result = comparer.Compare(old, @new, "Order");

        result.Mismatches.Should().BeEmpty();
        result.Matches.Should().HaveCount(2);
    }

    [Fact]
    public void Compare_Mismatch_ReportsColumnDifference()
    {
        var config = CreateConfig();
        var comparer = new ColumnComparer(config);

        var old = CreateRow(new() { ["Name"] = "John", ["Status"] = "Active" });
        var @new = CreateRow(new() { ["Name"] = "John", ["Status"] = "Pending" });

        var result = comparer.Compare(old, @new, "Order");

        result.Mismatches.Should().HaveCount(1);
        result.Mismatches[0].ColumnName.Should().Be("Status");
        result.Mismatches[0].OldValue.Should().Be("Active");
        result.Mismatches[0].NewValue.Should().Be("Pending");
    }

    [Fact]
    public void Compare_ExcludedColumn_IsSkipped()
    {
        var config = CreateConfig(exclusions:
        [
            new ExclusionConfig { TableName = "*", ColumnName = "CreatedDate" }
        ]);
        var comparer = new ColumnComparer(config);

        var old = CreateRow(new() { ["Name"] = "John", ["CreatedDate"] = DateTime.Now });
        var @new = CreateRow(new() { ["Name"] = "John", ["CreatedDate"] = DateTime.Now.AddHours(1) });

        var result = comparer.Compare(old, @new, "Order");

        result.Mismatches.Should().BeEmpty();
        // CreatedDate should not appear in matches either
        result.Matches.Should().HaveCount(1);
        result.Matches[0].ColumnName.Should().Be("Name");
    }

    [Fact]
    public void Compare_CurrencyRule_AppliesTolerance()
    {
        var config = CreateConfig(columnRules:
        [
            new ColumnRuleConfig { TableName = "*", ColumnName = "Amount", CompareRule = "currency", Tolerance = "0.01" }
        ]);
        var comparer = new ColumnComparer(config);

        var old = CreateRow(new() { ["Amount"] = 100.00m });
        var @new = CreateRow(new() { ["Amount"] = 100.01m });

        var result = comparer.Compare(old, @new, "Order");

        result.Mismatches.Should().BeEmpty();
        result.Matches.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_NullVsNull_IsMatch()
    {
        var config = CreateConfig();
        var comparer = new ColumnComparer(config);

        var old = CreateRow(new() { ["Notes"] = (object?)null });
        var @new = CreateRow(new() { ["Notes"] = (object?)null });

        var result = comparer.Compare(old, @new, "Order");

        result.Mismatches.Should().BeEmpty();
    }

    [Fact]
    public void Compare_NullVsValue_IsMismatch()
    {
        var config = CreateConfig();
        var comparer = new ColumnComparer(config);

        var old = CreateRow(new() { ["Notes"] = (object?)null });
        var @new = CreateRow(new() { ["Notes"] = "Some note" });

        var result = comparer.Compare(old, @new, "Order");

        result.Mismatches.Should().HaveCount(1);
        result.Mismatches[0].OldValue.Should().Be("<NULL>");
        result.Mismatches[0].NewValue.Should().Be("Some note");
    }

    [Fact]
    public void Compare_IgnoreRule_AlwaysMatches()
    {
        var config = CreateConfig(columnRules:
        [
            new ColumnRuleConfig { TableName = "*", ColumnName = "Version", CompareRule = "ignore" }
        ]);
        var comparer = new ColumnComparer(config);

        var old = CreateRow(new() { ["Version"] = "1.0" });
        var @new = CreateRow(new() { ["Version"] = "2.0" });

        var result = comparer.Compare(old, @new, "Order");

        result.Mismatches.Should().BeEmpty();
    }

    [Fact]
    public void Compare_BooleanRule_NormalizesValues()
    {
        var config = CreateConfig(columnRules:
        [
            new ColumnRuleConfig { TableName = "*", ColumnName = "IsActive", CompareRule = "boolean" }
        ]);
        var comparer = new ColumnComparer(config);

        var old = CreateRow(new() { ["IsActive"] = "Y" });
        var @new = CreateRow(new() { ["IsActive"] = true });

        var result = comparer.Compare(old, @new, "Order");

        result.Mismatches.Should().BeEmpty();
    }

    [Fact]
    public void Compare_CaseInsensitiveRule_MatchesDifferentCase()
    {
        var config = CreateConfig(columnRules:
        [
            new ColumnRuleConfig { TableName = "*", ColumnName = "Status", CompareRule = "exact-ci" }
        ]);
        var comparer = new ColumnComparer(config);

        var old = CreateRow(new() { ["Status"] = "Active" });
        var @new = CreateRow(new() { ["Status"] = "ACTIVE" });

        var result = comparer.Compare(old, @new, "Order");

        result.Mismatches.Should().BeEmpty();
    }

    [Fact]
    public void Compare_ColumnsOnlyInOld_StillCompared()
    {
        var config = CreateConfig();
        var comparer = new ColumnComparer(config);

        var old = CreateRow(new() { ["Name"] = "John", ["ExtraCol"] = "x" });
        var @new = CreateRow(new() { ["Name"] = "John" });

        var result = comparer.Compare(old, @new, "Order");

        // ExtraCol: old has "x", new doesn't have it (null)
        result.Mismatches.Should().Contain(m => m.ColumnName == "ExtraCol");
    }

    [Fact]
    public void Compare_ColumnsOnlyInNew_StillCompared()
    {
        var config = CreateConfig();
        var comparer = new ColumnComparer(config);

        var old = CreateRow(new() { ["Name"] = "John" });
        var @new = CreateRow(new() { ["Name"] = "John", ["NewCol"] = "y" });

        var result = comparer.Compare(old, @new, "Order");

        result.Mismatches.Should().Contain(m => m.ColumnName == "NewCol");
    }
}
