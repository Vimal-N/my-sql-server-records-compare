# MsSqlRecordsCompare

A database record comparison tool for validating data parity between two systems writing to the same Microsoft SQL Server database.

When migrating from a legacy application to a modernized system, both may perform the same operations and write to shared database tables. This tool takes pairs of record IDs (one from each system), reads their data across configurable tables, and produces a detailed comparison report highlighting any mismatches — field by field, row by row.

## Features

- **Config-driven** — All tables, columns, exclusions, comparison rules, and connection details are configured in an Excel workbook. No code changes needed.
- **Flexible record fetching** — Simple direct lookups via column name, or custom SQL queries for joins, filters, and complex relationships.
- **Multiple table sets** — Define different comparison configurations (e.g., Orders vs. Customers) as separate sheets in the same workbook. Switch between them at runtime.
- **Smart row matching** — For tables with multiple rows per record, match rows by configurable business key columns before comparing.
- **Column comparison rules** — Exact match, case-insensitive, currency (with tolerance), date, numeric, percentage, fuzzy (Levenshtein), boolean normalization, contains, and ignore.
- **Self-contained HTML report** — Single-file report with filtering, drill-down, color coding, CSV export, and a "Save as PDF" button. Print-friendly layout with proper page breaks.
- **Excel summary export** — One row per mismatch, ready for import into a defect tracker.
- **Guided setup wizard** — Interactive CLI wizard that connects to your database, discovers tables, and generates your config workbook.
- **Windows Integrated Auth & SQL Auth** — Uses Windows credentials by default. Also supports SQL Server authentication with `--user` / `--password` for Docker, Linux, or macOS environments.
- **Cross-platform** — Runs on Windows, macOS, and Linux wherever .NET and SQL Server are available.

## Quick Start

### Prerequisites

- .NET 10.0 SDK (or Runtime for published executables)
- Access to a Microsoft SQL Server instance
- SELECT permissions on the tables you want to compare

### Installation

**Option A — Build from source:**

```bash
git clone https://github.com/your-username/my-sql-server-records-compare.git
cd my-sql-server-records-compare
dotnet build
```

**Option B — Download a published executable** from the [Releases](../../releases) page.

### First Run

**1. Generate a config template:**

```bash
# Windows
dotnet run --project src/MsSqlRecordsCompare.CLI -- --generate-template .\config\MyConfig.xlsx

# macOS / Linux
dotnet run --project src/MsSqlRecordsCompare.CLI -- --generate-template ./config/MyConfig.xlsx
```

This creates an Excel workbook with example rows and an Instructions sheet explaining every column.

**2. Edit the config workbook:**

Open the generated Excel file and:
- Set your server and database in the `ConnectionConfig` sheet
- Configure your tables in the `Tables-*` sheet (add/remove tables, set RecordIDColumn or CustomQuery)
- Add column exclusions in the `Exclusions` sheet
- Add comparison rules in the `ColumnRules` sheet (optional)
- Add record ID pairs in the `Comparisons` sheet

**3. Run a comparison:**

```bash
# Windows (Integrated Auth)
dotnet run --project src/MsSqlRecordsCompare.CLI -- --config .\config\MyConfig.xlsx

# macOS / Linux / Docker (SQL Auth)
dotnet run --project src/MsSqlRecordsCompare.CLI -- \
  --config ./config/MyConfig.xlsx \
  --user sa --password "YourPassword"
```

## Usage

