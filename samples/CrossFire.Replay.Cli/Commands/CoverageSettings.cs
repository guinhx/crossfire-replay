using System.ComponentModel;
using Spectre.Console.Cli;

namespace CrossFire.Replay.Cli.Commands;

public sealed class CoverageSettings : CommandSettings
{
    [CommandArgument(0, "<FILE>")]
    [Description("PacketSimulator replay (.cfn or .cfo).")]
    public string FilePath { get; set; } = string.Empty;

    [CommandOption("--top")]
    [Description("Number of unknown message IDs to list (default 25).")]
    [DefaultValue(25)]
    public int Top { get; set; } = 25;

    [CommandOption("--json")]
    [Description("Write full coverage report as JSON to stdout.")]
    [DefaultValue(false)]
    public bool Json { get; set; }
}
