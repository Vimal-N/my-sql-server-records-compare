using ClosedXML.Excel;
using FluentAssertions;
using MsSqlRecordsCompare.Core.Config;

namespace MsSqlRecordsCompare.Tests.Config;

public class TemplateGeneratorTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var f in _tempFiles)
            if (File.Exists(f)) File.Delete(f);
    }

    private string GenerateTemplate()
    {
        var path = Path.Combine(Path.GetTempPath(), $"template_{Guid.NewGuid():N}.xlsx");
        _tempFiles.Add(path);
        new TemplateGenerator().Generate(path);
        return path;
    }

    [Fact]
    public void Generate_CreatesFile()
    {
        var path = GenerateTemplate();
        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public void Generate_ContainsAllRequiredSheets()
    {
        var path = GenerateTemplate();
        using var workbook = new XLWorkbook(path);

        var sheetNames = workbook.Worksheets.Select(ws => ws.Name).ToList();
        sheetNames.Should().Contain("ConnectionConfig");
        sheetNames.Should().Contain("Tables-Sample");
        sheetNames.Should().Contain("Exclusions");
        sheetNames.Should().Contain("ColumnRules");
        sheetNames.Should().Contain("Comparisons");
        sheetNames.Should().Contain("Instructions");
    }

    [Fact]
    public void Generate_ConnectionConfig_HasPlaceholders()
    {
        var path = GenerateTemplate();
        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheet("ConnectionConfig");

        sheet.Cell(2, 1).GetString().Should().Be("ServerName");
        sheet.Cell(2, 2).GetString().Should().Contain("YOUR_SERVER");
        sheet.Cell(3, 1).GetString().Should().Be("DatabaseName");
    }

    [Fact]
    public void Generate_TablesSheet_HasExamplePatterns()
    {
        var path = GenerateTemplate();
        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheet("Tables-Sample");

        // Headers
        sheet.Cell(1, 1).GetString().Should().Be("TableName");
        sheet.Cell(1, 5).GetString().Should().Be("CustomQuery");

        // Simple lookup example
        sheet.Cell(2, 1).GetString().Should().Be("Order");
        sheet.Cell(2, 3).GetString().Should().Be("RecordID");

        // Multi-row matching example
        sheet.Cell(3, 4).GetString().Should().Contain("ProductCode");

        // Custom query example
        sheet.Cell(4, 5).GetString().Should().Contain("@RecordID");
    }

    [Fact]
    public void Generate_Exclusions_HasCommonExamples()
    {
        var path = GenerateTemplate();
        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheet("Exclusions");

        var values = Enumerable.Range(2, 5)
            .Select(r => sheet.Cell(r, 2).GetString())
            .ToList();

        values.Should().Contain("CreatedDate");
        values.Should().Contain("ModifiedDate");
        values.Should().Contain("RecordID");
    }

    [Fact]
    public void Generate_ColumnRules_ShowsAllRuleTypes()
    {
        var path = GenerateTemplate();
        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheet("ColumnRules");

        var rules = Enumerable.Range(2, 6)
            .Select(r => sheet.Cell(r, 3).GetString())
            .ToList();

        rules.Should().Contain("currency");
        rules.Should().Contain("date");
        rules.Should().Contain("fuzzy");
        rules.Should().Contain("boolean");
    }

    [Fact]
    public void Generate_Instructions_HasSetupGuide()
    {
        var path = GenerateTemplate();
        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheet("Instructions");

        var firstCell = sheet.Cell(1, 1).GetString();
        firstCell.Should().Contain("Configuration Guide");
    }

    [Fact]
    public void Generate_Template_IsReadableByConfigReader()
    {
        var path = GenerateTemplate();

        // The template should be readable by ExcelConfigReader
        var reader = new ExcelConfigReader();
        var tableSets = reader.DiscoverTableSets(path);
        tableSets.Should().Contain("Tables-Sample");

        var config = reader.Read(path, "Tables-Sample");
        config.Tables.Should().HaveCount(4);
        config.Exclusions.Should().HaveCount(5);
    }
}
