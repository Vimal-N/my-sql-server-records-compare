using ClosedXML.Excel;
using MsSqlRecordsCompare.Core.Comparison.Models;

namespace MsSqlRecordsCompare.Core.Reporting;

public class ExcelReportGenerator
{
    public void GenerateToFile(ComparisonResult result, string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Mismatches");

        // Headers
        var headers = new[] { "Scenario", "OldRecordID", "NewRecordID", "Table", "RowKey", "Column", "OldValue", "NewValue", "CompareRule", "Tolerance" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#34495e");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var scenario in result.Scenarios)
        {
            foreach (var table in scenario.TableResults)
            {
                // Row mismatches
                foreach (var rowResult in table.RowResults)
                {
                    foreach (var mismatch in rowResult.Mismatches)
                    {
                        sheet.Cell(row, 1).Value = scenario.Scenario;
                        sheet.Cell(row, 2).Value = scenario.OldRecordId;
                        sheet.Cell(row, 3).Value = scenario.NewRecordId;
                        sheet.Cell(row, 4).Value = $"{table.Schema}.{table.TableName}";
                        sheet.Cell(row, 5).Value = rowResult.MatchKey ?? "";
                        sheet.Cell(row, 6).Value = mismatch.ColumnName;
                        sheet.Cell(row, 7).Value = mismatch.OldValue ?? "<NULL>";
                        sheet.Cell(row, 8).Value = mismatch.NewValue ?? "<NULL>";
                        sheet.Cell(row, 9).Value = mismatch.CompareRule;
                        sheet.Cell(row, 10).Value = mismatch.Tolerance ?? "";
                        row++;
                    }
                }

                // Unmatched rows
                foreach (var unmatched in table.UnmatchedOldRows)
                {
                    sheet.Cell(row, 1).Value = scenario.Scenario;
                    sheet.Cell(row, 2).Value = scenario.OldRecordId;
                    sheet.Cell(row, 3).Value = scenario.NewRecordId;
                    sheet.Cell(row, 4).Value = $"{table.Schema}.{table.TableName}";
                    sheet.Cell(row, 6).Value = "(entire row)";
                    sheet.Cell(row, 7).Value = unmatched.Description;
                    sheet.Cell(row, 8).Value = "";
                    sheet.Cell(row, 9).Value = "missing";
                    row++;
                }

                foreach (var unmatched in table.UnmatchedNewRows)
                {
                    sheet.Cell(row, 1).Value = scenario.Scenario;
                    sheet.Cell(row, 2).Value = scenario.OldRecordId;
                    sheet.Cell(row, 3).Value = scenario.NewRecordId;
                    sheet.Cell(row, 4).Value = $"{table.Schema}.{table.TableName}";
                    sheet.Cell(row, 6).Value = "(entire row)";
                    sheet.Cell(row, 7).Value = "";
                    sheet.Cell(row, 8).Value = unmatched.Description;
                    sheet.Cell(row, 9).Value = "extra";
                    row++;
                }
            }
        }

        // Auto-fit columns
        sheet.Columns().AdjustToContents();

        // Add summary sheet
        var summarySheet = workbook.Worksheets.Add("Summary");
        summarySheet.Cell(1, 1).Value = "Metric";
        summarySheet.Cell(1, 2).Value = "Value";
        summarySheet.Cell(1, 1).Style.Font.Bold = true;
        summarySheet.Cell(1, 2).Style.Font.Bold = true;

        summarySheet.Cell(2, 1).Value = "Report Generated";
        summarySheet.Cell(2, 2).Value = result.RunTimestamp.ToString("yyyy-MM-dd HH:mm:ss");
        summarySheet.Cell(3, 1).Value = "Server";
        summarySheet.Cell(3, 2).Value = result.ServerName;
        summarySheet.Cell(4, 1).Value = "Database";
        summarySheet.Cell(4, 2).Value = result.DatabaseName;
        summarySheet.Cell(5, 1).Value = "User";
        summarySheet.Cell(5, 2).Value = result.UserName;
        summarySheet.Cell(6, 1).Value = "Table Set";
        summarySheet.Cell(6, 2).Value = result.TableSet;
        summarySheet.Cell(7, 1).Value = "Total Scenarios";
        summarySheet.Cell(7, 2).Value = result.TotalScenarios;
        summarySheet.Cell(8, 1).Value = "Passed";
        summarySheet.Cell(8, 2).Value = result.PassedScenarios;
        summarySheet.Cell(9, 1).Value = "Failed";
        summarySheet.Cell(9, 2).Value = result.FailedScenarios;
        summarySheet.Cell(10, 1).Value = "Total Mismatches";
        summarySheet.Cell(10, 2).Value = result.TotalMismatches;
        summarySheet.Columns().AdjustToContents();

        workbook.SaveAs(filePath);
    }
}
