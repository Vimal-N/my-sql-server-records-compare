using System.Text;
using System.Web;
using MsSqlRecordsCompare.Core.Comparison.Models;

namespace MsSqlRecordsCompare.Core.Reporting;

public class HtmlReportGenerator
{
    public string Generate(ComparisonResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"<title>Comparison Report — {Encode(result.TableSet)} — {result.RunTimestamp:yyyy-MM-dd HH:mm}</title>");
        AppendStyles(sb);
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        AppendHeader(sb, result);
        AppendExecutiveSummary(sb, result);
        AppendFilters(sb);

        if (result.Scenarios.Count > 1)
        {
            AppendScenarioOverviewTable(sb, result);
            AppendMismatchSummaryByTable(sb, result);
        }

        AppendScenarioDetails(sb, result);
        AppendExclusionsAudit(sb, result);
        AppendFooter(sb, result);
        AppendScript(sb);

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    public void GenerateToFile(ComparisonResult result, string filePath)
    {
        var html = Generate(result);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, html, Encoding.UTF8);
    }

    private static void AppendStyles(StringBuilder sb)
    {
        sb.AppendLine("<style>");
        sb.AppendLine("""
            * { box-sizing: border-box; margin: 0; padding: 0; }
            body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; font-size: 14px; color: #333; background: #f5f5f5; padding: 20px; }
            .container { max-width: 1200px; margin: 0 auto; background: #fff; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); padding: 30px; }
            h1 { font-size: 24px; color: #2c3e50; margin-bottom: 5px; }
            h2 { font-size: 18px; color: #34495e; margin: 25px 0 10px; border-bottom: 2px solid #ecf0f1; padding-bottom: 8px; }
            h3 { font-size: 15px; color: #2c3e50; margin: 15px 0 8px; }
            .meta { color: #7f8c8d; font-size: 12px; margin-bottom: 20px; }
            .meta span { margin-right: 20px; }
            .summary-box { display: flex; gap: 20px; margin: 15px 0; flex-wrap: wrap; }
            .summary-card { background: #f8f9fa; border-radius: 6px; padding: 15px 20px; flex: 1; min-width: 140px; text-align: center; border-left: 4px solid #bdc3c7; }
            .summary-card.pass { border-left-color: #27ae60; }
            .summary-card.fail { border-left-color: #e74c3c; }
            .summary-card .number { font-size: 28px; font-weight: bold; }
            .summary-card .label { font-size: 12px; color: #7f8c8d; text-transform: uppercase; }
            .pass .number { color: #27ae60; }
            .fail .number { color: #e74c3c; }
            .filters { margin: 15px 0; padding: 10px; background: #f8f9fa; border-radius: 6px; display: flex; gap: 10px; flex-wrap: wrap; align-items: center; }
            .filters label { font-size: 12px; color: #7f8c8d; }
            .filters select, .filters button { padding: 5px 10px; border: 1px solid #ddd; border-radius: 4px; font-size: 13px; cursor: pointer; }
            .filters button { background: #3498db; color: white; border: none; }
            .filters button:hover { background: #2980b9; }
            .btn-pdf { background: #8e44ad !important; }
            .btn-pdf:hover { background: #7d3c98 !important; }
            table { width: 100%; border-collapse: collapse; margin: 10px 0; font-size: 13px; }
            th { background: #34495e; color: white; padding: 8px 12px; text-align: left; font-weight: 500; }
            td { padding: 8px 12px; border-bottom: 1px solid #ecf0f1; word-break: break-all; }
            tr:hover { background: #f8f9fa; }
            .match { color: #27ae60; } .match-bg { background: #eafaf1; }
            .mismatch { color: #e74c3c; } .mismatch-bg { background: #fdf2f2; }
            .fuzzy { color: #f39c12; } .fuzzy-bg { background: #fef9e7; }
            .excluded { color: #95a5a6; } .excluded-bg { background: #f8f9fa; }
            .scenario-header { cursor: pointer; padding: 12px 15px; margin: 8px 0; border-radius: 6px; display: flex; justify-content: space-between; align-items: center; }
            .scenario-header.pass-header { background: #eafaf1; border: 1px solid #a9dfbf; }
            .scenario-header.fail-header { background: #fdf2f2; border: 1px solid #f5b7b1; }
            .scenario-header .arrow { transition: transform 0.2s; font-size: 12px; }
            .scenario-header.collapsed .arrow { transform: rotate(-90deg); }
            .scenario-body { padding: 0 15px; }
            .scenario-body.hidden { display: none; }
            .table-section { margin: 10px 0; padding: 10px; border: 1px solid #ecf0f1; border-radius: 4px; }
            .table-header { cursor: pointer; display: flex; justify-content: space-between; align-items: center; padding: 5px 0; }
            .table-body.hidden { display: none; }
            .tag { display: inline-block; padding: 2px 8px; border-radius: 3px; font-size: 11px; font-weight: 500; }
            .tag-pass { background: #d5f5e3; color: #1e8449; }
            .tag-fail { background: #fadbd8; color: #c0392b; }
            .tag-warn { background: #fdebd0; color: #d68910; }
            .tag-info { background: #d6eaf8; color: #2471a3; }
            .warning { color: #d68910; font-size: 12px; padding: 3px 0; }
            .overview-table td.pass-cell { background: #eafaf1; text-align: center; }
            .overview-table td.fail-cell { background: #fdf2f2; text-align: center; font-weight: bold; }
            .no-print { }
            @media print {
                body { background: white; padding: 0; font-size: 11pt; }
                .container { box-shadow: none; padding: 0; max-width: 100%; }
                .no-print { display: none !important; }
                .scenario-body.hidden { display: block !important; }
                .table-body.hidden { display: block !important; }
                .scenario-header .arrow { display: none; }
                .scenario-header { cursor: default; page-break-after: avoid; }
                h2 { page-break-after: avoid; }
                table { page-break-inside: avoid; }
                tr { page-break-inside: avoid; }
                .scenario-body { page-break-before: auto; }
                .table-section { page-break-inside: avoid; }
                @page { margin: 1.5cm; }
            }
        """);
        sb.AppendLine("</style>");
    }

    private static void AppendHeader(StringBuilder sb, ComparisonResult result)
    {
        sb.AppendLine("<div class=\"container\">");
        sb.AppendLine("<h1>MsSqlRecordsCompare &mdash; Comparison Report</h1>");
        sb.AppendLine("<div class=\"meta\">");
        sb.AppendLine($"<span>Generated: {result.RunTimestamp:yyyy-MM-dd HH:mm:ss}</span>");
        sb.AppendLine($"<span>User: {Encode(result.UserName)}</span>");
        sb.AppendLine($"<span>Table Set: {Encode(result.TableSet)}</span><br>");
        sb.AppendLine($"<span>Server: {Encode(result.ServerName)}</span>");
        sb.AppendLine($"<span>Database: {Encode(result.DatabaseName)}</span>");
        sb.AppendLine($"<span>Config: {Encode(result.ConfigFile)}</span>");
        sb.AppendLine("</div>");
    }

    private static void AppendExecutiveSummary(StringBuilder sb, ComparisonResult result)
    {
        sb.AppendLine("<h2>Executive Summary</h2>");
        sb.AppendLine("<div class=\"summary-box\">");

        var passClass = result.FailedScenarios == 0 ? "pass" : "fail";
        sb.AppendLine($"<div class=\"summary-card {passClass}\">");
        sb.AppendLine($"<div class=\"number\">{result.PassedScenarios}/{result.TotalScenarios}</div>");
        sb.AppendLine("<div class=\"label\">Scenarios Passed</div></div>");

        var mismatchClass = result.TotalMismatches == 0 ? "pass" : "fail";
        sb.AppendLine($"<div class=\"summary-card {mismatchClass}\">");
        sb.AppendLine($"<div class=\"number\">{result.TotalMismatches}</div>");
        sb.AppendLine("<div class=\"label\">Mismatches</div></div>");

        var totalTables = result.Scenarios.SelectMany(s => s.TableResults).Count();
        sb.AppendLine("<div class=\"summary-card\">");
        sb.AppendLine($"<div class=\"number\">{totalTables}</div>");
        sb.AppendLine("<div class=\"label\">Tables Compared</div></div>");

        sb.AppendLine("</div>");
    }

    private static void AppendFilters(StringBuilder sb)
    {
        sb.AppendLine("<div class=\"filters no-print\">");
        sb.AppendLine("<label>Filter:</label>");
        sb.AppendLine("<button onclick=\"filterScenarios('all')\">Show All</button>");
        sb.AppendLine("<button onclick=\"filterScenarios('mismatches')\">Mismatches Only</button>");
        sb.AppendLine("<button class=\"btn-pdf\" onclick=\"window.print()\">Save as PDF</button>");
        sb.AppendLine("</div>");
    }

    private static void AppendScenarioOverviewTable(StringBuilder sb, ComparisonResult result)
    {
        sb.AppendLine("<h2>Scenario Overview</h2>");
        sb.AppendLine("<table class=\"overview-table\">");
        sb.AppendLine("<tr><th>Scenario</th><th>Old ID</th><th>New ID</th><th>Status</th><th>Mismatches</th></tr>");

        foreach (var scenario in result.Scenarios)
        {
            var statusClass = scenario.Passed ? "pass-cell" : "fail-cell";
            var statusIcon = scenario.Passed ? "✓" : "✗";
            var mismatchText = scenario.Passed ? "&mdash;" : $"{scenario.TotalMismatches}";

            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td>{Encode(scenario.Scenario)}</td>");
            sb.AppendLine($"<td>{Encode(scenario.OldRecordId)}</td>");
            sb.AppendLine($"<td>{Encode(scenario.NewRecordId)}</td>");
            sb.AppendLine($"<td class=\"{statusClass}\">{statusIcon}</td>");
            sb.AppendLine($"<td class=\"{statusClass}\">{mismatchText}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");
    }

    private static void AppendMismatchSummaryByTable(StringBuilder sb, ComparisonResult result)
    {
        var tableSummary = result.Scenarios
            .SelectMany(s => s.TableResults)
            .GroupBy(t => $"{t.Schema}.{t.TableName}")
            .Select(g => new
            {
                Table = g.Key,
                Mismatches = g.Sum(t => t.MismatchCount),
                ScenariosAffected = g.Count(t => t.HasMismatches)
            })
            .OrderByDescending(x => x.Mismatches)
            .ToList();

        sb.AppendLine("<h2>Mismatch Summary by Table</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Table</th><th>Mismatches</th><th>Scenarios Affected</th></tr>");

        foreach (var row in tableSummary)
        {
            var rowClass = row.Mismatches > 0 ? "mismatch-bg" : "";
            sb.AppendLine($"<tr class=\"{rowClass}\">");
            sb.AppendLine($"<td>{Encode(row.Table)}</td>");
            sb.AppendLine($"<td>{row.Mismatches}</td>");
            sb.AppendLine($"<td>{row.ScenariosAffected}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");
    }

    private static void AppendScenarioDetails(StringBuilder sb, ComparisonResult result)
    {
        sb.AppendLine("<h2>Scenario Details</h2>");

        foreach (var scenario in result.Scenarios)
        {
            var headerClass = scenario.Passed ? "pass-header" : "fail-header";
            var bodyClass = scenario.Passed ? "hidden" : "";
            var statusTag = scenario.Passed
                ? "<span class=\"tag tag-pass\">✓ PASSED</span>"
                : $"<span class=\"tag tag-fail\">✗ {scenario.TotalMismatches} MISMATCH{(scenario.TotalMismatches != 1 ? "ES" : "")}</span>";

            var scenarioId = $"scenario-{scenario.Scenario.Replace(" ", "-")}";

            sb.AppendLine($"<div class=\"scenario-block\" data-passed=\"{scenario.Passed.ToString().ToLower()}\">");
            sb.AppendLine($"<div class=\"scenario-header {headerClass}\" onclick=\"toggleSection('{scenarioId}')\">");
            sb.AppendLine($"<span><strong>{Encode(scenario.Scenario)}</strong> <small>({Encode(scenario.OldRecordId)} &rarr; {Encode(scenario.NewRecordId)})</small></span>");
            sb.AppendLine($"<span>{statusTag} <span class=\"arrow\">&#9660;</span></span>");
            sb.AppendLine("</div>");
            sb.AppendLine($"<div id=\"{scenarioId}\" class=\"scenario-body {bodyClass}\">");

            foreach (var table in scenario.TableResults)
            {
                AppendTableDetail(sb, table, scenarioId);
            }

            sb.AppendLine("</div></div>");
        }
    }

    private static void AppendTableDetail(StringBuilder sb, TableResult table, string parentId)
    {
        var tableId = $"{parentId}-{table.Schema}-{table.TableName}";
        var statusTag = table.Error != null
            ? "<span class=\"tag tag-fail\">ERROR</span>"
            : table.HasMismatches
                ? $"<span class=\"tag tag-fail\">✗ {table.MismatchCount}</span>"
                : "<span class=\"tag tag-pass\">✓ match</span>";

        var bodyClass = !table.HasMismatches && table.Error == null ? "hidden" : "";

        sb.AppendLine("<div class=\"table-section\">");
        sb.AppendLine($"<div class=\"table-header\" onclick=\"toggleSection('{tableId}')\">");
        sb.AppendLine($"<span><strong>[{Encode(table.Schema)}].[{Encode(table.TableName)}]</strong> <small>({table.OldRowCount} / {table.NewRowCount} rows)</small></span>");
        sb.AppendLine($"<span>{statusTag} <span class=\"arrow\">&#9660;</span></span>");
        sb.AppendLine("</div>");
        sb.AppendLine($"<div id=\"{tableId}\" class=\"table-body {bodyClass}\">");

        if (table.Error != null)
        {
            sb.AppendLine($"<p class=\"mismatch\"><strong>Error:</strong> {Encode(table.Error)}</p>");
        }
        else
        {
            // Warnings
            foreach (var warning in table.Warnings)
            {
                sb.AppendLine($"<p class=\"warning\">⚠ {Encode(warning)}</p>");
            }

            // Row comparison results
            foreach (var row in table.RowResults)
            {
                if (row.Mismatches.Count == 0 && row.Matches.Count == 0) continue;

                if (!string.IsNullOrEmpty(row.MatchKey))
                    sb.AppendLine($"<h3>Row: {Encode(row.MatchKey)}</h3>");

                if (row.Mismatches.Count > 0 || row.Matches.Count > 0)
                {
                    sb.AppendLine("<table>");
                    sb.AppendLine("<tr><th>Column</th><th>Old Value</th><th>New Value</th><th>Result</th></tr>");

                    foreach (var mismatch in row.Mismatches)
                    {
                        sb.AppendLine($"<tr class=\"mismatch-bg\">");
                        sb.AppendLine($"<td>{Encode(mismatch.ColumnName)}</td>");
                        sb.AppendLine($"<td>{Encode(mismatch.OldValue)}</td>");
                        sb.AppendLine($"<td>{Encode(mismatch.NewValue)}</td>");
                        sb.AppendLine($"<td class=\"mismatch\">✗ DIFF <small>({mismatch.CompareRule})</small></td>");
                        sb.AppendLine("</tr>");
                    }

                    foreach (var match in row.Matches)
                    {
                        sb.AppendLine($"<tr class=\"match-bg\">");
                        sb.AppendLine($"<td>{Encode(match.ColumnName)}</td>");
                        sb.AppendLine($"<td colspan=\"2\">{Encode(match.Value)}</td>");
                        sb.AppendLine($"<td class=\"match\">✓ Match <small>({match.CompareRule})</small></td>");
                        sb.AppendLine("</tr>");
                    }

                    sb.AppendLine("</table>");
                }
            }

            // Unmatched rows
            foreach (var unmatched in table.UnmatchedOldRows)
            {
                sb.AppendLine($"<p class=\"mismatch\">⚠ {Encode(unmatched.Description)}</p>");
            }
            foreach (var unmatched in table.UnmatchedNewRows)
            {
                sb.AppendLine($"<p class=\"mismatch\">⚠ {Encode(unmatched.Description)}</p>");
            }
        }

        sb.AppendLine("</div></div>");
    }

    private static void AppendExclusionsAudit(StringBuilder sb, ComparisonResult result)
    {
        var allExclusions = result.Scenarios
            .SelectMany(s => s.TableResults)
            .SelectMany(t => t.ExcludedColumns.Select(e => new { Table = t.TableName, e.ColumnName, e.Reason }))
            .Distinct()
            .ToList();

        if (allExclusions.Count == 0) return;

        sb.AppendLine("<h2>Exclusions Applied</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Table</th><th>Column</th><th>Reason</th></tr>");

        foreach (var exc in allExclusions.DistinctBy(e => $"{e.Table}.{e.ColumnName}"))
        {
            sb.AppendLine("<tr class=\"excluded-bg\">");
            sb.AppendLine($"<td>{Encode(exc.Table)}</td>");
            sb.AppendLine($"<td>{Encode(exc.ColumnName)}</td>");
            sb.AppendLine($"<td>{Encode(exc.Reason ?? "")}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");
    }

    private static void AppendFooter(StringBuilder sb, ComparisonResult result)
    {
        sb.AppendLine("<div class=\"meta\" style=\"margin-top: 30px; border-top: 1px solid #ecf0f1; padding-top: 10px;\">");
        sb.AppendLine($"Generated by MsSqlRecordsCompare v1.0 at {result.RunTimestamp:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");
    }

    private static void AppendScript(StringBuilder sb)
    {
        sb.AppendLine("<script>");
        sb.AppendLine("""
            function toggleSection(id) {
                var el = document.getElementById(id);
                if (el) el.classList.toggle('hidden');
            }
            function filterScenarios(mode) {
                var blocks = document.querySelectorAll('.scenario-block');
                blocks.forEach(function(block) {
                    if (mode === 'all') {
                        block.style.display = '';
                    } else if (mode === 'mismatches') {
                        block.style.display = block.dataset.passed === 'true' ? 'none' : '';
                    }
                });
            }
        """);
        sb.AppendLine("</script>");
    }

    private static string Encode(string? value)
    {
        return HttpUtility.HtmlEncode(value ?? "") ?? "";
    }
}
