using ClosedXML.Excel;

namespace MsSqlRecordsCompare.Core.Config;

public class ExcelConfigReader
{
    private const string ConnectionSheetName = "ConnectionConfig";
    private const string ExclusionsSheetName = "Exclusions";
    private const string ColumnRulesSheetName = "ColumnRules";
    private const string ComparisonsSheetName = "Comparisons";
    private const string TablesSheetPrefix = "Tables";

    public List<string> DiscoverTableSets(string filePath)
    {
        ValidateFileExists(filePath);

        using var workbook = new XLWorkbook(filePath);
        return GetTableSetNames(workbook);
    }

    public ComparisonConfig Read(string filePath, string tableSetName)
    {
        ValidateFileExists(filePath);

        using var workbook = new XLWorkbook(filePath);

        var availableTableSets = GetTableSetNames(workbook);
        if (availableTableSets.Count == 0)
            throw new ConfigValidationException(
                "No table configuration sheets found. Sheets must use 'Tables-' prefix or be named 'Tables'.");

        var tableSheetName = ResolveTableSheetName(workbook, tableSetName, availableTableSets);
        var connection = ReadConnectionConfig(workbook);
        var tables = ReadTables(workbook, tableSheetName);
        var exclusions = ReadExclusions(workbook);
        var columnRules = ReadColumnRules(workbook);
        var comparisonPairs = ReadComparisonPairs(workbook);

        return new ComparisonConfig
        {
            Connection = connection,
            SelectedTableSet = tableSetName,
            Tables = tables,
            Exclusions = exclusions,
            ColumnRules = columnRules,
            ComparisonPairs = comparisonPairs,
            AvailableTableSets = availableTableSets
        };
    }

    private static void ValidateFileExists(string filePath)
    {
        if (!File.Exists(filePath))
            throw new ConfigValidationException($"Config file not found at '{filePath}'.");
    }