```bash
# Run comparison (interactive table set selection)
dotnet run --project src/MsSqlRecordsCompare.CLI -- --config ./config/ComparisonConfig.xlsx

# Specify table set directly
dotnet run --project src/MsSqlRecordsCompare.CLI -- --config ./config/ComparisonConfig.xlsx --table-set "Orders"

# Quick one-off comparison
dotnet run --project src/MsSqlRecordsCompare.CLI -- --config ./config/ComparisonConfig.xlsx --old-id 100234 --new-id 200891

# Quick one-off with a scenario name
dotnet run --project src/MsSqlRecordsCompare.CLI -- --config ./config/ComparisonConfig.xlsx --old-id 100234 --new-id 200891 --scenario "Quick-Test"

# Run a single scenario from the Comparisons sheet
dotnet run --project src/MsSqlRecordsCompare.CLI -- --config ./config/ComparisonConfig.xlsx --scenario "Basic-Order"

# Bulk pairs from CSV file
dotnet run --project src/MsSqlRecordsCompare.CLI -- --config ./config/ComparisonConfig.xlsx --pairs ./data/record-pairs.csv

# Override database connection (same config, different environment)
dotnet run --project src/MsSqlRecordsCompare.CLI -- --config ./config/ComparisonConfig.xlsx --server "STAGING\SQL" --database "OrdersDB_QA"

# SQL Server Authentication (Docker, Linux, macOS)
dotnet run --project src/MsSqlRecordsCompare.CLI -- --config ./config/ComparisonConfig.xlsx --server "localhost,1433" --user sa --password "YourPassword"

# Custom output directory
dotnet run --project src/MsSqlRecordsCompare.CLI -- --config ./config/ComparisonConfig.xlsx --output ./my-results

# Validate config only (no comparison)
dotnet run --project src/MsSqlRecordsCompare.CLI -- --config ./config/ComparisonConfig.xlsx --validate-only

# Verbose logging
dotnet run --project src/MsSqlRecordsCompare.CLI -- --config ./config/ComparisonConfig.xlsx --verbose
```

### CLI Flags

| Flag | Description |
|---|---|
| `--config <path>` | Path to the Excel configuration workbook |
| `--table-set <name>` | Select a specific table set (e.g., "Orders" matches "Tables-Orders") |
| `--old-id <id>` | Old system record ID for quick one-off comparison |
| `--new-id <id>` | New system record ID for quick one-off comparison |
| `--scenario <name>` | Filter to a single scenario, or name an inline comparison |
| `--pairs <path>` | CSV file with record ID pairs |
| `--server <server>` | Override SQL Server from config |
| `--database <db>` | Override database from config |
| `--user <username>` | SQL Server user name (uses SQL Auth instead of Windows Auth) |
| `--password <password>` | SQL Server password (used with `--user`) |
| `--output <path>` | Override results output directory |
| `--verbose` | Enable detailed logging |
| `--validate-only` | Check config and DB connectivity without running comparison |
| `--generate-template <path>` | Generate a blank config template workbook |
| `--setup` | Launch the guided setup wizard |

## Configuration

All configuration lives in a single Excel workbook with the following sheets:

### ConnectionConfig

Key-value pairs for database connection and tool settings.

| Setting | Description | Example |
|---|---|---|
| ServerName | SQL Server instance | `SQLPROD01\MAIN` or `localhost,1433` |
| DatabaseName | Database name | `OrdersDB` |
| UserName | SQL Auth user name (optional, leave empty for Windows Auth) | `sa` |
| Password | SQL Auth password (optional, used with UserName) | |
| CommandTimeout | Query timeout in seconds | `120` |
| ReportOutputPath | Results output directory (optional) | `.\results` |

### Tables-* (Table Sets)

Each `Tables-*` sheet defines a set of tables to compare. You can have multiple sheets (e.g., `Tables-Orders`, `Tables-Customers`) for different comparison types. Select which one to use at runtime.

| Column | Required | Description |
|---|---|---|
| TableName | Yes | Name of the database table |
| Schema | Yes | Schema name (e.g., `dbo`) |
| RecordIDColumn | Conditional | Column for direct `WHERE col = @RecordID` lookups |
| RowMatchColumns | No | Comma-separated columns for matching rows in multi-row tables |
| CustomQuery | Conditional | Full SQL SELECT with `@RecordID` placeholder |

