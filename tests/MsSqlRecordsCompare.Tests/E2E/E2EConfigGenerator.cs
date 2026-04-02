using MsSqlRecordsCompare.Tests.Config;

namespace MsSqlRecordsCompare.Tests.E2E;

/// <summary>
/// Generates the E2E test Excel config workbook.
/// Run via: dotnet test --filter "E2EConfigGenerator.GenerateTestConfigWorkbook"
/// Set E2E_CONFIG_PATH env var to control output location.
/// </summary>
public class E2EConfigGenerator
{
    [Fact]
    public void GenerateTestConfigWorkbook()
    {
        // Walk up from bin/Debug/net10.0 → tests/MsSqlRecordsCompare.Tests → tests → repo root → tests/e2e
        var baseDir = AppContext.BaseDirectory;
        var repoRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        var defaultPath = Path.Combine(repoRoot, "tests", "e2e", "TestCompareConfig.xlsx");

        var outputPath = Environment.GetEnvironmentVariable("E2E_CONFIG_PATH");
        if (string.IsNullOrEmpty(outputPath))
            outputPath = defaultPath;

        var builder = new TestWorkbookBuilder()
            .WithConnectionConfig(
                server: "localhost,11433",
                database: "TestCompareDB",
                timeout: 30)
            .WithTablesSheet("Tables-TestOrders",
                ("Customer",  "dbo", "RecordID", null, null),
                ("Order",     "dbo", "RecordID", null, null),
                ("OrderLine", "dbo", "RecordID", "ProductCode,SizeCode", null),
                ("Payment",   "dbo", null, "PaymentType",
                    "SELECT p.* FROM Payment p INNER JOIN OrderPayment op ON p.PaymentID = op.PaymentID WHERE op.RecordID = @RecordID"),
                ("AuditLog",  "dbo", null, "Action",
                    "SELECT * FROM AuditLog WHERE RecordID = @RecordID AND Category = 'Financial'"),
                ("OrderTag",  "dbo", "RecordID", "TagName", null)
            )
            .WithExclusions(
                // Wildcard exclusions
                ("*",         "RecordID",      "Different IDs by design"),
                ("*",         "CreatedDate",   "Different timestamps"),
                ("*",         "ModifiedDate",  "Different timestamps"),
                ("*",         "CreatedBy",     "Different user sessions"),
                // Table-specific exclusions
                ("Customer",  "CustomerID",    "Auto-generated surrogate"),
                ("Order",     "OrderID",       "Auto-generated surrogate"),
                ("Order",     "CustomerRecordID", "Different IDs by design"),
                ("OrderLine", "OrderLineID",   "Auto-generated surrogate"),
                ("Payment",   "PaymentID",     "Auto-generated surrogate"),
                ("AuditLog",  "AuditLogID",    "Auto-generated surrogate"),
                ("AuditLog",  "RecordID",      "Returned by custom query, different IDs"),
                ("AuditLog",  "Category",      "Always Financial due to query filter"),
                ("AuditLog",  "LogTimestamp",  "Different timestamps"),
                ("AuditLog",  "UserName",      "Different user sessions"),
                ("OrderTag",  "OrderTagID",    "Auto-generated surrogate"),
                ("Payment",   "ProcessedBy",   "Different processor sessions")
            )
            .WithColumnRules(
                // Order rules
                ("Order",     "OrderDate",       "date",       null),
                ("Order",     "TotalAmount",     "currency",   "0.01"),
                ("Order",     "SubTotal",        "currency",   "0.01"),
                ("Order",     "TaxAmount",       "currency",   "0.01"),
                ("Order",     "TaxRate",         "percentage", "0.001"),
                ("Order",     "DiscountPercent", "percentage", "0.001"),
                ("Order",     "ProcessedFlag",   "boolean",    null),
                ("Order",     "Notes",           "ignore",     null),
                // OrderLine rules
                ("OrderLine", "UnitPrice",       "currency",   "0.01"),
                ("OrderLine", "LineTotal",       "currency",   "0.01"),
                ("OrderLine", "Description",     "fuzzy",      "0.85"),
                // Payment rules
                ("Payment",   "Amount",          "currency",   "0.01"),
                ("Payment",   "PaymentDate",     "datetime",   null),
                ("Payment",   "ReferenceNumber", "contains",   null),
                ("Payment",   "IsRefund",        "boolean",    null),
                // Customer rules
                ("Customer",  "FullName",        "fuzzy",      "0.90"),
                ("Customer",  "Email",           "exact-ci",   null),
                ("Customer",  "IsActive",        "boolean",    null),
                ("Customer",  "Address",         "contains",   null),
                // AuditLog rules
                ("AuditLog",  "Details",         "fuzzy",      "0.80"),
                // Default
                ("*",         "*",               "exact",      null)
            )
            .WithComparisons(
                ("Perfect-Match",        "OLD-1001", "NEW-1001"),
                ("Known-Mismatches",     "OLD-2001", "NEW-2001"),
                ("Row-Count-Diff",       "OLD-3001", "NEW-3001"),
                ("Null-Handling",        "OLD-4001", "NEW-4001"),
                ("Complex-Queries-Dupes","OLD-5001", "NEW-5001")
            );

        var tempPath = builder.SaveToTempFile();

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.Copy(tempPath, outputPath, overwrite: true);
        File.Delete(tempPath);

        Assert.True(File.Exists(outputPath), $"Config workbook should exist at {outputPath}");
    }
}
