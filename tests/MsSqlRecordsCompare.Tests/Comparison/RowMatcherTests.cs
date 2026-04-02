using FluentAssertions;
using MsSqlRecordsCompare.Core.Comparison;
using MsSqlRecordsCompare.Core.Database.Models;

namespace MsSqlRecordsCompare.Tests.Comparison;

public class RowMatcherTests
{
    private readonly RowMatcher _sut = new();

    private static TableRecord CreateRow(string tableName, string recordId, Dictionary<string, object?> columns)
    {
        return new TableRecord
        {
            TableName = tableName,
            RecordId = recordId,
            Columns = columns
        };
    }

    // --- Single Row Matching (empty RowMatchColumns) ---

    [Fact]
    public void SingleRow_BothHaveOneRow_MatchesThem()
    {
        var old = new List<TableRecord> { CreateRow("Order", "1", new() { ["Name"] = "Test" }) };
        var @new = new List<TableRecord> { CreateRow("Order", "2", new() { ["Name"] = "Test" }) };

        var result = _sut.Match(old, @new, []);

        result.MatchedPairs.Should().HaveCount(1);
        result.UnmatchedOldRows.Should().BeEmpty();
        result.UnmatchedNewRows.Should().BeEmpty();
    }

    [Fact]
    public void SingleRow_BothEmpty_NoMatches()
    {
        var result = _sut.Match([], [], []);
        result.MatchedPairs.Should().BeEmpty();
        result.UnmatchedOldRows.Should().BeEmpty();
        result.UnmatchedNewRows.Should().BeEmpty();
    }

    [Fact]
    public void SingleRow_OldEmpty_NewUnmatched()
    {
        var @new = new List<TableRecord> { CreateRow("Order", "2", new() { ["Name"] = "Test" }) };
        var result = _sut.Match([], @new, []);

        result.MatchedPairs.Should().BeEmpty();
        result.UnmatchedNewRows.Should().HaveCount(1);
    }

    [Fact]
    public void SingleRow_NewEmpty_OldUnmatched()
    {
        var old = new List<TableRecord> { CreateRow("Order", "1", new() { ["Name"] = "Test" }) };
        var result = _sut.Match(old, [], []);

        result.MatchedPairs.Should().BeEmpty();
        result.UnmatchedOldRows.Should().HaveCount(1);
    }

    [Fact]
    public void SingleRow_MultipleOldRows_WarnsAndMatchesFirst()
    {
        var old = new List<TableRecord>
        {
            CreateRow("Order", "1", new() { ["Name"] = "First" }),
            CreateRow("Order", "1", new() { ["Name"] = "Second" })
        };
        var @new = new List<TableRecord> { CreateRow("Order", "2", new() { ["Name"] = "Test" }) };

        var result = _sut.Match(old, @new, []);

        result.MatchedPairs.Should().HaveCount(1);
        result.MatchedPairs[0].OldRow.Columns["Name"].Should().Be("First");
        result.Warnings.Should().Contain(w => w.Contains("Expected single row") && w.Contains("old record"));
    }

    // --- Multi-Row Matching (with RowMatchColumns) ---

    [Fact]
    public void MultiRow_MatchesByKey()
    {
        var old = new List<TableRecord>
        {
            CreateRow("OrderLine", "1", new() { ["ProductCode"] = "SKU-100", ["Qty"] = 5 }),
            CreateRow("OrderLine", "1", new() { ["ProductCode"] = "SKU-200", ["Qty"] = 2 })
        };
        var @new = new List<TableRecord>
        {
            CreateRow("OrderLine", "2", new() { ["ProductCode"] = "SKU-200", ["Qty"] = 2 }),
            CreateRow("OrderLine", "2", new() { ["ProductCode"] = "SKU-100", ["Qty"] = 5 })
        };

        var result = _sut.Match(old, @new, ["ProductCode"]);

        result.MatchedPairs.Should().HaveCount(2);
        result.UnmatchedOldRows.Should().BeEmpty();
        result.UnmatchedNewRows.Should().BeEmpty();
    }

    [Fact]
    public void MultiRow_CompositeKey_Matches()
    {
        var old = new List<TableRecord>
        {
            CreateRow("OrderLine", "1", new() { ["ProductCode"] = "SKU-100", ["SizeCode"] = "LG", ["Price"] = 29.99m }),
            CreateRow("OrderLine", "1", new() { ["ProductCode"] = "SKU-100", ["SizeCode"] = "MD", ["Price"] = 24.99m })
        };
        var @new = new List<TableRecord>
        {
            CreateRow("OrderLine", "2", new() { ["ProductCode"] = "SKU-100", ["SizeCode"] = "MD", ["Price"] = 24.99m }),
            CreateRow("OrderLine", "2", new() { ["ProductCode"] = "SKU-100", ["SizeCode"] = "LG", ["Price"] = 29.99m })
        };

        var result = _sut.Match(old, @new, ["ProductCode", "SizeCode"]);

        result.MatchedPairs.Should().HaveCount(2);
        // Verify correct pairing by checking matched keys
        var lgPair = result.MatchedPairs.First(p => p.MatchKey!.Contains("LG"));
        lgPair.OldRow.Columns["Price"].Should().Be(29.99m);
        lgPair.NewRow.Columns["Price"].Should().Be(29.99m);
    }

