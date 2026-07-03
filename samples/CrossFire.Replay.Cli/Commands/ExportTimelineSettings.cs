using System.ComponentModel;
using Spectre.Console.Cli;

namespace CrossFire.Replay.Cli.Commands;

public sealed class ExportTimelineSettings : CommandSettings
{
    [CommandArgument(0, "<FILE>")]
    [Description("PacketSimulator replay (.cfn or .cfo).")]
    public string FilePath { get; set; } = string.Empty;

    [CommandArgument(1, "<OUTPUT>")]
    [Description("Output path (.json or .csv).")]
    public string OutputPath { get; set; } = string.Empty;
}
