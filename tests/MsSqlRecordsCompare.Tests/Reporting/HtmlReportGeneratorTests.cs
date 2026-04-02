using FluentAssertions;
using MsSqlRecordsCompare.Core.Reporting;
using MsSqlRecordsCompare.Tests.TestHelpers;

namespace MsSqlRecordsCompare.Tests.Reporting;

public class HtmlReportGeneratorTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var f in _tempFiles)
            if (File.Exists(f)) File.Delete(f);
    }

    [Fact]
    public void Generate_PassingResult_ContainsPassedStatus()
    {
        var result = MockComparisonData.CreatePassingResult();
        var html = new HtmlReportGenerator().Generate(result);

        html.Should().Contain("PASSED");
        html.Should().Contain("1/1");
        html.Should().Contain("Basic-Order");
    }

    [Fact]
    public void Generate_WithMismatches_ContainsMismatchDetails()
    {
        var result = MockComparisonData.CreateResultWithMismatches();
        var html = new HtmlReportGenerator().Generate(result);

        html.Should().Contain("ShippingMethod");
        html.Should().Contain("Express");
        html.Should().Contain("EXPRESS");
        html.Should().Contain("DIFF");
        html.Should().Contain("DiscountCode");
    }

    [Fact]
    public void Generate_ContainsMetadata()
    {
        var result = MockComparisonData.CreatePassingResult();
        var html = new HtmlReportGenerator().Generate(result);

        html.Should().Contain("TestServer");
        html.Should().Contain("TestDB");
        html.Should().Contain("DOMAIN\\testuser");
        html.Should().Contain("Tables-Orders");
    }

    [Fact]
    public void Generate_ContainsPrintStylesheet()
    {
        var result = MockComparisonData.CreatePassingResult();
        var html = new HtmlReportGenerator().Generate(result);

        html.Should().Contain("@media print");
        html.Should().Contain("page-break");
    }

    [Fact]
    public void Generate_ContainsSavePdfButton()
    {
        var result = MockComparisonData.CreatePassingResult();
        var html = new HtmlReportGenerator().Generate(result);

        html.Should().Contain("Save as PDF");
        html.Should().Contain("window.print()");
    }

    [Fact]
    public void Generate_ContainsFilterButtons()
    {
        var result = MockComparisonData.CreatePassingResult();
        var html = new HtmlReportGenerator().Generate(result);

        html.Should().Contain("Show All");
        html.Should().Contain("Mismatches Only");
    }

    [Fact]
    public void Generate_MultipleScenarios_ContainsOverviewTable()
    {
        var result = MockComparisonData.CreateResultWithMismatches();
        var html = new HtmlReportGenerator().Generate(result);

        html.Should().Contain("Scenario Overview");
        html.Should().Contain("Mismatch Summary by Table");
        html.Should().Contain("Express-Shipping");
    }

    [Fact]
    public void Generate_ContainsExclusionAudit()
    {
        var result = MockComparisonData.CreatePassingResult();
        var html = new HtmlReportGenerator().Generate(result);

        html.Should().Contain("Exclusions Applied");
        html.Should().Contain("CreatedDate");
        html.Should().Contain("Timestamps");
    }

    [Fact]
    public void Generate_IsSelfContained_NoCdnReferences()
    {
        var result = MockComparisonData.CreatePassingResult();
        var html = new HtmlReportGenerator().Generate(result);

        html.Should().NotContain("cdn.");
        html.Should().NotContain("googleapis");
        html.Should().NotContain("bootstrapcdn");
    }

    [Fact]
    public void Generate_ContainsMatchIcons()
    {
        var result = MockComparisonData.CreateResultWithMismatches();
        var html = new HtmlReportGenerator().Generate(result);

        html.Should().Contain("✓");
        html.Should().Contain("✗");
    }

    [Fact]
    public void GenerateToFile_CreatesFile()
    {
        var result = MockComparisonData.CreatePassingResult();
        var path = Path.Combine(Path.GetTempPath(), $"test_report_{Guid.NewGuid():N}.html");
        _tempFiles.Add(path);

        new HtmlReportGenerator().GenerateToFile(result, path);

        File.Exists(path).Should().BeTrue();
        var content = File.ReadAllText(path);
        content.Should().Contain("<!DOCTYPE html>");
    }
}
