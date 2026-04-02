using System.CommandLine;
using MsSqlRecordsCompare.Core.Comparison;
using MsSqlRecordsCompare.Core.Comparison.Models;
using MsSqlRecordsCompare.Core.Config;
using MsSqlRecordsCompare.Core.Database;
using MsSqlRecordsCompare.Core.Reporting;

namespace MsSqlRecordsCompare.CLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var configOption = new Option<FileInfo?>("--config") { Description = "Path to the Excel configuration workbook" };
        var tableSetOption = new Option<string?>("--table-set") { Description = "Select a specific table set" };
        var oldIdOption = new Option<string?>("--old-id") { Description = "Old system record ID for quick one-off comparison" };
        var newIdOption = new Option<string?>("--new-id") { Description = "New system record ID for quick one-off comparison" };
        var scenarioOption = new Option<string?>("--scenario") { Description = "Filter to a single scenario, or name an inline comparison" };
        var pairsOption = new Option<FileInfo?>("--pairs") { Description = "CSV file with record ID pairs" };
        var serverOption = new Option<string?>("--server") { Description = "Override SQL Server instance from config" };
        var databaseOption = new Option<string?>("--database") { Description = "Override database name from config" };
        var userOption = new Option<string?>("--user") { Description = "SQL Server user name (uses SQL Auth instead of Windows Auth)" };
        var passwordOption = new Option<string?>("--password") { Description = "SQL Server password (used with --user)" };
        var outputOption = new Option<DirectoryInfo?>("--output") { Description = "Override results output directory" };
        var verboseOption = new Option<bool>("--verbose") { Description = "Enable detailed logging" };
        var validateOnlyOption = new Option<bool>("--validate-only") { Description = "Check config and DB connectivity without running comparison" };
        var generateTemplateOption = new Option<FileInfo?>("--generate-template") { Description = "Generate a blank config template workbook" };
        var setupOption = new Option<bool>("--setup") { Description = "Launch the guided setup wizard" };

        var rootCommand = new RootCommand("MsSqlRecordsCompare — Database Record Comparison Tool")
        {
            configOption, tableSetOption, oldIdOption, newIdOption, scenarioOption,
            pairsOption, serverOption, databaseOption, userOption, passwordOption, outputOption, verboseOption,
            validateOnlyOption, generateTemplateOption, setupOption
        };

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var configFile = parseResult.GetValue(configOption);
            var tableSet = parseResult.GetValue(tableSetOption);
            var oldId = parseResult.GetValue(oldIdOption);
            var newId = parseResult.GetValue(newIdOption);
            var scenario = parseResult.GetValue(scenarioOption);
            var pairsFile = parseResult.GetValue(pairsOption);
            var serverOverride = parseResult.GetValue(serverOption);
            var databaseOverride = parseResult.GetValue(databaseOption);
            var userOverride = parseResult.GetValue(userOption);
            var passwordOverride = parseResult.GetValue(passwordOption);
            var outputDir = parseResult.GetValue(outputOption);
            var verbose = parseResult.GetValue(verboseOption);
            var validateOnly = parseResult.GetValue(validateOnlyOption);
            var generateTemplate = parseResult.GetValue(generateTemplateOption);
            var setup = parseResult.GetValue(setupOption);

            try
            {
                if (setup)
                {
                    ConsoleReportWriter.WriteHeader();
                    ConsoleReportWriter.WriteWarning("  Setup wizard is not yet implemented.");
                    return;
                }

                if (generateTemplate != null)
                {
                    ConsoleReportWriter.WriteHeader();
                    new TemplateGenerator().Generate(generateTemplate.FullName);
                    ConsoleReportWriter.WriteSuccess($"  ✓ Template generated: {generateTemplate.FullName}");
                    ConsoleReportWriter.WriteDim("  Open the workbook, replace example rows with your data, and run:");
                    ConsoleReportWriter.WriteDim($"  MsSqlRecordsCompare --config {generateTemplate.FullName}");
                    return;
                }

                if (configFile == null)
                {
                    ConsoleReportWriter.WriteError("  Error: --config is required. Use --help for usage information.");
                    Environment.ExitCode = 1;
                    return;
                }

                ConsoleReportWriter.WriteHeader();

                // Read config
                var reader = new ExcelConfigReader();

                // Discover table sets
                var availableTableSets = reader.DiscoverTableSets(configFile.FullName);
                if (availableTableSets.Count == 0)
                {
                    ConsoleReportWriter.WriteError("  No table configuration sheets found. Sheets must use 'Tables-' prefix or be named 'Tables'.");
                    Environment.ExitCode = 1;
                    return;
                }

                // Select table set
                string selectedTableSet;
                if (!string.IsNullOrEmpty(tableSet))
                {
                    selectedTableSet = tableSet;
                }
                else
                {
                    selectedTableSet = InteractivePrompts.SelectTableSet(availableTableSets);
                }

                // Load config
                var config = reader.Read(configFile.FullName, selectedTableSet);

                // Apply CLI overrides
                if (!string.IsNullOrEmpty(serverOverride) || !string.IsNullOrEmpty(databaseOverride)
                    || !string.IsNullOrEmpty(userOverride))
                {
                    config = new ComparisonConfig
                    {
                        Connection = new ConnectionConfig
                        {
                            ServerName = serverOverride ?? config.Connection.ServerName,
                            DatabaseName = databaseOverride ?? config.Connection.DatabaseName,
                            UserName = userOverride ?? config.Connection.UserName,
                            Password = passwordOverride ?? config.Connection.Password,
                            CommandTimeout = config.Connection.CommandTimeout,
                            ReportOutputPath = config.Connection.ReportOutputPath
                        },
                        SelectedTableSet = config.SelectedTableSet,
                        Tables = config.Tables,
                        Exclusions = config.Exclusions,
                        ColumnRules = config.ColumnRules,
                        ComparisonPairs = config.ComparisonPairs,
                        AvailableTableSets = config.AvailableTableSets
                    };
                }

                // Validate config
                config.Validate();

                ConsoleReportWriter.WriteTableSetSelection(config.SelectedTableSet, config.Tables.Count);

                // Connect to DB
                ConsoleReportWriter.WriteProgress("Connecting to database...");
                var connectionFactory = new SqlConnectionFactory(
                    config.Connection.ServerName,
                    config.Connection.DatabaseName,
                    config.Connection.CommandTimeout,
                    config.Connection.UserName,
                    config.Connection.Password);

                string userName;
                try
                {
                    userName = await connectionFactory.TestConnectionAsync();
                    ConsoleReportWriter.WriteConnectionInfo(
                        config.Connection.ServerName,
                        config.Connection.DatabaseName,
                        userName);
                }
                catch (Exception ex)
                {
                    ConsoleReportWriter.WriteError($"  ✗ Cannot connect to {config.Connection.ServerName}: {ex.Message}");
                    Environment.ExitCode = 1;
                    return;
                }

                // Schema validation
                ConsoleReportWriter.WriteProgress("Validating schema...");
                var inspector = new SchemaInspector(connectionFactory);
                var validationResult = await inspector.ValidateTablesAsync(config.Tables);
                ConsoleReportWriter.WriteValidationResult(validationResult.IsValid, validationResult.GetSummary());

                if (!validationResult.IsValid)
                {
                    ConsoleReportWriter.WriteError("  Schema validation failed. Fix the issues above before running comparison.");
                    Environment.ExitCode = 1;
                    return;
                }

                if (validateOnly)
                {
                    ConsoleReportWriter.WriteSuccess("  ✓ Validation complete. No comparison was run.");
                    return;
                }

                // Determine comparison pairs
                var pairs = ResolveComparisonPairs(config, oldId, newId, scenario, pairsFile);
                if (pairs.Count == 0)
                {
                    ConsoleReportWriter.WriteError("  No comparison pairs found. Add pairs to the Comparisons sheet, use --old-id/--new-id, or use --pairs.");
                    Environment.ExitCode = 1;
                    return;
                }

                // Filter by scenario if specified (and not inline)
                if (!string.IsNullOrEmpty(scenario) && oldId == null)
                {
                    pairs = pairs.Where(p => p.Scenario.Equals(scenario, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (pairs.Count == 0)
                    {
                        ConsoleReportWriter.WriteError($"  Scenario '{scenario}' not found in comparison pairs.");
                        Environment.ExitCode = 1;
                        return;
                    }
                }

                // Run comparison
                ConsoleReportWriter.WriteProgress($"Running {pairs.Count} comparison(s)...");
                Console.WriteLine();

                var progress = verbose ? new Progress<string>(ConsoleReportWriter.WriteProgress) : null;
                var dataReader = new RecordDataReader(connectionFactory);
                var engine = new ComparisonEngine(config, dataReader, progress);
                var comparisonResult = await engine.RunAsync(configFile.FullName, userName, pairs);

                // Write console results
                foreach (var scenarioResult in comparisonResult.Scenarios)
                {
                    ConsoleReportWriter.WriteScenarioResult(scenarioResult);
                }

                // Determine output directory
                var outputPath = ResolveOutputPath(outputDir, config.Connection.ReportOutputPath);
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                var resultDir = Path.Combine(outputPath, timestamp);
                Directory.CreateDirectory(resultDir);

                // Generate reports
                var htmlPath = Path.Combine(resultDir, "Report.html");
                new HtmlReportGenerator().GenerateToFile(comparisonResult, htmlPath);

                var excelPath = Path.Combine(resultDir, "Summary.xlsx");
                new ExcelReportGenerator().GenerateToFile(comparisonResult, excelPath);

                // Write summary
                ConsoleReportWriter.WriteSummary(comparisonResult);
                ConsoleReportWriter.WriteReportPath(htmlPath);

                if (comparisonResult.FailedScenarios > 0)
                    Environment.ExitCode = 1;
            }
            catch (ConfigValidationException ex)
            {
                ConsoleReportWriter.WriteError($"  Configuration error: {ex.Message}");
                Environment.ExitCode = 1;
            }
            catch (Exception ex)
            {
                ConsoleReportWriter.WriteError($"  Unexpected error: {ex.Message}");
                if (verbose)
                    ConsoleReportWriter.WriteError($"  {ex.StackTrace}");
                Environment.ExitCode = 1;
            }
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static List<ComparisonPair> ResolveComparisonPairs(
        ComparisonConfig config, string? oldId, string? newId, string? scenario, FileInfo? pairsFile)
    {
        // Priority: inline > CSV > Excel sheet
        if (!string.IsNullOrEmpty(oldId) && !string.IsNullOrEmpty(newId))
        {
            return
            [
                new ComparisonPair
                {
                    Scenario = scenario ?? "Inline-Comparison",
                    OldRecordId = oldId,
                    NewRecordId = newId
                }
            ];
        }

        if (pairsFile != null)
        {
            return ReadCsvPairs(pairsFile.FullName);
        }

        return config.ComparisonPairs;
    }

    private static List<ComparisonPair> ReadCsvPairs(string filePath)
    {
        var pairs = new List<ComparisonPair>();
        var lines = File.ReadAllLines(filePath);

        foreach (var line in lines.Skip(1)) // Skip header
        {
            var parts = line.Split(',');
            if (parts.Length >= 3)
            {
                pairs.Add(new ComparisonPair
                {
                    Scenario = parts[0].Trim(),
                    OldRecordId = parts[1].Trim(),
                    NewRecordId = parts[2].Trim()
                });
            }
        }

        return pairs;
    }

    private static string ResolveOutputPath(DirectoryInfo? cliOutput, string? configOutput)
    {
        if (cliOutput != null)
            return cliOutput.FullName;

        if (!string.IsNullOrEmpty(configOutput))
            return configOutput;

        // Default: results/ next to the executable
        var exeDir = AppContext.BaseDirectory;
        return Path.Combine(exeDir, "results");
    }
}