    [Fact]
    public void MultiRow_UnmatchedOldRow_ReportedAsMissing()
    {
        var old = new List<TableRecord>
        {
            CreateRow("OrderLine", "1", new() { ["ProductCode"] = "SKU-100" }),
            CreateRow("OrderLine", "1", new() { ["ProductCode"] = "SKU-200" }),
            CreateRow("OrderLine", "1", new() { ["ProductCode"] = "SKU-300" })
        };
        var @new = new List<TableRecord>
        {
            CreateRow("OrderLine", "2", new() { ["ProductCode"] = "SKU-100" }),
            CreateRow("OrderLine", "2", new() { ["ProductCode"] = "SKU-200" })
        };

        var result = _sut.Match(old, @new, ["ProductCode"]);

        result.MatchedPairs.Should().HaveCount(2);
        result.UnmatchedOldRows.Should().HaveCount(1);
        result.UnmatchedOldRows[0].Columns["ProductCode"].Should().Be("SKU-300");
    }

    [Fact]
    public void MultiRow_UnmatchedNewRow_ReportedAsExtra()
    {
        var old = new List<TableRecord>
        {
            CreateRow("OrderLine", "1", new() { ["ProductCode"] = "SKU-100" })
        };
        var @new = new List<TableRecord>
        {
            CreateRow("OrderLine", "2", new() { ["ProductCode"] = "SKU-100" }),
            CreateRow("OrderLine", "2", new() { ["ProductCode"] = "SKU-999" })
        };

        var result = _sut.Match(old, @new, ["ProductCode"]);

        result.MatchedPairs.Should().HaveCount(1);
        result.UnmatchedNewRows.Should().HaveCount(1);
    }

    [Fact]
    public void MultiRow_DuplicateKeys_WarnsAndMatchesFirst()
    {
        var old = new List<TableRecord>
        {
            CreateRow("OrderLine", "1", new() { ["ProductCode"] = "SKU-100", ["Qty"] = 5 }),
            CreateRow("OrderLine", "1", new() { ["ProductCode"] = "SKU-100", ["Qty"] = 10 })
        };
        var @new = new List<TableRecord>
        {
            CreateRow("OrderLine", "2", new() { ["ProductCode"] = "SKU-100", ["Qty"] = 5 })
        };

        var result = _sut.Match(old, @new, ["ProductCode"]);

        result.MatchedPairs.Should().HaveCount(1);
        result.Warnings.Should().Contain(w => w.Contains("Duplicate key") && w.Contains("old record"));
    }

    [Fact]
    public void MultiRow_CaseInsensitiveKeyMatching()
    {
        var old = new List<TableRecord>
        {
            CreateRow("OrderLine", "1", new() { ["ProductCode"] = "sku-100" })
        };
        var @new = new List<TableRecord>
        {
            CreateRow("OrderLine", "2", new() { ["ProductCode"] = "SKU-100" })
        };

        var result = _sut.Match(old, @new, ["ProductCode"]);

        result.MatchedPairs.Should().HaveCount(1);
    }

    [Fact]
    public void MultiRow_NullKeyValues_MatchAsNull()
    {
        var old = new List<TableRecord>
        {
            CreateRow("Payment", "1", new() { ["PaymentType"] = null, ["Amount"] = 100m })
        };
        var @new = new List<TableRecord>
        {
            CreateRow("Payment", "2", new() { ["PaymentType"] = null, ["Amount"] = 100m })
        };

        var result = _sut.Match(old, @new, ["PaymentType"]);

        result.MatchedPairs.Should().HaveCount(1);
    }

    [Fact]
    public void MultiRow_BothEmpty_NoMatchesNoWarnings()
    {
        var result = _sut.Match([], [], ["ProductCode"]);
        result.MatchedPairs.Should().BeEmpty();
        result.UnmatchedOldRows.Should().BeEmpty();
        result.UnmatchedNewRows.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void MultiRow_OldEmpty_AllNewUnmatched()
    {
        var @new = new List<TableRecord>
        {
            CreateRow("OrderLine", "2", new() { ["ProductCode"] = "SKU-100" }),
            CreateRow("OrderLine", "2", new() { ["ProductCode"] = "SKU-200" })
        };

        var result = _sut.Match([], @new, ["ProductCode"]);

        result.MatchedPairs.Should().BeEmpty();
        result.UnmatchedNewRows.Should().HaveCount(2);
    }
}
