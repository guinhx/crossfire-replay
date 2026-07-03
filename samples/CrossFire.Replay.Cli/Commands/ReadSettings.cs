using System.ComponentModel;
using Spectre.Console.Cli;

namespace CrossFire.Replay.Cli.Commands;

public sealed class ReadSettings : CommandSettings
{
    [CommandArgument(0, "<FILE>")]
    [Description("Replay file (.cfr, .cfn, or .cfo).")]
    public string FilePath { get; set; } = string.Empty;

    [CommandOption("-d|--dump")]
    [Description("Print message / packet samples.")]
    [DefaultValue(false)]
    public bool Dump { get; set; }
}
