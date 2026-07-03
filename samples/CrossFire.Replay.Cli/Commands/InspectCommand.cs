using CrossFire.Replay.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CrossFire.Replay.Cli.Commands;

public sealed class InspectCommand : Command<InspectSettings>
{
    public override int Execute(CommandContext context, InspectSettings settings)
    {
        if (!File.Exists(settings.FilePath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] file not found: {Markup.Escape(settings.FilePath)}");
            return 1;
        }

        try
        {
            var bytes = File.ReadAllBytes(settings.FilePath);
            var info = ReplayService.Default.Inspect(settings.FilePath);
            Output.InspectWriter.Write(settings.FilePath, bytes, info, settings.IncludeHex);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Inspect failed:[/] {Markup.Escape(ex.Message)}");
            return 2;
        }
    }
}
