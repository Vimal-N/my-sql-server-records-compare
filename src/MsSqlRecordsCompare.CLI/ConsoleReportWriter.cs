using MsSqlRecordsCompare.Core.Comparison.Models;

namespace MsSqlRecordsCompare.CLI;

public static class ConsoleReportWriter
{
    public static void WriteHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine("  MsSqlRecordsCompare v1.0");
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.ResetColor();
    }

    public static void WriteConnectionInfo(string server, string database, string userName)
    {
        WriteDim($"  Server:   {server}");
        WriteDim($"  Database: {database}");
        WriteSuccess($"  ✓ Connected as {userName}");
        Console.WriteLine();
    }

    public static void WriteValidationResult(bool isValid, string summary)
    {
        if (isValid)
            WriteSuccess($"  ✓ {summary}");
        else
            WriteError($"  ✗ {summary}");
    }

    public static void WriteTableSetSelection(string tableSet, int tableCount)
    {
        WriteDim($"  Table set: {tableSet} ({tableCount} tables)");
    }

    public static void WriteProgress(string message)
    {
        WriteDim($"  {message}");
    }

    public static void WriteScenarioResult(ScenarioResult scenario)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"  Scenario: {scenario.Scenario} ");
        WriteDim($"({scenario.OldRecordId} → {scenario.NewRecordId})");

        foreach (var table in scenario.TableResults)
        {
            WriteTableResult(table);
        }
    }

    public static void WriteTableResult(TableResult table)
    {
        var fullName = $"    {table.Schema}.{table.TableName}";

        if (table.Error != null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{fullName,-40} ✗ ERROR: {table.Error}");
            Console.ResetColor();
            return;
        }

        if (table.HasMismatches)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{fullName,-40} ✗ ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"{table.MismatchCount} mismatch{(table.MismatchCount != 1 ? "es" : "")}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{fullName,-40} ✓ match");
            Console.ResetColor();
        }

        foreach (var warning in table.Warnings)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"      ⚠ {warning}");
            Console.ResetColor();
        }
    }

    public static void WriteSummary(ComparisonResult result)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("───────────────────────────────────────────────");
        Console.WriteLine("  SUMMARY");
        Console.WriteLine("───────────────────────────────────────────────");
        Console.ResetColor();

        // Passed/Failed
        if (result.FailedScenarios == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"  Passed:     ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"{result.PassedScenarios} of {result.TotalScenarios} scenarios");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"  Failed:     ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"{result.FailedScenarios}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($" of ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"{result.TotalScenarios}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" scenarios");
            Console.ResetColor();
        }

        // Mismatches
        if (result.TotalMismatches > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"  Mismatches: ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"{result.TotalMismatches}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" total");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"  Mismatches: ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"0");
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.ResetColor();
    }

    public static void WriteReportPath(string path)
    {
        Console.Write("  Report:     ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(path);
        Console.ResetColor();
    }

    public static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void WriteDim(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
