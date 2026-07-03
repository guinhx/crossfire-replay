using System.Text.Json;
using System.Text.Json.Serialization;
using CrossFire.Replay;
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Protocol.Lt;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CrossFire.Replay.Cli.Commands;

public sealed class CoverageCommand : Command<CoverageSettings>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public override int Execute(CommandContext context, CoverageSettings settings)
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
                AnsiConsole.MarkupLine("[red]Error:[/] coverage requires a PacketSimulator replay (.cfn / .cfo).");
                return 1;
            }

            var report = IltDecodeCoverage.Analyze(ps);
            if (settings.Json)
            {
                Console.WriteLine(JsonSerializer.Serialize(report, JsonOptions));
                return 0;
            }

            Output.CoverageWriter.Write(report, settings.Top);
            return 0;
        }
        catch (ReplayParseException ex)
        {
            AnsiConsole.MarkupLine($"[red]Parse error:[/] {Markup.Escape(ex.Message)}");
            return 2;
        }
    }
}
