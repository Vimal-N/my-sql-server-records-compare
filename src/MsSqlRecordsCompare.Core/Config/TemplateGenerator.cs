using ClosedXML.Excel;

namespace MsSqlRecordsCompare.Core.Config;

public class TemplateGenerator
{
    public void Generate(string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        using var workbook = new XLWorkbook();

        CreateConnectionConfigSheet(workbook);
        CreateTablesSheet(workbook);
        CreateExclusionsSheet(workbook);
        CreateColumnRulesSheet(workbook);
        CreateComparisonsSheet(workbook);
        CreateInstructionsSheet(workbook);

        workbook.SaveAs(filePath);
    }

    private static void CreateConnectionConfigSheet(IXLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("ConnectionConfig");
        SetHeaders(sheet, "Setting", "Value");

        sheet.Cell(2, 1).Value = "ServerName";
        sheet.Cell(2, 2).Value = "YOUR_SERVER\\INSTANCE";
        AddComment(sheet.Cell(2, 2), "Replace with your SQL Server instance name");

        sheet.Cell(3, 1).Value = "DatabaseName";
        sheet.Cell(3, 2).Value = "YourDatabase";
        AddComment(sheet.Cell(3, 2), "Replace with your database name");

        sheet.Cell(4, 1).Value = "CommandTimeout";
        sheet.Cell(4, 2).Value = "120";
        AddComment(sheet.Cell(4, 2), "Query timeout in seconds. Increase for large tables.");

        sheet.Cell(5, 1).Value = "UserName";
        sheet.Cell(5, 2).Value = "";
        AddComment(sheet.Cell(5, 2), "Optional. SQL Server user name. Leave empty for Windows Integrated Auth.");

        sheet.Cell(6, 1).Value = "Password";
        sheet.Cell(6, 2).Value = "";
        AddComment(sheet.Cell(6, 2), "Optional. SQL Server password. Used with UserName for SQL Auth.");

        sheet.Cell(7, 1).Value = "ReportOutputPath";
        sheet.Cell(7, 2).Value = ".\\results";
        AddComment(sheet.Cell(7, 2), "Optional. Where to save reports. Default: results/ next to executable.");

        HighlightExampleRows(sheet, 2, 7);
        sheet.Columns().AdjustToContents();
    }

    private static void CreateTablesSheet(IXLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("Tables-Sample");
        SetHeaders(sheet, "TableName", "Schema", "RecordIDColumn", "RowMatchColumns", "CustomQuery");

        AddComment(sheet.Cell(1, 1), "Name of the database table");
        AddComment(sheet.Cell(1, 2), "Schema name (default: dbo)");
        AddComment(sheet.Cell(1, 3), "Column for simple WHERE lookup. Leave empty if using CustomQuery.");
        AddComment(sheet.Cell(1, 4), "Comma-separated columns for matching rows in multi-row tables. Leave empty for single-row tables.");
        AddComment(sheet.Cell(1, 5), "Full SQL SELECT with @RecordID placeholder. Overrides RecordIDColumn if both provided.");

        // Example: Simple direct lookup
        sheet.Cell(2, 1).Value = "Order";
        sheet.Cell(2, 2).Value = "dbo";
        sheet.Cell(2, 3).Value = "RecordID";
        AddComment(sheet.Cell(2, 1), "EXAMPLE: Simple direct lookup — 1 row per record");

        // Example: Multi-row with matching
        sheet.Cell(3, 1).Value = "OrderLine";
        sheet.Cell(3, 2).Value = "dbo";
        sheet.Cell(3, 3).Value = "RecordID";
        sheet.Cell(3, 4).Value = "ProductCode,SizeCode";
        AddComment(sheet.Cell(3, 1), "EXAMPLE: Multi-row table — rows matched by ProductCode + SizeCode");

        // Example: Custom query with join
        sheet.Cell(4, 1).Value = "Payment";
        sheet.Cell(4, 2).Value = "dbo";
        sheet.Cell(4, 4).Value = "PaymentType";
        sheet.Cell(4, 5).Value = "SELECT p.* FROM Payment p INNER JOIN OrderPayment op ON p.PaymentID = op.PaymentID WHERE op.RecordID = @RecordID";
        AddComment(sheet.Cell(4, 1), "EXAMPLE: Custom query — records fetched via JOIN, not direct lookup");

        // Example: Filtered query
        sheet.Cell(5, 1).Value = "AuditLog";
        sheet.Cell(5, 2).Value = "dbo";
        sheet.Cell(5, 3).Value = "RecordID";
        sheet.Cell(5, 5).Value = "SELECT * FROM AuditLog WHERE RecordID = @RecordID AND Category = 'Financial'";
        AddComment(sheet.Cell(5, 1), "EXAMPLE: Filtered query — only compare financial audit entries");

        HighlightExampleRows(sheet, 2, 5);
        sheet.Columns().AdjustToContents();
        sheet.Column(5).Width = 80;
    }

