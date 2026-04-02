# CLAUDE.md — Database Record Comparison Tool (QuoteCompare)

## Project Overview

This is a general-purpose database record comparison tool for validating data parity between two systems that write to the same Microsoft SQL Server database. When both a legacy application and a modernized application perform the same operation (e.g., creating a record), both write to multiple shared database tables. This tool compares the resulting records field-by-field to verify the modern system produces equivalent data output.

**Our goal:** Build a .NET 8.0 solution with a **Core class library** and a **CLI console app** that takes pairs of record IDs (one created by the old system, one by the new system for the same scenario), reads their data from configurable database tables, and produces a detailed comparison report highlighting any mismatches. The architecture supports adding a **WPF desktop UI** in a future release (v1.5).

**Key constraints:**
- The database uses **Windows Integrated Authentication** (no username/password). The tool must run as the user's Windows process and inherit their credentials.
- **Record IDs are different** between old and new systems. The user provides pairs of `(OldRecordID, NewRecordID)` — the tool looks up records for each and compares them. Record IDs are treated as **strings** (supports integers, GUIDs, alphanumeric codes).
- Some tables have **multiple rows per record** (parent-child relationships). The tool must match rows using configurable business key columns before comparing.
- The tool must be **fully configurable** — no code changes when tables, columns, or exclusion rules change. All configuration is driven by an Excel workbook.
- **Data accuracy is critical.** This tool is used to validate production-equivalent behavior during system modernization.

