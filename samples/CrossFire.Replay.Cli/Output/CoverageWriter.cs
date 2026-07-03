using CrossFire.Replay.Protocol.Lt;
using Spectre.Console;

namespace CrossFire.Replay.Cli.Output;

internal static class CoverageWriter
{
    public static void Write(IltDecodeCoverageReport report, int topUnknown)
    {
        var summary = new Table().Border(TableBorder.Rounded).Title("ILT decode coverage");
        summary.AddColumn("Metric");
        summary.AddColumn("Value");

        summary.AddRow("Timeline packets", report.TotalPackets.ToString("N0"));
        summary.AddRow("With message ID", $"{report.WithMessageId:N0} ({Pct(report.WithMessageId, report.TotalPackets)})");
        summary.AddRow("Semantically decoded", $"{report.SemanticallyDecoded:N0} ({Pct(report.SemanticallyDecoded, report.WithMessageId)})");
        summary.AddRow("LtUnknownDecoded", $"{report.UnknownDecoded:N0} ({Pct(report.UnknownDecoded, report.WithMessageId)})");
        summary.AddRow("No ILT id / no decode", report.Undecoded.ToString("N0"));
        summary.AddRow("Distinct message IDs", report.DistinctMessageIds.ToString());
        summary.AddRow("IDs fully semantic", $"{report.DistinctSemanticIds} ({report.SemanticIdRatio:P1})");
        summary.AddRow("IDs with unknown payloads", report.DistinctUnknownIds.ToString());

        AnsiConsole.Write(summary);

        var unknown = report.ByMessageId
            .Where(static c => c.Unknown > 0)
            .OrderByDescending(static c => c.Unknown)
            .Take(Math.Max(1, topUnknown))
            .ToArray();

        if (unknown.Length == 0)
        {
            AnsiConsole.MarkupLine("[green]No LtUnknownDecoded message IDs in timeline.[/]");
            return;
        }

        AnsiConsole.WriteLine();
        var table = new Table().Border(TableBorder.Rounded).Title($"Top unknown IDs (max {topUnknown})");
        table.AddColumn("ID");
        table.AddColumn("Name");
        table.AddColumn("Unknown");
        table.AddColumn("Total");

        foreach (var row in unknown)
        {
            table.AddRow(
                $"0x{(ushort)row.MessageId:X4}",
                Markup.Escape(row.Name),
                row.Unknown.ToString("N0"),
                row.Total.ToString("N0"));
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("[grey]Use --json for the full per-ID breakdown. Open decode/format gap issues for priority IDs.[/]");
    }

    private static string Pct(int part, int total) =>
        total == 0 ? "0%" : $"{100.0 * part / total:F1}%";
}