    private static List<string> GetTableSetNames(IXLWorkbook workbook)
    {
        return workbook.Worksheets
            .Select(ws => ws.Name)
            .Where(name =>
                name.Equals(TablesSheetPrefix, StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith(TablesSheetPrefix + "-", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static string ResolveTableSheetName(IXLWorkbook workbook, string tableSetName, List<string> available)
    {
        // Try exact match first
        var exactMatch = available.FirstOrDefault(n =>
            n.Equals(tableSetName, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
            return exactMatch;

        // Try "Tables-{name}" match
        var prefixMatch = available.FirstOrDefault(n =>
            n.Equals($"{TablesSheetPrefix}-{tableSetName}", StringComparison.OrdinalIgnoreCase));
        if (prefixMatch != null)
            return prefixMatch;

        throw new ConfigValidationException(
            $"Table set '{tableSetName}' not found. Available table sets: {string.Join(", ", available)}");
    }

    private static ConnectionConfig ReadConnectionConfig(IXLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Equals(ConnectionSheetName, StringComparison.OrdinalIgnoreCase))
            ?? throw new ConfigValidationException(
                $"Required sheet '{ConnectionSheetName}' not found in the workbook.");

        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            var key = sheet.Cell(row, 1).GetString().Trim();
            var value = sheet.Cell(row, 2).GetString().Trim();
            if (!string.IsNullOrEmpty(key))
                settings[key] = value;
        }

        return new ConnectionConfig
        {
            ServerName = settings.GetValueOrDefault("ServerName")
                ?? throw new ConfigValidationException("ServerName not found in ConnectionConfig sheet."),
            DatabaseName = settings.GetValueOrDefault("DatabaseName")
                ?? throw new ConfigValidationException("DatabaseName not found in ConnectionConfig sheet."),
            UserName = settings.GetValueOrDefault("UserName"),
            Password = settings.GetValueOrDefault("Password"),
            CommandTimeout = int.TryParse(settings.GetValueOrDefault("CommandTimeout"), out var timeout)
                ? timeout : 120,
            ReportOutputPath = settings.GetValueOrDefault("ReportOutputPath")
        };
    }

    private static List<TableConfig> ReadTables(IXLWorkbook workbook, string sheetName)
    {
        var sheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase))
            ?? throw new ConfigValidationException($"Sheet '{sheetName}' not found in the workbook.");

        var headers = ReadHeaders(sheet);
        RequireColumn(headers, "TableName", sheetName);

        var tables = new List<TableConfig>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            var tableName = GetCellValue(sheet, row, headers, "TableName");
            if (string.IsNullOrWhiteSpace(tableName))
                continue;

            var rowMatchColumnsRaw = GetCellValue(sheet, row, headers, "RowMatchColumns");
            var rowMatchColumns = string.IsNullOrWhiteSpace(rowMatchColumnsRaw)
                ? new List<string>()
                : rowMatchColumnsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

            tables.Add(new TableConfig
            {
                TableName = tableName,
                Schema = GetCellValue(sheet, row, headers, "Schema") ?? "dbo",
                RecordIdColumn = GetCellValue(sheet, row, headers, "RecordIDColumn"),
                RowMatchColumns = rowMatchColumns,
                CustomQuery = GetCellValue(sheet, row, headers, "CustomQuery")
            });
        }

        return tables;
    }

    private static List<ExclusionConfig> ReadExclusions(IXLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Equals(ExclusionsSheetName, StringComparison.OrdinalIgnoreCase));

        if (sheet == null)
            return [];

        var headers = ReadHeaders(sheet);
        var exclusions = new List<ExclusionConfig>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            var tableName = GetCellValue(sheet, row, headers, "TableName");
            var columnName = GetCellValue(sheet, row, headers, "ColumnName");
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName))
                continue;

            exclusions.Add(new ExclusionConfig
            {
                TableName = tableName,
                ColumnName = columnName,
                Reason = GetCellValue(sheet, row, headers, "Reason")
            });
        }

        return exclusions;
    }

    private static List<ColumnRuleConfig> ReadColumnRules(IXLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Equals(ColumnRulesSheetName, StringComparison.OrdinalIgnoreCase));

        if (sheet == null)
            return [];

        var headers = ReadHeaders(sheet);
        var rules = new List<ColumnRuleConfig>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            var tableName = GetCellValue(sheet, row, headers, "TableName");
            var columnName = GetCellValue(sheet, row, headers, "ColumnName");
            var compareRule = GetCellValue(sheet, row, headers, "CompareRule");
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName) ||
                string.IsNullOrWhiteSpace(compareRule))
                continue;

            rules.Add(new ColumnRuleConfig
            {
                TableName = tableName,
                ColumnName = columnName,
                CompareRule = compareRule.ToLowerInvariant(),
                Tolerance = GetCellValue(sheet, row, headers, "Tolerance")
            });
        }

        return rules;
    }

    private static List<ComparisonPair> ReadComparisonPairs(IXLWorkbook workbook)
    {
        var sheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Equals(ComparisonsSheetName, StringComparison.OrdinalIgnoreCase));

        if (sheet == null)
            return [];

        var headers = ReadHeaders(sheet);
        var pairs = new List<ComparisonPair>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            var scenario = GetCellValue(sheet, row, headers, "Scenario");
            var oldId = GetCellValue(sheet, row, headers, "OldRecordID");
            var newId = GetCellValue(sheet, row, headers, "NewRecordID");
            if (string.IsNullOrWhiteSpace(scenario) || string.IsNullOrWhiteSpace(oldId) ||
                string.IsNullOrWhiteSpace(newId))
                continue;

            pairs.Add(new ComparisonPair
            {
                Scenario = scenario,
                OldRecordId = oldId,
                NewRecordId = newId
            });
        }

        return pairs;
    }

    private static Dictionary<string, int> ReadHeaders(IXLWorksheet sheet)
    {
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 1;

        for (int col = 1; col <= lastCol; col++)
        {
            var header = sheet.Cell(1, col).GetString().Trim();
            if (!string.IsNullOrEmpty(header))
                headers[header] = col;
        }

        return headers;
    }

    private static string? GetCellValue(IXLWorksheet sheet, int row,
        Dictionary<string, int> headers, string columnName)
    {
        if (!headers.TryGetValue(columnName, out var col))
            return null;

        var value = sheet.Cell(row, col).GetString().Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static void RequireColumn(Dictionary<string, int> headers, string columnName, string sheetName)
    {
        if (!headers.ContainsKey(columnName))
            throw new ConfigValidationException(
                $"Required column '{columnName}' not found in sheet '{sheetName}'.");
    }
}