**Technology stack:**
- .NET 8.0 (C#)
- `QuoteCompare.Core` — class library (comparison engine, config, database, reporting)
- `QuoteCompare.CLI` — console app (CLI parsing, interactive prompts, console output)
- Future: `QuoteCompare.UI` — WPF desktop app (v1.5)
- Microsoft.Data.SqlClient with `Integrated Security=True` (Windows Auth)
- ClosedXML for reading/writing Excel configuration and reports
- Self-contained HTML report output with JavaScript filtering

**Release strategy:**
- **v1.0:** Core engine + CLI (with guided setup wizard). Fully functional comparison tool.
- **v1.5:** WPF desktop UI wrapping the same Core library. Visual config editor, schema browser, embedded report viewer.

---

## Architecture

```
┌───────────────────────────────────────────────────────────────┐
│                    Excel Configuration Workbook                │
│                    (ComparisonConfig.xlsx)                     │
│                                                                │
│  Sheet: "ConnectionConfig"                                     │
│  ┌──────────────────┬─────────────────────────────────────┐   │
│  │ Setting          │ Value                               │   │
│  │ ServerName       │ YOUR_SERVER\INSTANCE                │   │
│  │ DatabaseName     │ YourDatabase                        │   │
│  │ CommandTimeout   │ 120                                 │   │
│  │ ReportOutputPath │ .\results                           │   │
│  └──────────────────┴─────────────────────────────────────┘   │
│                                                                │
│  Sheet: "Tables-Orders" (multiple Tables-* sheets supported)   │
│  ┌──────────────┬────────┬────────────────┬─────────────────┬──────────────────────────────────────────────────────────┐
│  │ TableName    │ Schema │ RecordIDColumn │ RowMatchColumns │ CustomQuery                                             │
│  │ Order        │ dbo    │ RecordID       │                 │                                                          │
│  │ OrderLine    │ dbo    │ RecordID       │ ProductCode,Qty │                                                          │
│  │ Payment      │ dbo    │                │ PaymentType     │ SELECT p.* FROM Payment p INNER JOIN OrderPayment op     │
│  │              │        │                │                 │   ON p.PaymentID = op.PaymentID WHERE op.RecordID = @RecordID │
│  │ AuditLog     │ dbo    │ RecordID       │                 │ SELECT * FROM AuditLog WHERE RecordID = @RecordID       │
│  │              │        │                │                 │   AND Category = 'Financial'                             │
│  └──────────────┴────────┴────────────────┴─────────────────┴──────────────────────────────────────────────────────────┘
│                                                                │
│  Sheet: "Exclusions"                                           │
│  ┌──────────────┬──────────────┬────────────────────────────┐ │
│  │ TableName    │ ColumnName   │ Reason                     │ │
│  │ *            │ RecordID     │ Different IDs by design    │ │
│  │ *            │ CreatedDate  │ Different timestamps       │ │
│  │ *            │ ModifiedDate │ Different timestamps       │ │
│  │ *            │ CreatedBy    │ Different user sessions    │ │
│  │ OrderLine    │ LineItemOID  │ Auto-generated surrogate   │ │
│  └──────────────┴──────────────┴────────────────────────────┘ │
│                                                                │
│  Sheet: "Comparisons"                                          │
│  ┌──────────────────┬──────────────┬──────────────┐           │
│  │ Scenario         │ OldRecordID  │ NewRecordID  │           │
│  │ Basic-Order      │ 100234       │ 200891       │           │
│  │ Complex-Order    │ ABC-123      │ XYZ-789      │           │
│  │ Edge-Case-1      │ 100236       │ 200893       │           │
│  │ ... (bulk)       │              │              │           │
│  └──────────────────┴──────────────┴──────────────┘           │
│                                                                │
│  Sheet: "ColumnRules" (optional — for column-specific rules)   │
│  ┌──────────────┬──────────────┬──────────────┬─────────────┐ │
│  │ TableName    │ ColumnName   │ CompareRule  │ Tolerance   │ │
│  │ Payment      │ TotalAmount  │ currency     │ 0.01        │ │
│  │ Order        │ OrderDate    │ date         │ 0           │ │
│  │ OrderLine    │ UnitPrice    │ currency     │ 0.01        │ │
│  │ Customer     │ FullName     │ fuzzy        │ 0.90        │ │
│  │ *            │ *            │ exact        │             │ │
│  └──────────────┴──────────────┴──────────────┴─────────────┘ │
│  (Default rule is "exact" for any column not listed here)      │
│  (* wildcard matches all tables/columns)                       │
│                                                                │
│  Sheet: "Instructions" (template only — documentation)         │
│  Full documentation of every column, valid values, usage       │
│  patterns, and step-by-step setup guide.                       │
└───────────────────────┬───────────────────────────────────────┘
                        │ reads
                        ▼
┌───────────────────────────────────────────────────────────────┐
│              QuoteCompare.Core (class library)                 │
│                                                                │
│  1. Load Excel config (ClosedXML)                              │
│  2. Validate config (check tables exist, columns exist in DB)  │
│  3. Connect to SQL Server (Windows Integrated Auth)            │
│  4. For each comparison pair (OldRecordID, NewRecordID):       │
│     a. For each configured table:                              │
│        - If RecordIDColumn set (no CustomQuery):               │
│            SELECT * FROM [Schema].[Table]                      │
│              WHERE [RecordIDColumn] = @RecordID                │
│        - If CustomQuery set:                                   │
│            Execute custom SQL with @RecordID parameter         │
│        - Remove excluded columns                               │
│        - If RowMatchColumns defined:                           │
│            Match rows by business key values                   │
│            Report unmatched rows from either side              │
│        - For each matched row pair:                            │
│            Compare column by column                            │
│            Apply column-specific rules (currency, date, etc.)  │
│            Record mismatches: Table + Column + OldVal + NewVal │
│     b. Record scenario-level summary                           │
│  5. Generate HTML comparison report                            │
│  6. Generate Excel summary report (optional)                   │
│  7. Return results (Core exposes IProgress<T> for UI updates)  │
│                                                                │
│  QuoteCompare.CLI (console app)                                │
│  - CLI argument parsing (System.CommandLine)                   │
│  - Interactive table set selection prompt                       │
│  - Guided setup wizard (--setup)                               │
│  - Colored console output and progress display                 │
└───────────────────────┬───────────────────────────────────────┘
                        │ outputs
                        ▼
┌───────────────────────────────────────────────────────────────┐
│  Outputs (saved to results/ folder next to executable)         │
│                                                                │
│  results/                                                      │
│    2026-03-31_142205/                                          │
│      Report.html         ← self-contained, print-friendly     │
│      Summary.xlsx        ← one row per mismatch (optional)    │
│    2026-03-31_153010/                                          │
│      Report.html                                               │
│      Summary.xlsx                                              │
│                                                                │
│  1. HTML Report (self-contained, opens in any browser)         │
│     - Executive summary: X of Y scenarios passed               │
│     - Scenario overview table (bulk runs)                      │
│     - Mismatch summary by table (bulk runs)                    │
│     - Per-scenario breakdown with drill-down                   │
│     - Per-table mismatch detail                                │
│     - Drill-down: Table → Row → Column → OldValue vs NewValue │
│     - Filter by: table, scenario, mismatch-only                │
│     - Excluded columns listed with reasons (audit trail)       │
│     - Unmatched rows highlighted                               │
│     - Export to CSV button                                     │
│     - "Save as PDF" button (triggers browser print dialog)     │
│     - Print-friendly stylesheet (@media print)                 │
│                                                                │
│  2. Console Summary                                            │
│     - Quick pass/fail per scenario                             │
│     - Total mismatches count                                   │
│     - Path to full report                                      │
│                                                                │
│  3. Excel Summary (optional)                                   │
│     - One row per mismatch for import into defect tracker      │
└───────────────────────────────────────────────────────────────┘
```

---

## Project Structure

```
QuoteCompare/
├── QuoteCompare.sln
├── CLAUDE.md                              # this file (Claude Code instructions)
├── README.md                              # public-facing project documentation
├── LICENSE                                # project license
│
├── src/
│   ├── QuoteCompare.Core/                 # class library — NO console/UI dependencies
│   │   ├── QuoteCompare.Core.csproj
│   │   │
│   │   ├── Config/                        # configuration loading
│   │   │   ├── ExcelConfigReader.cs       # reads ComparisonConfig.xlsx
│   │   │   ├── ComparisonConfig.cs        # strongly-typed config model
│   │   │   ├── TableConfig.cs             # per-table configuration (incl. CustomQuery)
│   │   │   ├── ExclusionConfig.cs         # exclusion rules
│   │   │   ├── ColumnRuleConfig.cs        # column-specific comparison rules
│   │   │   └── ConfigValidator.cs         # validates config against actual DB schema
│   │   │
│   │   ├── Database/                      # database access
│   │   │   ├── SqlConnectionFactory.cs    # creates SqlConnection with Windows Auth
│   │   │   ├── RecordDataReader.cs        # reads records (auto-generated or custom query)
│   │   │   ├── SchemaInspector.cs         # validates table/column existence
│   │   │   └── Models/
│   │   │       └── TableRecord.cs         # generic row representation (Dictionary<string, object?>)
│   │   │
│   │   ├── Comparison/                    # comparison logic
│   │   │   ├── ComparisonEngine.cs        # main orchestrator
│   │   │   ├── RowMatcher.cs              # matches rows using RowMatchColumns
│   │   │   ├── ColumnComparer.cs          # compares individual column values
│   │   │   ├── Normalizers/               # value normalization
│   │   │   │   ├── INormalizer.cs         # normalizer interface
│   │   │   │   ├── CurrencyNormalizer.cs  # $1,234.56 → 1234.56
│   │   │   │   ├── DateNormalizer.cs      # various date formats → canonical
│   │   │   │   ├── DateTimeNormalizer.cs  # datetime with optional tolerance
│   │   │   │   ├── NumericNormalizer.cs   # decimal comparison with tolerance
│   │   │   │   ├── PercentageNormalizer.cs# 0.15 vs 15% normalization
│   │   │   │   ├── BooleanNormalizer.cs   # true/false/1/0/yes/no/Y/N
│   │   │   │   ├── FuzzyNormalizer.cs     # Levenshtein-based comparison
│   │   │   │   ├── ExactNormalizer.cs     # default: trim and exact match
│   │   │   │   ├── ContainsNormalizer.cs  # new value contains old value
│   │   │   │   └── NormalizerFactory.cs   # resolves CompareRule → INormalizer
│   │   │   └── Models/
│   │   │       ├── ComparisonResult.cs    # full result model
│   │   │       ├── ScenarioResult.cs      # per-scenario result
│   │   │       ├── TableResult.cs         # per-table result
│   │   │       ├── RowMatchResult.cs      # per-row-pair result
│   │   │       └── ColumnMismatch.cs      # individual mismatch detail
│   │   │
│   │   └── Reporting/                     # report generation
│   │       ├── HtmlReportGenerator.cs     # self-contained HTML report
│   │       ├── ExcelReportGenerator.cs    # Excel mismatch export
│   │       └── Templates/
│   │           └── ReportTemplate.html    # embedded HTML template with JS
│   │
│   └── QuoteCompare.CLI/                  # console app — thin wrapper over Core
│       ├── QuoteCompare.CLI.csproj
│       ├── Program.cs                     # entry point, System.CommandLine
│       ├── InteractivePrompts.cs          # table set selection, setup wizard prompts
│       └── ConsoleReportWriter.cs         # colored console output, progress
│
├── tests/
│   └── QuoteCompare.Tests/
│       ├── QuoteCompare.Tests.csproj
│       ├── Config/
│       │   ├── ExcelConfigReaderTests.cs
│       │   └── ConfigValidatorTests.cs
│       ├── Comparison/
│       │   ├── ComparisonEngineTests.cs
│       │   ├── RowMatcherTests.cs
│       │   ├── ColumnComparerTests.cs
│       │   └── Normalizers/
│       │       ├── CurrencyNormalizerTests.cs
│       │       ├── DateNormalizerTests.cs
│       │       ├── BooleanNormalizerTests.cs
│       │       └── FuzzyNormalizerTests.cs
│       └── TestData/
│           └── SampleConfig.xlsx          # test config workbook
│
├── config/
│   ├── ComparisonConfig.xlsx              # THE configuration workbook (user edits this)
│   ├── ComparisonConfig-Template.xlsx     # template with examples + Instructions sheet
│   └── README.md                          # how to fill out the config workbook
│
├── results/                               # output directory (next to executable)
│   └── .gitkeep
│
└── scripts/
    ├── run.bat                            # quick-launch: dotnet run -- --config .\config\ComparisonConfig.xlsx
    ├── run-single.bat                     # single scenario: dotnet run -- --config ... --scenario "Basic-Order"
    └── publish.bat                        # dotnet publish self-contained
```

---

## Core Design Principles

### 1. Zero Code Changes for Configuration Updates

The user should NEVER need to modify C# code for any of these changes:
- Adding or removing a table from comparison
- Adding or removing column exclusions
- Changing which columns are used for row matching
- Adding new comparison pairs (scenarios)
- Changing the database server or database name
- Adding column-specific comparison rules
- Changing how records are fetched (simple lookup vs custom query)
- Switching between different table sets

ALL of this is driven by the Excel configuration workbook.

### 2. Core + CLI Separation

The solution is split into two projects:

- **`QuoteCompare.Core`** (class library) — All business logic: config loading, database access, comparison engine, report generation. **Must NEVER reference `System.Console` or any CLI/UI framework.** Exposes `IProgress<T>` or event-based callbacks for progress reporting.
- **`QuoteCompare.CLI`** (console app) — Thin wrapper: CLI arg parsing, interactive prompts (table set selection), console output, progress display. References `QuoteCompare.Core`.
- **Future: `QuoteCompare.UI`** (WPF) — Will reference `QuoteCompare.Core` for the same engine.

This separation allows adding a WPF desktop UI (v1.5) without refactoring the core engine.

### 3. Hybrid Query Configuration

Each table supports two modes of record fetching:

| Column | Required? | Purpose |
|---|---|---|
| `RecordIDColumn` | Conditional | Column name for simple `WHERE col = @RecordID` lookups |
| `CustomQuery` | Conditional | Full SQL SELECT with `@RecordID` placeholder for complex cases |

**Rules:**
- If `RecordIDColumn` is provided and `CustomQuery` is empty → auto-generate `SELECT * FROM [Schema].[Table] WHERE [RecordIDColumn] = @RecordID`
- If `CustomQuery` is provided → use it (overrides `RecordIDColumn` if both present)
- If both are empty → validation error
- The same query runs twice: once with old ID, once with new ID

**CustomQuery validation:**
- Must contain `@RecordID` placeholder
- Must be a SELECT statement (block INSERT/UPDATE/DELETE/DROP/ALTER/TRUNCATE)
- Column names for exclusions/rules are inferred from the SqlDataReader result set (no extra config needed)

**Use cases:**
- **Simple direct lookup:** `RecordIDColumn = "RecordID"` → `SELECT * FROM dbo.Order WHERE RecordID = @RecordID`
- **Join / indirect relationship:** `CustomQuery = "SELECT p.* FROM Payment p INNER JOIN OrderPayment op ON p.PaymentID = op.PaymentID WHERE op.RecordID = @RecordID"`
- **Filtered subset:** `CustomQuery = "SELECT * FROM AuditLog WHERE RecordID = @RecordID AND Category = 'Financial'"`

### 4. Multiple Table Sets

Users can define multiple table configurations as separate sheets using a `Tables-*` naming convention:

- Sheets must use `Tables-` prefix: `Tables-Orders`, `Tables-Customers`, `Tables-Inventory`
- Plain `Tables` sheet also recognized (backward compatible)
- Each table set sheet has the same column structure

**Selection:**
1. **CLI flag:** `--table-set "Orders"` → matches sheet `Tables-Orders`
2. **Interactive prompt:** If `--table-set` not provided, **always** show a numbered menu for the user to select — even if only one sheet exists

```
Found 3 table configuration sheets:
  [1] Tables-Orders        (10 tables)
  [2] Tables-Customers     (8 tables)
  [3] Tables-Inventory     (5 tables)

Select a table set [1-3]: _
```

If `--table-set` value doesn't match any sheet → error listing available sheets.

### 5. Wildcard Exclusions

The exclusion sheet supports `*` as a wildcard for TableName:
- `* | CreatedDate | Different timestamps` → excludes CreatedDate from ALL tables
- `* | ModifiedDate | Different timestamps` → excludes ModifiedDate from ALL tables
- `* | RecordID | Different IDs by design` → excludes the record ID column itself from comparison
- `OrderLine | LineItemOID | Auto-generated` → excludes LineItemOID from OrderLine table only

Exclusion resolution: table-specific exclusions are additive with wildcard exclusions. A column is excluded if it matches either a table-specific rule or a wildcard rule.

### 6. Row Matching Strategy

For tables with multiple rows per record:

```
RowMatchColumns = "ProductCode,SizeCode"

Old Record 12345 rows in OrderLine table:
  Row A: ProductCode=SKU-100, SizeCode=LG, Qty=5, Price=29.99
  Row B: ProductCode=SKU-200, SizeCode=MD, Qty=2, Price=49.99
  Row C: ProductCode=SKU-300, SizeCode=SM, Qty=1, Price=19.99

New Record 67890 rows in OrderLine table:
  Row X: ProductCode=SKU-300, SizeCode=SM, Qty=1, Price=19.99
  Row Y: ProductCode=SKU-100, SizeCode=LG, Qty=5, Price=29.99
  Row Z: ProductCode=SKU-200, SizeCode=MD, Qty=2, Price=49.99

Matching result:
  Row A ↔ Row Y (matched on ProductCode=SKU-100, SizeCode=LG)
  Row B ↔ Row Z (matched on ProductCode=SKU-200, SizeCode=MD)
  Row C ↔ Row X (matched on ProductCode=SKU-300, SizeCode=SM)

Now compare column-by-column within each matched pair.
```

**Note:** RowMatchColumns works with both auto-generated queries and CustomQuery tables. The matching happens on the result set, not the query.

**Edge cases to handle:**
- **Unmatched old rows:** Rows in old record with no corresponding row in new record → report as "Missing in New System"
- **Unmatched new rows:** Rows in new record with no corresponding row in old record → report as "Extra in New System"
- **Duplicate matches:** Multiple rows in one side matching the same business key → report as "Duplicate Key" warning and compare first match, flag the rest
- **Empty RowMatchColumns:** Table has exactly one row per record (like a main/header table) → compare directly, error if either side has 0 or more than 1 row

### 7. Column Comparison Rules

The ColumnRules sheet allows fine-grained control over how values are compared:

| Rule         | Behavior                                                                                                   |
| ------------ | ---------------------------------------------------------------------------------------------------------- |
| `exact`      | Default. Trim whitespace, exact string match. Nulls match nulls.                                           |
| `exact-ci`   | Case-insensitive exact match                                                                               |
| `currency`   | Parse both as decimal, compare with configurable tolerance (default 0.01)                                  |
| `date`       | Parse both as dates, compare date-only (ignore time component unless specified)                            |
| `datetime`   | Parse both as datetime, compare with configurable tolerance (default 0 seconds)                            |
| `numeric`    | Parse both as decimal, compare with configurable tolerance                                                 |
| `percentage` | Normalize (strip %, handle 0.15 vs 15), compare as decimal                                                 |
| `fuzzy`      | Levenshtein similarity, configurable threshold (default 0.90)                                              |
| `boolean`    | Normalize (true/false/1/0/yes/no/Y/N/on/off), compare                                                      |
| `contains`   | New value contains old value (for fields where the modern system adds prefixes/suffixes)                   |
| `ignore`     | Skip comparison for this column (different from exclusion — column appears in report but marked "ignored") |

**Rule resolution order:**
1. Specific table + column match in ColumnRules sheet
2. Wildcard table (`*`) + specific column match
3. Specific table + wildcard column (`*`) match
4. Default: `exact`

### 8. Windows Integrated Authentication

The SQL connection string uses:
```csharp
var connectionString = $"Server={serverName};Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Command Timeout={timeout}";
```

- `Integrated Security=True` — uses the current Windows user's Kerberos/NTLM token
- `TrustServerCertificate=True` — avoids SSL certificate validation issues in enterprise environments
- No username or password anywhere in the config or code
- Server and database can be overridden via `--server` and `--database` CLI flags

The connection factory should:
1. Test the connection on startup and fail fast with a clear error message if it can't connect
2. Log the connected user identity for audit trail (`SELECT SUSER_SNAME()`)
3. Verify the user has SELECT permission on all configured tables before starting comparison

### 9. Record ID Input Methods

Three ways to provide record ID pairs:

| Method | CLI | Use case |
|---|---|---|
| Inline | `--old-id 100234 --new-id 200891 [--scenario "name"]` | Quick one-off comparison |
| CSV file | `--pairs records.csv` | Bulk pairs from export/query |
| Excel sheet | (default) Comparisons sheet in the config workbook | Curated set maintained alongside config |

**Priority order:** Inline flags > `--pairs` CSV > Comparisons sheet. Higher priority method overrides lower ones.

**Record ID type:** Strings (not restricted to numeric). Supports integers, GUIDs, alphanumeric codes — whatever the database uses.

**CSV format:**
```csv
Scenario,OldRecordID,NewRecordID
Basic-Order,100234,200891
Complex-Order,ABC-123,XYZ-789
```

### 10. Self-Contained HTML Report

The HTML report must be a single file with no external dependencies:
- All CSS inline
- All JavaScript inline
- No CDN references
- Opens in any browser (Chrome, Edge)
- Includes filtering: show all / mismatches only / by table / by scenario
- Includes a "Copy to CSV" button for each section
- Includes a "Save as PDF" button (triggers browser print dialog)
- Color-coded: green for match, red for mismatch, yellow for fuzzy match, gray for excluded/ignored
- Collapsible sections per scenario and per table (mismatched tables auto-expand, passed tables collapsed)
- For bulk runs: scenario overview table and mismatch summary by table at the top

**Print/PDF design requirements (design print-first, not as afterthought):**
- `@media print` stylesheet with proper page margins
- Page breaks between scenarios (`page-break-before`)
- Report header (metadata, server, user, date) repeats on each page
- All collapsible sections expand automatically for print
- Interactive elements hidden in print (filters, buttons, "Save as PDF")
- Color coding works in grayscale: always include icons (✓/✗/~) alongside color
- Long values (GUIDs, URLs) wrap cleanly — `word-break: break-all` on value cells
- Table rows don't split across pages (`page-break-inside: avoid`)
- Font sizes readable when printed (min 10pt for table data)
- No horizontal overflow — tables fit within print margins

### 11. CLI Color Scheme

All CLI output uses a consistent color scheme for readability:

| Color | Usage |
|---|---|
| **Cyan (bold)** | Headers, banners, app title, section dividers |
| **Green** | ✓ Pass, success, connected, matched |
| **Red** | ✗ Fail, mismatches, errors, disconnected |
| **Yellow** | ⚠ Warnings, input prompts, user input cues |
| **Gray (dim)** | Progress messages, status updates, informational |
| **White** | Table data, normal text, menu options |
| **Cyan (underline)** | File paths, clickable report links |
| **Magenta** | Counts, numbers, statistics highlights |

**Interactive prompt example:**
```
  QuoteCompare v1.0                              [Cyan bold]

  Found 3 table configuration sheets:
    [1] Tables-Orders        (10 tables)          [White]
    [2] Tables-Customers     (8 tables)           [White]
    [3] Tables-Inventory     (5 tables)           [White]

  Select a table set [1-3]: _                     [Yellow]

  Connecting to SQLPROD01\MAIN...                 [Gray]
  ✓ Connected as DOMAIN\vimal                     [Green]
  Validating tables...                            [Gray]
  ✓ All 10 tables verified                       [Green]
```

**Console report results example:**
```
  ═══════════════════════════════════════════════  [Cyan]
    COMPARISON RESULTS                             [Cyan bold]
  ═══════════════════════════════════════════════  [Cyan]

  Scenario: Basic-Order (100234 → 200891)
    dbo.Order            ✗ 2 mismatches            [Red]
    dbo.OrderLine        ✓ match                   [Green]
    dbo.Payment          ✗ 1 mismatch              [Red]
    dbo.Address          ✓ match                   [Green]

  Scenario: Express-Shipping (100235 → 200892)
    ✓ All tables match                             [Green]

  ───────────────────────────────────────────────  [Cyan]
    SUMMARY                                        [Cyan bold]
  ───────────────────────────────────────────────  [Cyan]
  Passed:     8 of 12 scenarios                    [Green/Red]
  Mismatches: 11 total across 4 scenarios          [Magenta + Red]
  Report:     results\2026-03-31_142205\Report.html [Cyan underline]
  ═══════════════════════════════════════════════  [Cyan]
```

### 12. Results Output Location

- All output goes into a `results` folder **next to the executable** (not the config file, not the working directory)
- Each run creates a **timestamped subfolder**: `results/2026-03-31_142205/`
- Preserves full run history — users can compare across runs
- Overridable via `--output` CLI flag or `ReportOutputPath` in ConnectionConfig sheet
- **Priority order:** `--output` CLI flag > ConnectionConfig `ReportOutputPath` > default (exe-relative `results/`)

### 13. Config Template Generation

`--generate-template [path]` creates a fully documented config workbook:

**Data sheets with example rows:**
- `ConnectionConfig` — placeholder values (YOUR_SERVER, YourDatabase, etc.)
- `Tables-Sample` — example rows showing all patterns (simple lookup, multi-row matching, custom query/join, filtered query)
- `Exclusions` — common starter exclusions (CreatedDate, ModifiedDate, CreatedBy, RecordID with reasons)
- `ColumnRules` — one example row per rule type (exact, currency, date, numeric, fuzzy, boolean, percentage, contains, ignore)
- `Comparisons` — sample scenario rows (Basic-Order, Complex-Order, Edge-Case-1)

**Instructions sheet:**
- Dedicated sheet with full documentation of every column across all sheets
- Valid values, required vs optional, wildcards, usage patterns
- Step-by-step guide: "How to set up your first comparison"

**Formatting:**
- Example rows visually distinct (cell comments or colored background) so users know to replace them
- Column headers with cell comments summarizing purpose

### 14. Guided Setup Wizard

`--setup` CLI flag walks first-time users through creating their config workbook interactively:

1. **Database connection** — ask for server/database, test connection, confirm identity
2. **Table selection** — read DB schema, present available tables for selection
3. **Per-table config** — for each selected table: RecordIDColumn or CustomQuery, RowMatchColumns
4. **Exclusions** — suggest common exclusions (CreatedDate, ModifiedDate, etc.), allow custom additions
5. **Save** — generate the filled config workbook, instruct user to add record ID pairs

This lives in the CLI project, not Core. Core provides the schema inspection APIs; CLI handles the interactive prompts.

---

## Command-Line Interface

```bash
# Run comparison (interactive table set selection)
QuoteCompare.exe --config .\config\ComparisonConfig.xlsx

# Specify table set directly (skip interactive prompt)
QuoteCompare.exe --config .\config\ComparisonConfig.xlsx --table-set "Orders"

# Quick one-off comparison (inline record IDs)
QuoteCompare.exe --config .\config\ComparisonConfig.xlsx --old-id 100234 --new-id 200891

# Quick one-off with scenario name
QuoteCompare.exe --config .\config\ComparisonConfig.xlsx --old-id 100234 --new-id 200891 --scenario "Quick-Test"

# Run a single scenario by name from the Comparisons sheet
QuoteCompare.exe --config .\config\ComparisonConfig.xlsx --scenario "Basic-Order"

# Run with bulk pairs from CSV file
QuoteCompare.exe --config .\config\ComparisonConfig.xlsx --pairs .\data\record-pairs.csv

# Override database connection (same config, different environment)
QuoteCompare.exe --config .\config\ComparisonConfig.xlsx --server "STAGING\SQL" --database "OrdersDB_QA"

# Run with a specific output directory
QuoteCompare.exe --config .\config\ComparisonConfig.xlsx --output .\my-results

# Run with verbose logging
QuoteCompare.exe --config .\config\ComparisonConfig.xlsx --verbose

# Validate config only (check DB connectivity, table/column existence)
QuoteCompare.exe --config .\config\ComparisonConfig.xlsx --validate-only

# Generate a blank config template
QuoteCompare.exe --generate-template .\config\NewConfig.xlsx

# Guided setup wizard (interactive config creation)
QuoteCompare.exe --setup

# Using dotnet run during development
dotnet run --project src/QuoteCompare.CLI -- --config .\config\ComparisonConfig.xlsx
```

**CLI flags summary:**

| Flag | Description |
|---|---|
| `--config <path>` | Path to the Excel configuration workbook (required for most commands) |
| `--table-set <name>` | Select a specific table set sheet (e.g., "Orders" matches "Tables-Orders") |
| `--old-id <id>` | Old system record ID for inline one-off comparison |
| `--new-id <id>` | New system record ID for inline one-off comparison |
| `--scenario <name>` | Filter to a single scenario by name, or name the inline comparison |
| `--pairs <path>` | CSV file with record ID pairs (overrides Comparisons sheet) |
| `--server <server>` | Override SQL Server instance from config |
| `--database <db>` | Override database name from config |
| `--output <path>` | Override results output directory |
| `--verbose` | Enable detailed logging |
| `--validate-only` | Check config and DB connectivity without running comparison |
| `--generate-template <path>` | Generate a blank config template workbook |
| `--setup` | Launch the guided setup wizard |

---

## NuGet Dependencies

All package versions are centrally managed in `Directory.Packages.props` with exact pinned versions (no wildcards).

| Package | Project | Purpose |
|---|---|---|
| Microsoft.Data.SqlClient | Core | SQL Server with Windows Auth |
| ClosedXML | Core | Excel reading/writing |
| FuzzySharp | Core | Levenshtein-based fuzzy string matching |
| System.CommandLine | CLI | Command-line argument parsing |
| xunit | Tests | Unit testing framework |
| Moq | Tests | Mocking framework |
| FluentAssertions | Tests | Readable test assertions |
| coverlet.collector | Tests | Code coverage |
| Microsoft.NET.Test.Sdk | Tests | Test SDK |

---

## Supply Chain Security

The project implements multiple layers of defense against NuGet package supply chain attacks.

### Protections in place

| Layer | What it does | File |
|---|---|---|
| **Central Package Management** | All versions defined in one file, exact pins, no wildcards | `Directory.Packages.props` |
| **Lock files** | Records exact resolved dependency tree per project. Any change (even transitive) is detected on next restore | `packages.lock.json` (per project) |
| **NuGet Audit** | Automatically flags packages with known CVEs during `dotnet restore` | `Directory.Packages.props` (`NuGetAudit=true`) |
| **Source pinning** | `<clear />` + explicit nuget.org only — blocks dependency confusion from rogue/private feeds | `nuget.config` |
| **Package age check** | Rejects packages published less than 10 days ago — newly published packages are higher risk | `build/check-package-age.sh` / `.ps1` |
| **Bypass list** | Urgent security fixes can skip the age check with a documented reason (audit trail) | `build/package-age-bypass.txt` |

### Upgrading a package

1. Update the version in `Directory.Packages.props` (single source of truth)
2. Run `dotnet restore --force-evaluate` to regenerate lock files
3. Run the age check:
   - macOS/Linux: `./build/check-package-age.sh`
   - Windows: `.\build\check-package-age.ps1`
4. If the age check fails, either wait for the package to age, or add a bypass (see below)
5. Commit `Directory.Packages.props` + all `packages.lock.json` files in your PR for review

### Bypassing the age check (urgent security fix)

When a critical CVE requires immediate package upgrade:

1. Add an entry to `build/package-age-bypass.txt`:
   ```
   Microsoft.Data.SqlClient|5.2.4|CVE-2026-XXXX critical SQL injection fix
   ```
2. Update the version in `Directory.Packages.props`
3. Run restore and commit — the age check passes via bypass
4. **Remove the bypass entry** once the package is 10+ days old (bypasses should be temporary)

### CI integration

To enforce locked restore in CI (fails if any package differs from the lock file):

Uncomment in `Directory.Packages.props`:
```xml
<RestoreLockedMode>true</RestoreLockedMode>
```

Add the age check as a CI step:
```yaml
# GitHub Actions example
- name: Check package ages
  run: ./build/check-package-age.sh
```

---

## Error Handling Strategy

The tool should handle these scenarios gracefully:

| Scenario                                                             | Behavior                                                                                                                      |
| -------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| Can't connect to SQL Server                                          | Fail fast with clear message: "Cannot connect to {server}. Verify the server is accessible and you have Windows Auth access." |
| Table in config doesn't exist in DB                                  | Fail validation step, list all missing tables, do not proceed                                                                 |
| Column in exclusion/rules doesn't exist                              | Warn but continue (column may have been dropped)                                                                              |
| RecordID not found in a table                                        | Report as "No data found" for that table+scenario (not a crash)                                                               |
| Multiple rows found for a "single row" table (empty RowMatchColumns) | Warn and compare first row, flag in report                                                                                    |
| RowMatchColumns produce duplicate keys                               | Warn, compare first match, flag duplicates in report                                                                          |
| Null values                                                          | Null == Null is a match. Null vs non-null is a mismatch. Display `<NULL>` in report.                                          |
| Excel config file not found                                          | Fail fast: "Config file not found at {path}"                                                                                  |
| Excel config has missing/malformed sheets                            | Fail fast with specific sheet name and expected format                                                                        |
| No Tables-* sheets found in workbook                                 | Fail fast: "No table configuration sheets found. Sheets must use 'Tables-' prefix."                                          |
| --table-set doesn't match any sheet                                  | Error listing available table set sheets                                                                                       |
| CustomQuery missing @RecordID                                        | Fail validation: "CustomQuery for table {name} must contain @RecordID placeholder"                                            |
| CustomQuery contains non-SELECT statement                            | Fail validation: "CustomQuery for table {name} must be a SELECT statement"                                                    |
| Both RecordIDColumn and CustomQuery empty                            | Fail validation: "Table {name} must have either RecordIDColumn or CustomQuery"                                                |
| DB timeout on large table                                            | Respect CommandTimeout config, report timeout error for that table, continue with next                                        |

---

## Key Design Decision: Records Representation

Database records are represented as `Dictionary<string, object?>` rather than strongly-typed models. This is intentional:

```csharp
public class TableRecord
{
    public string TableName { get; init; }
    public string RecordId { get; init; }
    public Dictionary<string, object?> Columns { get; init; } = new();

    // Row match key: composite value of RowMatchColumns for this record
    public string? RowMatchKey { get; set; }
}
```

**Why dictionary instead of typed models:**
- We're comparing N tables with different schemas
- Tables and columns change over time (that's why everything is configurable)
- Adding a new table to comparison should require ZERO code changes
- The comparison engine doesn't need to know what each table represents — it just compares column values
- CustomQuery tables return dynamic result sets — column names are inferred at runtime

---

## What Claude Code Should NOT Do

1. **Do not hardcode any table names, column names, or database details.** Everything comes from the Excel config.
2. **Do not store or prompt for database credentials.** Windows Auth only.
3. **Do not build a web server or API.** This is a console app + class library.
4. **Do not use Entity Framework or any ORM.** Raw ADO.NET (SqlConnection, SqlCommand, SqlDataReader) is simpler and more appropriate for dynamic schema reads.
5. **Do not use Dapper.** While Dapper is great for typed queries, our queries are fully dynamic (table names, column names come from config). Raw SqlDataReader is more appropriate.
6. **Do not include any domain-specific or client-specific references.** This is a generic, open-source tool. Keep all examples generic (Order, OrderLine, Payment, Customer, Address, etc.).
7. **Do not reference `System.Console` from `QuoteCompare.Core`.** All console/UI interaction belongs in the CLI project.

## What Claude Code SHOULD Prioritize

1. **Config-driven everything.** If it's not in the Excel config, it shouldn't affect behavior.
2. **Clear error messages.** A user running this tool should understand exactly what went wrong and how to fix it from the error message alone.
3. **Audit trail in reports.** The HTML report must include: which config was used, which table set was selected, which user ran it, when, what exclusions were applied and why, what the connection details were (server and database, no credentials).
4. **Performance for bulk comparisons.** Many scenarios × many tables = many table reads. Use connection pooling, consider parallel execution per scenario.
5. **Testability.** Comparison logic should be testable with mock data without needing a real database.
6. **Clean open-source readiness.** Good README, LICENSE file, clear documentation, no sensitive data anywhere in the codebase.
7. **Print-friendly HTML reports.** The HTML report must look clean when saved as PDF via the browser print dialog.

---
---

# TODO CHECKLIST — Interactive Work Plan for Claude Code

## How to Use This Checklist

**User instruction to Claude Code:** "Read CLAUDE.md and tell me what needs to be done next."

Claude Code should:
1. Read this entire CLAUDE.md file
2. Find the first TODO item with status `[ ]` (not started)
3. Present the task description, what it will produce, and what inputs it needs from the user
4. Wait for the user to provide required inputs before starting work
5. After completing a task, remind the user to update the checkbox to `[x]`

**Status legend:**
- `[ ]` — Not started
- `[~]` — In progress / partially done
- `[x]` — Complete
- `[!]` — Blocked (needs user input or external dependency)

---

## PHASE 1: Project Setup

### TODO 1.1 — Solution and Project Scaffolding
- **Status:** [ ]
- **What this does:** Creates the .NET 8.0 solution with **two projects** (`QuoteCompare.Core` class library + `QuoteCompare.CLI` console app), test project, folder structure, and all NuGet package references. Sets up the solution so it builds and runs immediately (with a "Hello World" placeholder).
- **What it produces:** `QuoteCompare.sln`, all three `.csproj` files, folder structure as defined in the architecture, `run.bat` and `publish.bat` scripts, README.md, LICENSE, .gitignore.
- **Inputs needed from user BEFORE starting:**

| #   | Question                                                          | Why Claude Code needs this           | Example                                         |
| --- | ----------------------------------------------------------------- | ------------------------------------ | ----------------------------------------------- |
| 1   | What directory should the solution be created in?                 | Project root path                    | Current directory (recommended)                 |
| 2   | What .NET SDK version do you have installed? (`dotnet --version`) | For target framework and global.json | `8.0.400`                                       |
| 3   | Do you want a global.json to pin the SDK version?                 | For team consistency                 | Yes (recommended) / No                          |
| 4   | Should the test project use xUnit, NUnit, or MSTest?              | Test framework preference            | xUnit (recommended)                             |
| 5   | Do you want .editorconfig configured for code style?              | For consistent formatting            | Yes / No                                        |
| 6   | What license do you want for the public repo?                     | For LICENSE file                     | MIT (recommended) / Apache 2.0 / Other          |
| 7   | Git repo — should Claude Code initialize one with .gitignore?     | For version control                  | Yes / No                                        |

- **Definition of done:** `dotnet build` succeeds with zero warnings, `dotnet test` runs (no tests yet but framework is wired), `dotnet run --project src/QuoteCompare.CLI -- --help` shows usage, README.md has project overview.

---

### TODO 1.2 — Configuration Models and Excel Reader
- **Status:** [ ]
- **What this does:** Creates the strongly-typed C# config models (`ComparisonConfig`, `TableConfig` with `CustomQuery` support, `ExclusionConfig`, `ColumnRuleConfig`) and the `ExcelConfigReader` that reads `ComparisonConfig.xlsx` into these models using ClosedXML. Supports `Tables-*` sheet discovery. Also creates the template Excel workbook with example rows, Instructions sheet, and properly formatted headers.
- **What it produces:** All files in `Core/Config/` folder, the template Excel workbook at `config/ComparisonConfig-Template.xlsx`, and unit tests for the config reader.
- **Inputs needed from user BEFORE starting:**

| #   | Question                                                                                                | Why Claude Code needs this            | Example                                        |
| --- | ------------------------------------------------------------------------------------------------------- | ------------------------------------- | ---------------------------------------------- |
| 1   | What is your SQL Server instance name? (will go in template as placeholder, user changes later)         | For the default value in the template | `YOUR_SERVER\INSTANCE`                         |
| 2   | What is the database name? (same — template placeholder)                                                | For the template                      | `YourDatabase`                                 |
| 3   | What is the default command timeout in seconds?                                                         | For long-running queries              | `120` (default)                                |

- **Definition of done:** `ExcelConfigReader` can read a properly formatted workbook with `Tables-*` sheets and `CustomQuery` column, template workbook is generated with example data + Instructions sheet, config validation catches malformed input (missing sheets, empty required fields, CustomQuery without `@RecordID`, both RecordIDColumn and CustomQuery empty), unit tests pass.

---

### TODO 1.3 — Database Connection and Schema Validation
- **Status:** [ ]
- **What this does:** Creates `SqlConnectionFactory` with Windows Integrated Auth (supporting `--server`/`--database` CLI overrides), `SchemaInspector` that validates all configured tables and columns exist in the database, and `RecordDataReader` that reads records using either auto-generated or custom queries.
- **What it produces:** All files in `Core/Database/` folder, `TableRecord` model, integration test stubs.
- **Inputs needed from user BEFORE starting:**

| #   | Question                                                                                          | Why Claude Code needs this                    | Example                                      |
| --- | ------------------------------------------------------------------------------------------------- | --------------------------------------------- | -------------------------------------------- |
| 1   | Can you connect to the database from your dev machine using SSMS with Windows Auth?               | Confirms Windows Auth works from your machine | Yes / No (if no, troubleshoot first)         |
| 2   | Are there any schemas besides `dbo`?                                                              | For schema handling                           | `dbo` only / `dbo`, `staging`, `archive`     |
| 3   | Do any tables have columns with special SQL types (geography, XML, varbinary, hierarchyid, etc.)? | For data type handling in SqlDataReader       | No / Yes (list them)                         |
| 4   | Is there a read-only replica or should we query the primary?                                      | To confirm connection target                  | Primary / Read replica (provide server name) |
| 5   | Provide one valid OldRecordID and one valid NewRecordID for testing                               | To verify the reader works end-to-end         | `OldRecordID: 100234, NewRecordID: 200891`   |

- **Definition of done:** `dotnet run --project src/QuoteCompare.CLI -- --config .\config\ComparisonConfig.xlsx --validate-only` connects to the database, verifies all tables and columns exist, reports the connected user identity, and prints a summary. RecordDataReader handles both auto-generated and CustomQuery modes. No comparison runs yet — just validation.

---

## PHASE 2: Comparison Core

### TODO 2.1 — Value Normalizers
- **Status:** [ ]
- **What this does:** Implements all normalizer classes (`CurrencyNormalizer`, `DateNormalizer`, `FuzzyNormalizer`, `ExactNormalizer`, etc.) and the `NormalizerFactory` that maps `CompareRule` strings to normalizer instances. These are pure logic classes with no database dependency.
- **What it produces:** All files in `Core/Comparison/Normalizers/`, comprehensive unit tests.
- **Inputs needed from user BEFORE starting:**

| #   | Question                                                                           | Why Claude Code needs this       | Example                                                          |
| --- | ---------------------------------------------------------------------------------- | -------------------------------- | ---------------------------------------------------------------- |
| 1   | What date formats exist in the database?                                           | For DateNormalizer patterns      | `datetime`, `datetime2`, `date`, `varchar dates like MM/DD/YYYY` |
| 2   | Are currency values stored as `decimal`, `money`, or `varchar`?                    | For CurrencyNormalizer           | `decimal(18,2)` / `money` / mixed                                |
| 3   | Are there any percentage fields stored as decimals (0.15) vs display format (15%)? | For PercentageNormalizer         | Yes / No / Not sure                                              |
| 4   | What is the acceptable currency tolerance for comparison?                          | For CurrencyNormalizer threshold | `0.01` (penny) / `0.00` (exact)                                  |
| 5   | Are there boolean columns stored as `bit`, `varchar` ('Y'/'N'), or `int` (1/0)?    | For BooleanNormalizer            | `bit` columns / mixed                                            |
| 6   | Any other data type quirks you've noticed?                                         | Catch-all for edge cases         | No / Describe                                                    |

- **Definition of done:** All normalizers implemented with edge case handling (nulls, empty strings, whitespace, malformed values). Unit tests cover 30+ scenarios including null handling, format variations, and boundary cases.

---

### TODO 2.2 — Row Matcher
- **Status:** [ ]
- **What this does:** Implements `RowMatcher` that takes two sets of `TableRecord` rows (old and new) and matches them using the configured `RowMatchColumns`. Handles single-row tables, multi-row matching, unmatched rows, and duplicate key detection.
- **What it produces:** `Core/Comparison/RowMatcher.cs`, `Core/Comparison/Models/RowMatchResult.cs`, unit tests.
- **Inputs needed from user BEFORE starting:**

| #   | Question                                                                                            | Why Claude Code needs this  | Example                                         |
| --- | --------------------------------------------------------------------------------------------------- | --------------------------- | ----------------------------------------------- |
| 1   | Should row matching be case-sensitive on the match columns?                                         | For key comparison logic    | Case-insensitive (recommended) / Case-sensitive |
| 2   | If a table has RowMatchColumns but one side has 0 rows, is that an error or a valid "empty" result? | Edge case handling          | Valid (report "No data in new/old system")      |
| 3   | Should duplicate key rows be compared (first match) or just flagged as errors?                      | Duplicate handling strategy | Compare first + warn (recommended) / Error only |

- **Definition of done:** RowMatcher correctly pairs rows across all scenarios (single row, multi-row, unmatched, duplicates). Unit tests cover 15+ matching scenarios.

---

### TODO 2.3 — Column Comparer and Comparison Engine
- **Status:** [ ]
- **What this does:** Implements `ColumnComparer` (applies exclusions and normalizers per column) and the main `ComparisonEngine` that orchestrates the full flow: load config → connect to DB → for each scenario, for each table, read rows, match, compare, collect results.
- **What it produces:** `Core/ComparisonEngine.cs`, `Core/ColumnComparer.cs`, all result models (`ComparisonResult`, `ScenarioResult`, `TableResult`, `ColumnMismatch`), unit tests with mock data.
- **Inputs needed from user BEFORE starting:**

| #   | Question                                                                       | Why Claude Code needs this        | Example                                                      |
| --- | ------------------------------------------------------------------------------ | --------------------------------- | ------------------------------------------------------------ |
| 1   | Should comparisons run sequentially or in parallel across scenarios?           | Performance vs simplicity         | Sequential first (recommended), parallel later               |
| 2   | Should the engine stop on first critical mismatch or continue all comparisons? | Fail-fast vs comprehensive        | Continue all (recommended for validation)                    |
| 3   | How should SQL NULL vs empty string ('') be treated?                           | Common source of false mismatches | NULL == '' (treat as same) / NULL != '' (treat as different) |

- **Definition of done:** Full comparison pipeline works with mock data (no real DB needed). Given two sets of `TableRecord` objects, engine produces correct `ComparisonResult` with all mismatches, exclusions, and unmatched rows identified. Unit tests verify all comparison rules, null handling, and edge cases.

---

## PHASE 3: Reporting

### TODO 3.1 — Console Report Writer
- **Status:** [ ]
- **What this does:** Implements colored console output that shows a quick pass/fail summary per scenario and per table as the comparison runs. Shows progress during execution and a final summary at the end.
- **What it produces:** `CLI/ConsoleReportWriter.cs`
- **Inputs needed from user BEFORE starting:**

| #   | Question                                                                | Why Claude Code needs this | Example                             |
| --- | ----------------------------------------------------------------------- | -------------------------- | ----------------------------------- |
| 1   | Do you want progress output during execution or just the final summary? | UX preference              | Progress (recommended) / Final only |

- **Definition of done:** Running a comparison prints clear, colored output showing scenario pass/fail status, mismatch counts per table, and a final summary with the report file path.

---

### TODO 3.2 — HTML Report Generator
- **Status:** [ ]
- **What this does:** Generates a self-contained, print-friendly HTML report from `ComparisonResult`. Includes executive summary, scenario overview table (bulk), mismatch summary by table (bulk), per-scenario drill-down, per-table detail with color-coded column comparisons, exclusion audit trail, JavaScript-based filtering, "Save as PDF" button, and `@media print` stylesheet.
- **What it produces:** `Core/Reporting/HtmlReportGenerator.cs`, `Core/Reporting/Templates/ReportTemplate.html`, embedded CSS/JS for the report.
- **Inputs needed from user BEFORE starting:**

| #   | Question                                                       | Why Claude Code needs this                   | Example                                          |
| --- | -------------------------------------------------------------- | -------------------------------------------- | ------------------------------------------------ |
| 1   | Who is the primary audience for the report?                    | Detail level and language                    | Technical QA team / Business stakeholders / Both |
| 2   | What defect tracker do you use? (for CSV export format)        | CSV column format                            | Jira / Azure DevOps / Rally / Generic            |
| 3   | Max how many scenarios and tables in a typical comparison run? | To design the report layout (collapsibility) | 20 scenarios × 16 tables / 50 × 16 / etc.        |

- **Definition of done:** Generating a report from mock comparison data produces a polished, self-contained HTML file. All filters work (scenario, table, mismatches-only). Side-by-side values are clearly readable. Exclusions with reasons are listed. CSV export works. "Save as PDF" produces a clean, readable PDF via browser print dialog. Print layout has proper page breaks, no overflow, readable fonts.

---

### TODO 3.3 — Excel Summary Report (Optional)
- **Status:** [ ]
- **What this does:** Generates an Excel workbook with one row per mismatch, suitable for importing into a defect tracking system or sharing with stakeholders who prefer Excel.
- **What it produces:** `Core/Reporting/ExcelReportGenerator.cs`
- **Inputs needed from user BEFORE starting:**

| #   | Question                                     | Why Claude Code needs this | Example                                                     |
| --- | -------------------------------------------- | -------------------------- | ----------------------------------------------------------- |
| 1   | Do you need this now or can it wait?         | Prioritization             | Now / Later                                                 |
| 2   | What columns should the mismatch Excel have? | For column layout          | Scenario, Table, Column, OldValue, NewValue, Rule (typical) |

- **Definition of done:** Mismatch Excel is generated alongside the HTML report in the timestamped results subfolder. Each row is one mismatch with full context.

---

## PHASE 4: CLI and Integration

### TODO 4.1 — Command-Line Argument Parsing and Interactive Prompts
- **Status:** [ ]
- **What this does:** Implements the full CLI using `System.CommandLine` with all flags: `--config`, `--table-set`, `--old-id`, `--new-id`, `--scenario`, `--pairs`, `--server`, `--database`, `--output`, `--verbose`, `--validate-only`, `--generate-template`, `--setup`. Implements the interactive table set selection prompt and guided setup wizard.
- **What it produces:** Updated `CLI/Program.cs`, `CLI/InteractivePrompts.cs` with all commands wired to their respective handlers.
- **Inputs needed from user BEFORE starting:**

| #   | Question                                                                         | Why Claude Code needs this   | Example                                  |
| --- | -------------------------------------------------------------------------------- | ---------------------------- | ---------------------------------------- |
| 1   | Any additional CLI flags you want?                                               | Completeness                 | No / `--parallel`, `--skip-report`, etc. |
| 2   | Should `--generate-template` require any inputs or just create a blank workbook? | Template generation behavior | Blank template (recommended)             |

- **Definition of done:** All CLI commands work. `--help` shows clear usage for each command. `--validate-only` connects and validates. `--generate-template` creates a template config workbook with Instructions sheet. `--table-set` selects the correct sheet. `--old-id`/`--new-id` runs inline comparison. Interactive table set prompt always shows when `--table-set` not provided. `--setup` walks through guided config creation.

---

### TODO 4.2 — End-to-End Integration Test
- **Status:** [ ]
- **What this does:** Runs the full pipeline against the real database with a known test scenario. Validates the complete flow: Excel config → table set selection → DB connect → read (both auto-generated and custom queries) → match → compare → report output to results/ folder.
- **What it produces:** A real comparison report, plus a list of issues and refinements.
- **Inputs needed from user BEFORE starting:**

| #   | Question                                                                                                           | Why Claude Code needs this        | Example                                  |
| --- | ------------------------------------------------------------------------------------------------------------------ | --------------------------------- | ---------------------------------------- |
| 1   | Provide the completed `ComparisonConfig.xlsx` with real table names, exclusions, and at least 2-3 comparison pairs | Real config for testing           | File path to your filled-out config      |
| 2   | Have you verified both RecordIDs in the pairs have data in all configured tables?                                  | To avoid "no data" false failures | Yes / Let me check                       |
| 3   | Are there any known differences you expect? (helps validate the engine catches them)                               | To verify engine correctness      | e.g., "TableX.VersionColumn will differ" |

- **Definition of done:** Full report generated from real data in timestamped results subfolder. Known differences are correctly flagged. No crashes, no unhandled exceptions. Report is reviewable and accurate. PDF export via browser print dialog looks clean.

---

### TODO 4.3 — Publishing and Distribution
- **Status:** [ ]
- **What this does:** Creates the self-contained publish configuration so the tool can be distributed as a single-folder (or single-file) executable. Creates `publish.bat` and documents the distribution process.
- **What it produces:** Published executable, `publish.bat`, distribution documentation in README.
- **Inputs needed from user BEFORE starting:**

| #   | Question                           | Why Claude Code needs this | Example                                                        |
| --- | ---------------------------------- | -------------------------- | -------------------------------------------------------------- |
| 1   | Target runtime: Windows x64 only?  | For publish RID            | `win-x64` (typical)                                            |
| 2   | Single file or single folder?      | Publish mode               | Single folder (recommended for easier debugging) / Single file |
| 3   | Where will the exe be distributed? | For README instructions    | GitHub releases / Shared network drive / Internal              |

- **Definition of done:** `publish.bat` produces a runnable distribution. Another user can copy the folder, edit the Excel config, and run the tool with zero setup. Results folder is created next to the executable on first run.

---

### TODO 4.4 — README and Documentation for Public Repo
- **Status:** [ ]
- **What this does:** Creates a comprehensive README.md suitable for a public GitHub repository, including: project description, features list, screenshots of the HTML report, installation instructions, configuration guide with Excel workbook documentation, usage examples, contributing guidelines.
- **What it produces:** Updated `README.md`, `CONTRIBUTING.md`, `config/README.md`
- **Inputs needed from user BEFORE starting:**

| #   | Question                                                    | Why Claude Code needs this       | Example                                                   |
| --- | ----------------------------------------------------------- | -------------------------------- | --------------------------------------------------------- |
| 1   | What do you want the GitHub repo name to be?                | For README badges and references | `QuoteCompare` / `db-record-compare` / `data-parity-tool` |
| 2   | Do you want a CONTRIBUTING.md for open-source contributors? | For community guidelines         | Yes / No (keep it personal)                               |
| 3   | Do you have a sample HTML report screenshot to include?     | For README visual appeal         | Will generate from TODO 4.2 / Not yet                     |

- **Definition of done:** README is polished, professional, and contains zero domain-specific or client-specific references. A stranger on GitHub can understand, install, configure, and use the tool from the README alone.

---

## PHASE 5: Enhancements (After Core Works)

### TODO 5.1 — Parallel Scenario Execution
- **Status:** [ ]
- **What this does:** Adds parallel execution for scenarios using `Parallel.ForEachAsync` to speed up bulk comparisons.
- **Inputs needed:** Max degree of parallelism preference (default: 4).
- **Definition of done:** Bulk comparisons run significantly faster. Thread-safe result collection. No data corruption.

---

### TODO 5.2 — Config Change Detection
- **Status:** [ ]
- **What this does:** Before running comparison, checks if the config workbook has been modified since the last run and shows a summary of changes (tables added/removed, exclusions changed).
- **Inputs needed:** None.
- **Definition of done:** User sees "Config changed since last run: 2 new exclusions added, 1 table removed" before comparison starts.

---

### TODO 5.3 — Historical Run Comparison
- **Status:** [ ]
- **What this does:** Saves comparison results as JSON alongside the HTML report in the timestamped results folder. Adds a `--compare-runs` flag that shows what changed between two runs (new mismatches, resolved mismatches).
- **Inputs needed:** None.
- **Definition of done:** User can track mismatch resolution progress across runs.

---

### TODO 5.4 — JSON Config Support
- **Status:** [ ]
- **What this does:** Adds support for JSON configuration files as an alternative to Excel. Useful for version control (JSON diffs cleanly in Git, Excel doesn't) and CI/CD pipelines.
- **Inputs needed:** None.
- **Definition of done:** Tool accepts either `--config file.xlsx` or `--config file.json`. JSON schema is documented. A `--convert-config` flag converts between formats.

---

### TODO 5.5 — WPF Desktop UI (v1.5)
- **Status:** [ ]
- **What this does:** Creates a WPF desktop application (`QuoteCompare.UI`) that wraps `QuoteCompare.Core`. Visual config editor, schema browser, embedded report viewer, table set management.
- **Inputs needed:** WPF design mockups, user workflows.
- **Definition of done:** Full-featured desktop app that can do everything the CLI does plus visual config editing.

---

## Quick Reference: What to Say to Claude Code

**Starting a new session:**
> "Read CLAUDE.md and tell me the next TODO item I need to work on. Show me what inputs you need from me."

**Resuming after providing inputs:**
> "Here are the inputs for TODO X.X: [paste answers]. Go ahead and build it."

**Checking progress:**
> "Read CLAUDE.md and give me a status summary of all TODO items."

**Skipping a step:**
> "Mark TODO X.X as blocked with reason: [reason]. Move to the next TODO."

**Going back to fix something:**
> "I found an issue with TODO X.X. Here's what needs to change: [description]. Update the relevant files."

**Running the tool after it's built:**
> This is a standalone .NET tool — run it from terminal, not through Claude Code.

---

## Dependency Map Between TODOs

```
TODO 1.1 (Solution Setup — Core + CLI + Tests)
  └→ TODO 1.2 (Config Models + Excel Reader + Template)
  │    └→ TODO 1.3 (DB Connection + Schema Validation + CustomQuery)
  │         └→ TODO 4.2 (End-to-End Integration)
  │
  └→ TODO 2.1 (Normalizers)
       └→ TODO 2.2 (Row Matcher)
            └→ TODO 2.3 (Column Comparer + Engine)
                 ├→ TODO 3.1 (Console Report)
                 ├→ TODO 3.2 (HTML Report — print-friendly)
                 ├→ TODO 3.3 (Excel Report — optional)
                 └→ TODO 4.1 (CLI Parsing + Interactive Prompts + Setup Wizard)
                      └→ TODO 4.2 (End-to-End Integration)
                           ├→ TODO 4.3 (Publishing)
                           └→ TODO 4.4 (README for Public Repo)
                                └→ TODO 5.x (Enhancements)
                                     └→ TODO 5.5 (WPF UI — v1.5)

PARALLEL TRACKS POSSIBLE:
- TODO 1.2 and TODO 2.1 can start in parallel after 1.1
- TODO 3.1, 3.2, 3.3 can all start in parallel after 2.3
- TODO 4.1 can start any time after 1.1 (just CLI arg parsing)
- TODO 4.4 can start any time but benefits from having a report screenshot from 4.2
```
