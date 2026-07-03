using CrossFire.Replay;
using CrossFire.Replay.Protocol;
using CrossFire.Replay.Protocol.Messages;
using Spectre.Console;

namespace CrossFire.Replay.Cli.Output;

internal static class CfrDocumentWriter
{
    public static void Write(CfrDocument cfr, bool dump)
    {
        var table = new Table().Border(TableBorder.Rounded).Title("SimpleProtocol (.cfr)");
        table.AddColumn("Field");
        table.AddColumn("Value");

        table.AddRow("Version", cfr.FileVersion.ToString());
        table.AddRow("Checksum header", cfr.HasChecksum ? "yes" : "no");
        table.AddRow("Spec defines", string.Join(", ", cfr.SpecDefines));
        table.AddRow("Messages", cfr.Messages.Count.ToString());

        if (cfr.MapInfo is MapInfoMessage map)
        {
            table.AddRow("Map index", map.MapIndex.ToString());
            table.AddRow("Round mode", map.RoundMode.ToString());
            table.AddRow("Win condition", map.MatchWinCondition.ToString());
            table.AddRow("Win goal", map.WinGoal.ToString());
        }

        AnsiConsole.Write(table);

        if (!dump)
            return;

        foreach (var message in cfr.Messages)
        {
            AnsiConsole.MarkupLine(
                $"[grey][[{message.Timestamp,10}]][/] [cyan]{message.MessageId,-35}[/] v{message.ProtocolVersion,2} payload={message.Payload.Length,4}");

            if (message is GenericReplayMessage generic)
            {
                foreach (var (key, value) in generic.Fields)
                    AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(key)}:[/] {Markup.Escape(value?.ToString() ?? "")}");
            }
        }
    }
}
