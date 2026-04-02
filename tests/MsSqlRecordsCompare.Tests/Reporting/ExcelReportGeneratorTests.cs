using ClosedXML.Excel;
using FluentAssertions;
using MsSqlRecordsCompare.Core.Reporting;
using MsSqlRecordsCompare.Tests.TestHelpers;

namespace MsSqlRecordsCompare.Tests.Reporting;

public class ExcelReportGeneratorTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var f in _tempFiles)
            if (File.Exists(f)) File.Delete(f);
    }

    [Fact]
    public void GenerateToFile_CreatesFile()
    {
        var result = MockComparisonData.CreateResultWithMismatches();
        var path = Path.Combine(Path.GetTempPath(), $"test_excel_{Guid.NewGuid():N}.xlsx");
        _tempFiles.Add(path);

        new ExcelReportGenerator().GenerateToFile(result, path);

        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public void GenerateToFile_ContainsMismatchSheet()
    {
        var result = MockComparisonData.CreateResultWithMismatches();
        var path = Path.Combine(Path.GetTempPath(), $"test_excel_{Guid.NewGuid():N}.xlsx");
        _tempFiles.Add(path);

        new ExcelReportGenerator().GenerateToFile(result, path);

        using var workbook = new XLWorkbook(path);
        workbook.Worksheets.Should().Contain(ws => ws.Name == "Mismatches");
    }

    [Fact]
    public void GenerateToFile_ContainsSummarySheet()
    {
        var result = MockComparisonData.CreateResultWithMismatches();
        var path = Path.Combine(Path.GetTempPath(), $"test_excel_{Guid.NewGuid():N}.xlsx");
        _tempFiles.Add(path);

        new ExcelReportGenerator().GenerateToFile(result, path);

        using var workbook = new XLWorkbook(path);
        workbook.Worksheets.Should().Contain(ws => ws.Name == "Summary");
    }

    [Fact]
    public void GenerateToFile_MismatchRows_HaveCorrectData()
    {
        var result = MockComparisonData.CreateResultWithMismatches();
        var path = Path.Combine(Path.GetTempPath(), $"test_excel_{Guid.NewGuid():N}.xlsx");
        _tempFiles.Add(path);

        new ExcelReportGenerator().GenerateToFile(result, path);

        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheet("Mismatches");

        // Header row
        sheet.Cell(1, 1).GetString().Should().Be("Scenario");
        sheet.Cell(1, 6).GetString().Should().Be("Column");

        // First mismatch row
        sheet.Cell(2, 1).GetString().Should().Be("Basic-Order");
        sheet.Cell(2, 6).GetString().Should().Be("ShippingMethod");
        sheet.Cell(2, 7).GetString().Should().Be("Express");
        sheet.Cell(2, 8).GetString().Should().Be("EXPRESS");
    }

    [Fact]
    public void GenerateToFile_PassingResult_NoMismatchRows()
    {
        var result = MockComparisonData.CreatePassingResult();
        var path = Path.Combine(Path.GetTempPath(), $"test_excel_{Guid.NewGuid():N}.xlsx");
        _tempFiles.Add(path);

        new ExcelReportGenerator().GenerateToFile(result, path);

        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheet("Mismatches");
        // Only header row, no data
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        lastRow.Should().Be(1);
    }
}