    private static void CreateExclusionsSheet(IXLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("Exclusions");
        SetHeaders(sheet, "TableName", "ColumnName", "Reason");

        AddComment(sheet.Cell(1, 1), "Table name, or * for all tables");
        AddComment(sheet.Cell(1, 2), "Column to exclude from comparison");
        AddComment(sheet.Cell(1, 3), "Why this column is excluded (shown in report)");

        sheet.Cell(2, 1).Value = "*";
        sheet.Cell(2, 2).Value = "RecordID";
        sheet.Cell(2, 3).Value = "Different IDs by design — old and new systems generate different IDs";

        sheet.Cell(3, 1).Value = "*";
        sheet.Cell(3, 2).Value = "CreatedDate";
        sheet.Cell(3, 3).Value = "Different timestamps — records created at different times";

        sheet.Cell(4, 1).Value = "*";
        sheet.Cell(4, 2).Value = "ModifiedDate";
        sheet.Cell(4, 3).Value = "Different timestamps";

        sheet.Cell(5, 1).Value = "*";
        sheet.Cell(5, 2).Value = "CreatedBy";
        sheet.Cell(5, 3).Value = "Different user sessions";

        sheet.Cell(6, 1).Value = "OrderLine";
        sheet.Cell(6, 2).Value = "LineItemOID";
        sheet.Cell(6, 3).Value = "Auto-generated surrogate key — different between systems";

        HighlightExampleRows(sheet, 2, 6);
        sheet.Columns().AdjustToContents();
    }

    private static void CreateColumnRulesSheet(IXLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("ColumnRules");
        SetHeaders(sheet, "TableName", "ColumnName", "CompareRule", "Tolerance");

        AddComment(sheet.Cell(1, 1), "Table name, or * for all tables");
        AddComment(sheet.Cell(1, 2), "Column name, or * for all columns in the table");
        AddComment(sheet.Cell(1, 3), "Comparison rule: exact, exact-ci, currency, date, datetime, numeric, percentage, fuzzy, boolean, contains, ignore");
        AddComment(sheet.Cell(1, 4), "Tolerance value (meaning depends on rule). E.g., 0.01 for currency, 0.90 for fuzzy.");

        sheet.Cell(2, 1).Value = "Payment";
        sheet.Cell(2, 2).Value = "TotalAmount";
        sheet.Cell(2, 3).Value = "currency";
        sheet.Cell(2, 4).Value = "0.01";

        sheet.Cell(3, 1).Value = "Order";
        sheet.Cell(3, 2).Value = "OrderDate";
        sheet.Cell(3, 3).Value = "date";

        sheet.Cell(4, 1).Value = "OrderLine";
        sheet.Cell(4, 2).Value = "UnitPrice";
        sheet.Cell(4, 3).Value = "currency";
        sheet.Cell(4, 4).Value = "0.01";

        sheet.Cell(5, 1).Value = "Customer";
        sheet.Cell(5, 2).Value = "FullName";
        sheet.Cell(5, 3).Value = "fuzzy";
        sheet.Cell(5, 4).Value = "0.90";

        sheet.Cell(6, 1).Value = "*";
        sheet.Cell(6, 2).Value = "IsActive";
        sheet.Cell(6, 3).Value = "boolean";

        sheet.Cell(7, 1).Value = "*";
        sheet.Cell(7, 2).Value = "Status";
        sheet.Cell(7, 3).Value = "exact-ci";

        HighlightExampleRows(sheet, 2, 7);
        sheet.Columns().AdjustToContents();
    }

    private static void CreateComparisonsSheet(IXLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("Comparisons");
        SetHeaders(sheet, "Scenario", "OldRecordID", "NewRecordID");

        AddComment(sheet.Cell(1, 1), "Descriptive name for this comparison");
        AddComment(sheet.Cell(1, 2), "Record ID from the old/legacy system");
        AddComment(sheet.Cell(1, 3), "Record ID from the new/modern system");

        sheet.Cell(2, 1).Value = "Basic-Order";
        sheet.Cell(2, 2).Value = "100234";
        sheet.Cell(2, 3).Value = "200891";

        sheet.Cell(3, 1).Value = "Complex-Order";
        sheet.Cell(3, 2).Value = "100235";
        sheet.Cell(3, 3).Value = "200892";

        sheet.Cell(4, 1).Value = "Edge-Case-1";
        sheet.Cell(4, 2).Value = "100236";
        sheet.Cell(4, 3).Value = "200893";

        HighlightExampleRows(sheet, 2, 4);
        sheet.Columns().AdjustToContents();
    }

