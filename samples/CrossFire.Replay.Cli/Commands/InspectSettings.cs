using System.ComponentModel;
using Spectre.Console.Cli;

namespace CrossFire.Replay.Cli.Commands;

public sealed class InspectSettings : CommandSettings
{
    [CommandArgument(0, "<FILE>")]
    [Description("Replay file (.cfr, .cfn, or .cfo).")]
    public string FilePath { get; set; } = string.Empty;

    [CommandOption("--hex")]
    [Description("Include a short hex preview of the file head.")]
    [DefaultValue(false)]
    public bool IncludeHex { get; set; }
}
