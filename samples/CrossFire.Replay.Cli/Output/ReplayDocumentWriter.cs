using CrossFire.Replay;
using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Protocol.Messages;
using Spectre.Console;

namespace CrossFire.Replay.Cli.Output;

internal static class ReplayDocumentWriter
{
    public static void WriteSummary(IReplayDocument document, bool dump)
    {
        AnsiConsole.MarkupLine($"[bold]Format[/] {document.FormatKind}");
        if (!string.IsNullOrEmpty(document.SourcePath))
            AnsiConsole.MarkupLine($"[bold]Source[/] {Markup.Escape(document.SourcePath)}");

        switch (document)
        {
            case CfrDocument cfr:
                CfrDocumentWriter.Write(cfr, dump);
                break;
            case PacketSimulatorReplayDocument ps:
                PacketSimulatorDocumentWriter.Write(ps, dump);
                break;
        }
    }
}