**You must provide either `RecordIDColumn` or `CustomQuery` (or both) for each table.**

**Examples:**

| Use Case | RecordIDColumn | CustomQuery |
|---|---|---|
| Simple lookup | `RecordID` | _(empty)_ |
| Multi-row with matching | `RecordID` | _(empty)_ |
| Join / indirect relationship | _(empty)_ | `SELECT p.* FROM Payment p INNER JOIN OrderPayment op ON p.PaymentID = op.PaymentID WHERE op.RecordID = @RecordID` |
| Filtered subset | `RecordID` | `SELECT * FROM AuditLog WHERE RecordID = @RecordID AND Category = 'Financial'` |

### Exclusions

Columns to skip during comparison. Supports `*` wildcard for TableName to apply across all tables.

| Column | Description |
|---|---|
| TableName | Table name or `*` for all tables |
| ColumnName | Column to exclude |
| Reason | Why this column is excluded (shown in report) |

### ColumnRules (Optional)

Fine-grained comparison rules per column. If not specified, the default is `exact`.

| Rule | Description |
|---|---|
| `exact` | Trim whitespace, exact string match. Nulls match nulls. |
| `exact-ci` | Case-insensitive exact match |
| `currency` | Parse as decimal, compare with tolerance (default 0.01) |
| `date` | Compare date portion only |
| `datetime` | Compare with optional time tolerance |
| `numeric` | Parse as decimal with tolerance |
| `percentage` | Normalize 0.15 vs 15%, compare as decimal |
| `fuzzy` | Levenshtein similarity with threshold (default 0.90) |
| `boolean` | Normalize true/false/1/0/yes/no/Y/N |
| `contains` | New value contains old value |
| `ignore` | Skip comparison (column still appears in report) |

### Comparisons

Pairs of record IDs to compare.

| Column | Description |
|---|---|
| Scenario | Descriptive name for this comparison |
| OldRecordID | Record ID from the old/legacy system |
| NewRecordID | Record ID from the new/modern system |

Record IDs support any string format (integers, GUIDs, alphanumeric codes).

**Alternative:** Provide pairs via CSV file (`--pairs`) or inline CLI flags (`--old-id` / `--new-id`).

## Output

Results are saved to a `results` folder next to the executable (overridable via `--output` or config). Each run creates a timestamped subfolder:

```
results/
  2026-03-31_142205/
    Report.html          <- self-contained HTML report
    Summary.xlsx         <- one row per mismatch
  2026-03-31_153010/
    Report.html
    Summary.xlsx
```

### HTML Report

The HTML report is a single self-contained file (no external dependencies) that opens in any browser. It includes:

- **Executive summary** — pass/fail counts, total mismatches
- **Scenario overview table** — quick scan across all scenarios (bulk runs)
- **Mismatch summary by table** — which tables have the most issues
- **Per-scenario drill-down** — expand to see table-by-table results
- **Per-table detail** — side-by-side column values with color coding
- **Exclusion audit trail** — which columns were excluded and why
- **Filtering** — show all / mismatches only / by table / by scenario
- **CSV export** — copy mismatch data for each section
- **Save as PDF** — triggers browser print dialog with a clean, paginated layout

## Project Structure

```
my-sql-server-records-compare/
├── src/
│   ├── MsSqlRecordsCompare.Core/     <- class library (engine, config, DB, reports)
│   └── MsSqlRecordsCompare.CLI/      <- console app (CLI, prompts, console output)
├── tests/
│   ├── MsSqlRecordsCompare.Tests/    <- unit tests (195 tests)
│   └── e2e/                          <- end-to-end test environment
│       ├── setup-e2e.sh              <- single command to spin up test DB
│       ├── docker-compose.yml        <- SQL Server 2022 in Docker
│       ├── 01-schema.sql             <- test database schema (7 tables)
│       └── 02-seed-scenario*.sql     <- 5 test scenarios with seed data
├── build/                            <- supply chain security scripts
├── config/                           <- configuration workbook templates
├── results/                          <- comparison output (auto-created)
└── scripts/                          <- run/publish helper scripts
```