    private static void CreateInstructionsSheet(IXLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.Add("Instructions");
        sheet.Column(1).Width = 100;

        var row = 1;
        void AddLine(string text, bool bold = false, bool header = false)
        {
            var cell = sheet.Cell(row, 1);
            cell.Value = text;
            if (bold || header) cell.Style.Font.Bold = true;
            if (header) { cell.Style.Font.FontSize = 14; cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#34495e"); cell.Style.Font.FontColor = XLColor.White; }
            row++;
        }

        AddLine("MsSqlRecordsCompare — Configuration Guide", header: true);
        row++;
        AddLine("HOW TO SET UP YOUR FIRST COMPARISON", bold: true);
        row++;
        AddLine("1. Edit the ConnectionConfig sheet with your SQL Server and database name.");
        AddLine("2. Rename 'Tables-Sample' to match your use case (e.g., 'Tables-Orders').");
        AddLine("3. Replace the example table rows with your actual tables.");
        AddLine("4. For each table, set either RecordIDColumn (simple lookup) or CustomQuery (complex).");
        AddLine("5. Add RowMatchColumns for tables with multiple rows per record.");
        AddLine("6. Update the Exclusions sheet — remove examples, add your own.");
        AddLine("7. Update the ColumnRules sheet if you need non-default comparison rules.");
        AddLine("8. Add your record ID pairs to the Comparisons sheet.");
        AddLine("9. Run: MsSqlRecordsCompare.exe --config YourConfig.xlsx");
        row++;
        AddLine("SHEET REFERENCE", bold: true);
        row++;
        AddLine("ConnectionConfig — Database connection settings (key-value pairs)", bold: true);
        AddLine("  ServerName: SQL Server instance (e.g., MYSERVER\\SQLEXPRESS)");
        AddLine("  DatabaseName: Database to connect to");
        AddLine("  CommandTimeout: Query timeout in seconds (default: 120)");
        AddLine("  UserName: SQL Server user name (optional, leave empty for Windows Auth)");
        AddLine("  Password: SQL Server password (optional, used with UserName)");
        AddLine("  ReportOutputPath: Where to save reports (optional)");
        row++;
        AddLine("Tables-* — Which tables to compare and how to fetch records", bold: true);
        AddLine("  TableName: Database table name (required)");
        AddLine("  Schema: Schema name, default 'dbo' (required)");
        AddLine("  RecordIDColumn: Column for WHERE clause lookup (conditional — need this or CustomQuery)");
        AddLine("  RowMatchColumns: Comma-separated columns for matching rows in multi-row tables");
        AddLine("  CustomQuery: Full SQL SELECT with @RecordID placeholder (conditional — need this or RecordIDColumn)");
        AddLine("  You can have multiple Tables-* sheets (e.g., Tables-Orders, Tables-Customers) for different comparisons.");
        row++;
        AddLine("Exclusions — Columns to skip during comparison", bold: true);
        AddLine("  TableName: Table name, or * for all tables");
        AddLine("  ColumnName: Column to exclude");
        AddLine("  Reason: Why excluded (shown in report for audit trail)");
        row++;
        AddLine("ColumnRules — Fine-grained comparison rules per column (optional)", bold: true);
        AddLine("  Rules: exact, exact-ci, currency, date, datetime, numeric, percentage, fuzzy, boolean, contains, ignore");
        AddLine("  Tolerance: Rule-specific threshold (e.g., 0.01 for currency, 0.90 for fuzzy)");
        AddLine("  Default rule is 'exact' for any column not listed.");
        row++;
        AddLine("Comparisons — Record ID pairs to compare", bold: true);
        AddLine("  Scenario: Descriptive name for this comparison");
        AddLine("  OldRecordID: Record ID from the legacy system (string — supports GUIDs, alphanumeric)");
        AddLine("  NewRecordID: Record ID from the modern system");
        AddLine("  Alternative: use --old-id/--new-id CLI flags or --pairs CSV file.");
        row++;
        AddLine("NOTES", bold: true);
        AddLine("  - Example rows in other sheets are highlighted. Replace them with your own data.");
        AddLine("  - The tool uses Windows Integrated Authentication — no credentials needed.");
        AddLine("  - CustomQuery must contain @RecordID and must be a SELECT statement.");
        AddLine("  - Wildcard * in Exclusions/ColumnRules applies to all tables.");
    }

    private static void SetHeaders(IXLWorksheet sheet, params string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#34495e");
            cell.Style.Font.FontColor = XLColor.White;
        }
    }

    private static void HighlightExampleRows(IXLWorksheet sheet, int startRow, int endRow)
    {
        for (int row = startRow; row <= endRow; row++)
        {
            var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 1;
            for (int col = 1; col <= lastCol; col++)
            {
                sheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#fef9e7");
            }
        }
    }

    private static void AddComment(IXLCell cell, string comment)
    {
        cell.GetComment().AddText(comment);
    }
}
