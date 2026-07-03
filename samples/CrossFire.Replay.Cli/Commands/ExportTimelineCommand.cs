using CrossFire.Replay;
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CrossFire.Replay.Cli.Commands;

public sealed class ExportTimelineCommand : Command<ExportTimelineSettings>
{
    public override int Execute(CommandContext context, ExportTimelineSettings settings)
    {
        if (!File.Exists(settings.FilePath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] file not found: {Markup.Escape(settings.FilePath)}");
            return 1;
        }

        try
        {
            var document = ReplayService.Default.Read(settings.FilePath);
            if (document is not PacketSimulatorReplayDocument ps)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] export-timeline requires a PacketSimulator replay (.cfn / .cfo).");
                return 1;
            }

            if (settings.OutputPath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                PacketSimulatorTimelineExporter.WriteCsv(settings.OutputPath, ps);
            else
                PacketSimulatorTimelineExporter.WriteJson(settings.OutputPath, ps);

            AnsiConsole.MarkupLine($"[green]Exported[/] {Markup.Escape(settings.OutputPath)}");
            AnsiConsole.MarkupLine(
                $"  unified={ps.UnifiedTimeline.Count} deduplicated={ps.DeduplicatedTimeline.Count}");
            return 0;
        }
        catch (ReplayParseException ex)
        {
            AnsiConsole.MarkupLine($"[red]Parse error:[/] {Markup.Escape(ex.Message)}");
            return 2;
        }
    }
}