The Core + CLI separation enables a future WPF desktop UI that reuses the same comparison engine.

## Development

### Building

```bash
dotnet build
dotnet test
```

### End-to-End Testing

The project includes a complete E2E test environment using Docker with SQL Server 2022. A single script creates the database, seeds 5 test scenarios covering all comparison paths, and prints the commands to run the tool.

```bash
# Start everything (build, generate config, start Docker, create DB, seed data)
./tests/e2e/setup-e2e.sh

# Reset (tear down and start fresh)
./tests/e2e/setup-e2e.sh --reset

# Tear down
./tests/e2e/setup-e2e.sh --down
```

**Test scenarios:**

| Scenario | What it tests |
|---|---|
| Perfect-Match | Zero false positives across all rule types |
| Known-Mismatches | Deliberate differences per comparison rule |
| Row-Count-Diff | Missing/extra rows in multi-row tables |
| Null-Handling | NULL vs NULL, NULL vs value |
| Complex-Queries-Dupes | Custom JOIN queries, filtered subsets, duplicate key detection |

**Requirements:** Docker Desktop with SQL Server 2022 image.

### Building a Standalone Executable

You can package the tool as a self-contained executable that runs without needing .NET installed. The output is a single folder with the executable and all dependencies bundled in.

**Step 1 — Publish for your platform:**

```bash
# Windows (produces MsSqlRecordsCompare.exe)
dotnet publish src/MsSqlRecordsCompare.CLI -c Release -r win-x64 --self-contained -o ./publish

# macOS Apple Silicon (produces MsSqlRecordsCompare)
dotnet publish src/MsSqlRecordsCompare.CLI -c Release -r osx-arm64 --self-contained -o ./publish

# macOS Intel (produces MsSqlRecordsCompare)
dotnet publish src/MsSqlRecordsCompare.CLI -c Release -r osx-x64 --self-contained -o ./publish

# Linux (produces MsSqlRecordsCompare)
dotnet publish src/MsSqlRecordsCompare.CLI -c Release -r linux-x64 --self-contained -o ./publish
```

Or use the helper scripts that auto-detect your platform:

```bash
# macOS / Linux
./scripts/publish.sh

# Windows
scripts\publish.bat
```

**Step 2 — Run the executable directly:**

```bash
# Windows
.\publish\MsSqlRecordsCompare.exe --help
.\publish\MsSqlRecordsCompare.exe --generate-template .\MyConfig.xlsx
.\publish\MsSqlRecordsCompare.exe --config .\MyConfig.xlsx

# macOS / Linux
./publish/MsSqlRecordsCompare --help
./publish/MsSqlRecordsCompare --generate-template ./MyConfig.xlsx
./publish/MsSqlRecordsCompare --config ./MyConfig.xlsx --server "localhost,1433" --user sa --password "YourPass"
```

**Step 3 — Distribute to others:**

Copy the entire `publish/` folder to any machine. No .NET SDK or runtime installation required — everything is bundled. Recipients just run the executable.

```
publish/
  MsSqlRecordsCompare.exe   <- (or MsSqlRecordsCompare on macOS/Linux)
  MsSqlRecordsCompare.dll
  ... (runtime dependencies)
```

> **Tip:** On macOS/Linux, you may need to mark the binary as executable after unzipping: `chmod +x MsSqlRecordsCompare`

## Supply Chain Security

All NuGet packages use exact pinned versions (no wildcards) with lock files, NuGet audit, source pinning, and a package age check that rejects packages published less than 10 days ago.

```bash
# Check all package ages
./build/check-package-age.sh        # macOS/Linux
.\build\check-package-age.ps1       # Windows
```

## License

[Apache License 2.0](LICENSE)
