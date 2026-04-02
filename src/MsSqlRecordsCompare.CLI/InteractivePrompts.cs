namespace MsSqlRecordsCompare.CLI;

public static class InteractivePrompts
{
    public static string SelectTableSet(List<string> availableTableSets)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  Found table configuration sheets:");
        Console.ResetColor();

        for (int i = 0; i < availableTableSets.Count; i++)
        {
            Console.Write("    ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{i + 1}] ");
            Console.ResetColor();
            Console.WriteLine(availableTableSets[i]);
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"  Select a table set [1-{availableTableSets.Count}]: ");
        Console.ResetColor();

        while (true)
        {
            var input = Console.ReadLine()?.Trim();
            if (int.TryParse(input, out var selection) && selection >= 1 && selection <= availableTableSets.Count)
            {
                return availableTableSets[selection - 1];
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"  Invalid selection. Enter a number [1-{availableTableSets.Count}]: ");
            Console.ResetColor();
        }
    }
}
