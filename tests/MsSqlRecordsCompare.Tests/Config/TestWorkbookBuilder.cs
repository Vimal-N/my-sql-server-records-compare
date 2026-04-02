using ClosedXML.Excel;

namespace MsSqlRecordsCompare.Tests.Config;

/// <summary>
/// Helper to build test Excel workbooks for config reader tests.
/// </summary>
public class TestWorkbookBuilder
{
    private readonly XLWorkbook _workbook = new();

    public TestWorkbookBuilder WithConnectionConfig(
        string server = "TestServer\\Instance",
        string database = "TestDB",
        int timeout = 120,
        string? reportOutputPath = null)
    {
        var sheet = _workbook.Worksheets.Add("ConnectionConfig");
        sheet.Cell(1, 1).Value = "Setting";
        sheet.Cell(1, 2).Value = "Value";
        sheet.Cell(2, 1).Value = "ServerName";
        sheet.Cell(2, 2).Value = server;
        sheet.Cell(3, 1).Value = "DatabaseName";
        sheet.Cell(3, 2).Value = database;
        sheet.Cell(4, 1).Value = "CommandTimeout";
        sheet.Cell(4, 2).Value = timeout.ToString();
        if (reportOutputPath != null)
        {
            sheet.Cell(5, 1).Value = "ReportOutputPath";
            sheet.Cell(5, 2).Value = reportOutputPath;
        }
        return this;
    }

    public TestWorkbookBuilder WithTablesSheet(string sheetName, params (string TableName, string Schema, string? RecordIdColumn, string? RowMatchColumns, string? CustomQuery)[] tables)
    {
        var sheet = _workbook.Worksheets.Add(sheetName);
        sheet.Cell(1, 1).Value = "TableName";
        sheet.Cell(1, 2).Value = "Schema";
        sheet.Cell(1, 3).Value = "RecordIDColumn";
        sheet.Cell(1, 4).Value = "RowMatchColumns";
        sheet.Cell(1, 5).Value = "CustomQuery";

        for (int i = 0; i < tables.Length; i++)
        {
            var row = i + 2;
            sheet.Cell(row, 1).Value = tables[i].TableName;
            sheet.Cell(row, 2).Value = tables[i].Schema;
            if (tables[i].RecordIdColumn != null) sheet.Cell(row, 3).Value = tables[i].RecordIdColumn;
            if (tables[i].RowMatchColumns != null) sheet.Cell(row, 4).Value = tables[i].RowMatchColumns;
            if (tables[i].CustomQuery != null) sheet.Cell(row, 5).Value = tables[i].CustomQuery;
        }
        return this;
    }

    public TestWorkbookBuilder WithExclusions(params (string TableName, string ColumnName, string? Reason)[] exclusions)
    {
        var sheet = _workbook.Worksheets.Add("Exclusions");
        sheet.Cell(1, 1).Value = "TableName";
        sheet.Cell(1, 2).Value = "ColumnName";
        sheet.Cell(1, 3).Value = "Reason";

        for (int i = 0; i < exclusions.Length; i++)
        {
            var row = i + 2;
            sheet.Cell(row, 1).Value = exclusions[i].TableName;
            sheet.Cell(row, 2).Value = exclusions[i].ColumnName;
            if (exclusions[i].Reason != null) sheet.Cell(row, 3).Value = exclusions[i].Reason;
        }
        return this;
    }

    public TestWorkbookBuilder WithColumnRules(params (string TableName, string ColumnName, string CompareRule, string? Tolerance)[] rules)
    {
        var sheet = _workbook.Worksheets.Add("ColumnRules");
        sheet.Cell(1, 1).Value = "TableName";
        sheet.Cell(1, 2).Value = "ColumnName";
        sheet.Cell(1, 3).Value = "CompareRule";
        sheet.Cell(1, 4).Value = "Tolerance";

        for (int i = 0; i < rules.Length; i++)
        {
            var row = i + 2;
            sheet.Cell(row, 1).Value = rules[i].TableName;
            sheet.Cell(row, 2).Value = rules[i].ColumnName;
            sheet.Cell(row, 3).Value = rules[i].CompareRule;
            if (rules[i].Tolerance != null) sheet.Cell(row, 4).Value = rules[i].Tolerance;
        }
        return this;
    }

    public TestWorkbookBuilder WithComparisons(params (string Scenario, string OldId, string NewId)[] pairs)
    {
        var sheet = _workbook.Worksheets.Add("Comparisons");
        sheet.Cell(1, 1).Value = "Scenario";
        sheet.Cell(1, 2).Value = "OldRecordID";
        sheet.Cell(1, 3).Value = "NewRecordID";

        for (int i = 0; i < pairs.Length; i++)
        {
            var row = i + 2;
            sheet.Cell(row, 1).Value = pairs[i].Scenario;
            sheet.Cell(row, 2).Value = pairs[i].OldId;
            sheet.Cell(row, 3).Value = pairs[i].NewId;
        }
        return this;
    }

    public string SaveToTempFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid():N}.xlsx");
        _workbook.SaveAs(path);
        return path;
    }

    public void Dispose() => _workbook.Dispose();
}
