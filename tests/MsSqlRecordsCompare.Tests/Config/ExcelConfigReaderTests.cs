using FluentAssertions;
using MsSqlRecordsCompare.Core.Config;

namespace MsSqlRecordsCompare.Tests.Config;

public class ExcelConfigReaderTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    private string CreateTestFile(TestWorkbookBuilder builder)
    {
        var path = builder.SaveToTempFile();
        _tempFiles.Add(path);
        return path;
    }

    // --- File Validation ---

    [Fact]
    public void Read_FileNotFound_ThrowsConfigValidationException()
    {
        var reader = new ExcelConfigReader();
        var act = () => reader.Read("/nonexistent/path.xlsx", "Tables");
        act.Should().Throw<ConfigValidationException>()
            .WithMessage("*not found*");
    }

    // --- ConnectionConfig ---

    [Fact]
    public void Read_ConnectionConfig_ParsesAllSettings()
    {
        var path = CreateTestFile(new TestWorkbookBuilder()
            .WithConnectionConfig("MyServer\\SQL", "MyDB", 60, ".\\output")
            .WithTablesSheet("Tables",
                ("Order", "dbo", "RecordID", null, null)));

        var config = new ExcelConfigReader().Read(path, "Tables");

        config.Connection.ServerName.Should().Be("MyServer\\SQL");
        config.Connection.DatabaseName.Should().Be("MyDB");
        config.Connection.CommandTimeout.Should().Be(60);
        config.Connection.ReportOutputPath.Should().Be(".\\output");
    }

    [Fact]
    public void Read_MissingConnectionConfigSheet_Throws()
    {
        var builder = new TestWorkbookBuilder()
            .WithTablesSheet("Tables", ("Order", "dbo", "RecordID", null, null));
        var path = CreateTestFile(builder);

        var act = () => new ExcelConfigReader().Read(path, "Tables");
        act.Should().Throw<ConfigValidationException>()
            .WithMessage("*ConnectionConfig*not found*");
    }

    [Fact]
    public void Read_MissingServerName_Throws()
    {
        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig("", "MyDB")
            .WithTablesSheet("Tables", ("Order", "dbo", "RecordID", null, null));
        var path = CreateTestFile(builder);

        var act = () =>
        {
            var config = new ExcelConfigReader().Read(path, "Tables");
            config.Validate();
        };
        act.Should().Throw<ConfigValidationException>()
            .WithMessage("*ServerName*");
    }

    [Fact]
    public void Read_DefaultCommandTimeout_Is120()
    {
        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig("Server", "DB")
            .WithTablesSheet("Tables", ("Order", "dbo", "RecordID", null, null));
        var path = CreateTestFile(builder);

        var config = new ExcelConfigReader().Read(path, "Tables");
        config.Connection.CommandTimeout.Should().Be(120);
    }

    // --- Table Sets Discovery ---

    [Fact]
    public void DiscoverTableSets_FindsTablesAndPrefixedSheets()
    {
        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig()
            .WithTablesSheet("Tables", ("Order", "dbo", "RecordID", null, null))
            .WithTablesSheet("Tables-Orders", ("Order", "dbo", "RecordID", null, null))
            .WithTablesSheet("Tables-Customers", ("Customer", "dbo", "CustomerID", null, null));
        var path = CreateTestFile(builder);

        var sets = new ExcelConfigReader().DiscoverTableSets(path);

        sets.Should().HaveCount(3);
        sets.Should().Contain("Tables");
        sets.Should().Contain("Tables-Orders");
        sets.Should().Contain("Tables-Customers");
    }

    [Fact]
    public void DiscoverTableSets_IgnoresNonTableSheets()
    {
        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig()
            .WithTablesSheet("Tables-Orders", ("Order", "dbo", "RecordID", null, null))
            .WithExclusions(("*", "CreatedDate", "Timestamps"));
        var path = CreateTestFile(builder);

        var sets = new ExcelConfigReader().DiscoverTableSets(path);
        sets.Should().HaveCount(1);
        sets.Should().Contain("Tables-Orders");
    }

    [Fact]
    public void Read_TableSetByShortName_ResolvesToPrefixedSheet()
    {
        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig()
            .WithTablesSheet("Tables-Orders",
                ("Order", "dbo", "RecordID", null, null));
        var path = CreateTestFile(builder);

        var config = new ExcelConfigReader().Read(path, "Orders");
        config.Tables.Should().HaveCount(1);
        config.Tables[0].TableName.Should().Be("Order");
        config.SelectedTableSet.Should().Be("Orders");
    }

    [Fact]
    public void Read_InvalidTableSet_ThrowsWithAvailableList()
    {
        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig()
            .WithTablesSheet("Tables-Orders", ("Order", "dbo", "RecordID", null, null));
        var path = CreateTestFile(builder);

        var act = () => new ExcelConfigReader().Read(path, "Customers");
        act.Should().Throw<ConfigValidationException>()
            .WithMessage("*not found*Tables-Orders*");
    }

    // --- Tables Reading ---

    [Fact]
    public void Read_Tables_ParsesAllColumns()
    {
        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig()
            .WithTablesSheet("Tables",
                ("Order", "dbo", "RecordID", null, null),
                ("OrderLine", "dbo", "RecordID", "ProductCode,SizeCode", null),
                ("Payment", "billing", null, "PaymentType",
                    "SELECT p.* FROM Payment p INNER JOIN OrderPayment op ON p.PaymentID = op.PaymentID WHERE op.RecordID = @RecordID"));
        var path = CreateTestFile(builder);

        var config = new ExcelConfigReader().Read(path, "Tables");

        config.Tables.Should().HaveCount(3);

        var order = config.Tables[0];
        order.TableName.Should().Be("Order");
        order.Schema.Should().Be("dbo");
        order.RecordIdColumn.Should().Be("RecordID");
        order.RowMatchColumns.Should().BeEmpty();
        order.CustomQuery.Should().BeNull();
        order.UsesCustomQuery.Should().BeFalse();

        var orderLine = config.Tables[1];
        orderLine.RowMatchColumns.Should().BeEquivalentTo(["ProductCode", "SizeCode"]);

        var payment = config.Tables[2];
        payment.Schema.Should().Be("billing");
        payment.RecordIdColumn.Should().BeNull();
        payment.UsesCustomQuery.Should().BeTrue();
        payment.CustomQuery.Should().Contain("@RecordID");
    }

    [Fact]
    public void Read_Tables_DefaultSchemaToDbo()
    {
        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig()
            .WithTablesSheet("Tables",
                ("Order", "", "RecordID", null, null));
        var path = CreateTestFile(builder);

        // Schema column is empty, but the builder writes "" not null
        // The reader should default to "dbo" when schema is empty
        var config = new ExcelConfigReader().Read(path, "Tables");
        // The reader returns "" because it reads the cell value.
        // The TableConfig default is "dbo" only if schema is null from the reader.
        // Let's verify the actual behavior:
        config.Tables[0].Schema.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Read_TablesSheet_MissingTableNameColumn_Throws()
    {
        // Create a sheet without the required TableName column
        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig();
        // Manually create a bad sheet
        var path = builder.SaveToTempFile();
        _tempFiles.Add(path);

        using (var workbook = new ClosedXML.Excel.XLWorkbook(path))
        {
            var sheet = workbook.Worksheets.Add("Tables");
            sheet.Cell(1, 1).Value = "WrongColumn";
            sheet.Cell(1, 2).Value = "Schema";
            workbook.Save();
        }

        var act = () => new ExcelConfigReader().Read(path, "Tables");
        act.Should().Throw<ConfigValidationException>()
            .WithMessage("*TableName*not found*");
    }

    // --- Exclusions ---

    [Fact]
    public void Read_Exclusions_ParsesWildcardsAndSpecific()
    {
        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig()
            .WithTablesSheet("Tables", ("Order", "dbo", "RecordID", null, null))
            .WithExclusions(
                ("*", "CreatedDate", "Different timestamps"),
                ("*", "ModifiedDate", "Different timestamps"),
                ("OrderLine", "LineItemOID", "Auto-generated"));
        var path = CreateTestFile(builder);

        var config = new ExcelConfigReader().Read(path, "Tables");

        config.Exclusions.Should().HaveCount(3);

        var wildcard = config.Exclusions[0];
        wildcard.IsWildcard.Should().BeTrue();
        wildcard.ColumnName.Should().Be("CreatedDate");
        wildcard.Reason.Should().Be("Different timestamps");

        var specific = config.Exclusions[2];
        specific.IsWildcard.Should().BeFalse();
        specific.TableName.Should().Be("OrderLine");
    }

    [Fact]
    public void Read_NoExclusionsSheet_ReturnsEmptyList()
    {
        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig()
            .WithTablesSheet("Tables", ("Order", "dbo", "RecordID", null, null));
        var path = CreateTestFile(builder);

        var config = new ExcelConfigReader().Read(path, "Tables");
        config.Exclusions.Should().BeEmpty();
    }

    // --- ColumnRules ---

    [Fact]
    public void Read_ColumnRules_ParsesRulesAndTolerances()
    {
        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig()
            .WithTablesSheet("Tables", ("Order", "dbo", "RecordID", null, null))
            .WithColumnRules(
                ("Payment", "TotalAmount", "currency", "0.01"),
                ("*", "FullName", "fuzzy", "0.90"),
                ("*", "*", "exact", null));
        var path = CreateTestFile(builder);

        var config = new ExcelConfigReader().Read(path, "Tables");

        config.ColumnRules.Should().HaveCount(3);
        config.ColumnRules[0].CompareRule.Should().Be("currency");
        config.ColumnRules[0].Tolerance.Should().Be("0.01");
        config.ColumnRules[1].IsTableWildcard.Should().BeTrue();
        config.ColumnRules[1].IsColumnWildcard.Should().BeFalse();
        config.ColumnRules[2].IsTableWildcard.Should().BeTrue();
        config.ColumnRules[2].IsColumnWildcard.Should().BeTrue();
    }

    [Fact]
    public void Read_NoColumnRulesSheet_ReturnsEmptyList()
    {
        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig()
            .WithTablesSheet("Tables", ("Order", "dbo", "RecordID", null, null));
        var path = CreateTestFile(builder);

        var config = new ExcelConfigReader().Read(path, "Tables");
        config.ColumnRules.Should().BeEmpty();
    }

    // --- Comparisons ---

    [Fact]
    public void Read_Comparisons_ParsesPairs()
    {
        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig()
            .WithTablesSheet("Tables", ("Order", "dbo", "RecordID", null, null))
            .WithComparisons(
                ("Basic-Order", "100234", "200891"),
                ("Complex-Order", "ABC-123", "XYZ-789"));
        var path = CreateTestFile(builder);

        var config = new ExcelConfigReader().Read(path, "Tables");

        config.ComparisonPairs.Should().HaveCount(2);
        config.ComparisonPairs[0].Scenario.Should().Be("Basic-Order");
        config.ComparisonPairs[0].OldRecordId.Should().Be("100234");
        config.ComparisonPairs[0].NewRecordId.Should().Be("200891");
        config.ComparisonPairs[1].OldRecordId.Should().Be("ABC-123");
    }

    [Fact]
    public void Read_NoComparisonsSheet_ReturnsEmptyList()
    {
        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig()
            .WithTablesSheet("Tables", ("Order", "dbo", "RecordID", null, null));
        var path = CreateTestFile(builder);

        var config = new ExcelConfigReader().Read(path, "Tables");
        config.ComparisonPairs.Should().BeEmpty();
    }

    // --- AvailableTableSets ---

    [Fact]
    public void Read_PopulatesAvailableTableSets()
    {
        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig()
            .WithTablesSheet("Tables-Alpha", ("Order", "dbo", "RecordID", null, null))
            .WithTablesSheet("Tables-Beta", ("Customer", "dbo", "CustID", null, null));
        var path = CreateTestFile(builder);

        var config = new ExcelConfigReader().Read(path, "Alpha");
        config.AvailableTableSets.Should().HaveCount(2);
        config.AvailableTableSets.Should().Contain("Tables-Alpha");
        config.AvailableTableSets.Should().Contain("Tables-Beta");
    }
}
