using FluentAssertions;
using MsSqlRecordsCompare.Core.Config;

namespace MsSqlRecordsCompare.Tests.Config;

public class ComparisonConfigTests
{
    private ComparisonConfig CreateConfig(
        List<ExclusionConfig>? exclusions = null,
        List<ColumnRuleConfig>? columnRules = null)
    {
        return new ComparisonConfig
        {
            Connection = new ConnectionConfig
            {
                ServerName = "TestServer",
                DatabaseName = "TestDB"
            },
            SelectedTableSet = "Tables",
            Tables = [new TableConfig { TableName = "Order", RecordIdColumn = "RecordID" }],
            Exclusions = exclusions ?? [],
            ColumnRules = columnRules ?? []
        };
    }

    // --- IsColumnExcluded ---

    [Fact]
    public void IsColumnExcluded_WildcardMatch_ReturnsTrue()
    {
        var config = CreateConfig(exclusions:
        [
            new ExclusionConfig { TableName = "*", ColumnName = "CreatedDate" }
        ]);

        config.IsColumnExcluded("Order", "CreatedDate").Should().BeTrue();
        config.IsColumnExcluded("Payment", "CreatedDate").Should().BeTrue();
    }

    [Fact]
    public void IsColumnExcluded_SpecificTableMatch_ReturnsTrue()
    {
        var config = CreateConfig(exclusions:
        [
            new ExclusionConfig { TableName = "OrderLine", ColumnName = "LineItemOID" }
        ]);

        config.IsColumnExcluded("OrderLine", "LineItemOID").Should().BeTrue();
        config.IsColumnExcluded("Order", "LineItemOID").Should().BeFalse();
    }

    [Fact]
    public void IsColumnExcluded_CaseInsensitive()
    {
        var config = CreateConfig(exclusions:
        [
            new ExclusionConfig { TableName = "*", ColumnName = "createddate" }
        ]);

        config.IsColumnExcluded("Order", "CreatedDate").Should().BeTrue();
    }

    [Fact]
    public void IsColumnExcluded_NoMatch_ReturnsFalse()
    {
        var config = CreateConfig(exclusions:
        [
            new ExclusionConfig { TableName = "*", ColumnName = "CreatedDate" }
        ]);

        config.IsColumnExcluded("Order", "TotalAmount").Should().BeFalse();
    }

    // --- GetCompareRule ---

    [Fact]
    public void GetCompareRule_SpecificTableAndColumn_ReturnsRule()
    {
        var config = CreateConfig(columnRules:
        [
            new ColumnRuleConfig { TableName = "Payment", ColumnName = "TotalAmount", CompareRule = "currency" }
        ]);

        config.GetCompareRule("Payment", "TotalAmount").Should().Be("currency");
    }

    [Fact]
    public void GetCompareRule_WildcardTableSpecificColumn_ReturnsRule()
    {
        var config = CreateConfig(columnRules:
        [
            new ColumnRuleConfig { TableName = "*", ColumnName = "FullName", CompareRule = "fuzzy" }
        ]);

        config.GetCompareRule("Customer", "FullName").Should().Be("fuzzy");
        config.GetCompareRule("Employee", "FullName").Should().Be("fuzzy");
    }

    [Fact]
    public void GetCompareRule_SpecificTableWildcardColumn_ReturnsRule()
    {
        var config = CreateConfig(columnRules:
        [
            new ColumnRuleConfig { TableName = "AuditLog", ColumnName = "*", CompareRule = "exact-ci" }
        ]);

        config.GetCompareRule("AuditLog", "AnyColumn").Should().Be("exact-ci");
    }

    [Fact]
    public void GetCompareRule_SpecificOverridesWildcard()
    {
        var config = CreateConfig(columnRules:
        [
            new ColumnRuleConfig { TableName = "Payment", ColumnName = "TotalAmount", CompareRule = "currency" },
            new ColumnRuleConfig { TableName = "*", ColumnName = "TotalAmount", CompareRule = "numeric" }
        ]);

        // Specific table+column should win over wildcard table
        config.GetCompareRule("Payment", "TotalAmount").Should().Be("currency");
        // Other tables should get the wildcard rule
        config.GetCompareRule("Order", "TotalAmount").Should().Be("numeric");
    }

    [Fact]
    public void GetCompareRule_NoMatch_ReturnsExact()
    {
        var config = CreateConfig(columnRules: []);
        config.GetCompareRule("Order", "SomeColumn").Should().Be("exact");
    }

    // --- GetExclusionsForTable ---

    [Fact]
    public void GetExclusionsForTable_ReturnsWildcardAndSpecific()
    {
        var config = CreateConfig(exclusions:
        [
            new ExclusionConfig { TableName = "*", ColumnName = "CreatedDate", Reason = "Timestamps" },
            new ExclusionConfig { TableName = "Order", ColumnName = "InternalFlag", Reason = "Internal" },
            new ExclusionConfig { TableName = "Payment", ColumnName = "AuthToken", Reason = "Security" }
        ]);

        var orderExclusions = config.GetExclusionsForTable("Order");
        orderExclusions.Should().HaveCount(2); // wildcard + specific
        orderExclusions.Select(e => e.ColumnName).Should().Contain("CreatedDate");
        orderExclusions.Select(e => e.ColumnName).Should().Contain("InternalFlag");

        var paymentExclusions = config.GetExclusionsForTable("Payment");
        paymentExclusions.Should().HaveCount(2); // wildcard + specific
    }

    // --- Validate ---

    [Fact]
    public void Validate_NoTables_Throws()
    {
        var config = new ComparisonConfig
        {
            Connection = new ConnectionConfig { ServerName = "S", DatabaseName = "D" },
            SelectedTableSet = "Tables",
            Tables = []
        };

        var act = () => config.Validate();
        act.Should().Throw<ConfigValidationException>()
            .WithMessage("*No tables*");
    }

    [Fact]
    public void Validate_InvalidColumnRule_Throws()
    {
        var config = CreateConfig(columnRules:
        [
            new ColumnRuleConfig { TableName = "*", ColumnName = "*", CompareRule = "invalid_rule" }
        ]);

        var act = () => config.Validate();
        act.Should().Throw<ConfigValidationException>()
            .WithMessage("*Invalid CompareRule*");
    }

    [Fact]
    public void Validate_ValidConfig_DoesNotThrow()
    {
        var config = CreateConfig(
            exclusions: [new ExclusionConfig { TableName = "*", ColumnName = "CreatedDate" }],
            columnRules: [new ColumnRuleConfig { TableName = "*", ColumnName = "*", CompareRule = "exact" }]);

        var act = () => config.Validate();
        act.Should().NotThrow();
    }
}
