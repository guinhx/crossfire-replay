using CrossFire.Replay;
using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CrossFire.Replay.Cli.Commands;

public sealed class ReadCommand : Command<ReadSettings>
{
    public override int Execute(CommandContext context, ReadSettings settings)
    {
        if (!File.Exists(settings.FilePath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] file not found: {Markup.Escape(settings.FilePath)}");
            return 1;
        }

        try
        {
            var document = ReplayService.Default.Read(settings.FilePath);
            Output.ReplayDocumentWriter.WriteSummary(document, settings.Dump);
            return 0;
        }
        catch (ReplayParseException ex)
        {
            AnsiConsole.MarkupLine($"[red]Parse error:[/] {Markup.Escape(ex.Message)}");
            return 2;
        }
    }
}
