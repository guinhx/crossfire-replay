using CrossFire.Replay.Cli.Commands;
using Spectre.Console.Cli;

namespace CrossFire.Replay.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("cfreplay");
            config.ValidateExamples();

            config.AddCommand<ReadCommand>("read")
                .WithDescription("Parse a replay and print a summary.")
                .WithExample("read", "replay.cfn")
                .WithExample("read", "replay.cfn", "--dump");

            config.AddCommand<InspectCommand>("inspect")
                .WithDescription("Inspect container layout without a full semantic parse.")
                .WithExample("inspect", "replay.cfn")
                .WithExample("inspect", "replay.cfn", "--hex");

            config.AddCommand<ExportTimelineCommand>("export-timeline")
                .WithDescription("Export PacketSimulator ILT timeline to JSON or CSV.")
                .WithExample("export-timeline", "replay.cfn", "timeline.json")
                .WithExample("export-timeline", "replay.cfn", "timeline.csv");

            config.AddCommand<CoverageCommand>("coverage")
                .WithDescription("Report ILT semantic decode coverage for a PacketSimulator replay.")
                .WithExample("coverage", "replay.cfn")
                .WithExample("coverage", "replay.cfn", "--top", "40");

            config.AddCommand<SelfTestCommand>("self-test")
                .WithDescription("Run built-in CFR / container round-trip checks.");
        });

        return app.Run(CliArgumentNormalizer.Normalize(args));
    }
}

internal static class CliArgumentNormalizer
{
    private static readonly HashSet<string> KnownCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "read",
        "inspect",
        "export-timeline",
        "coverage",
        "self-test",
    };

    private static readonly string[] ReplayExtensions = [".cfr", ".cfn", ".cfo", ".rtp"];

    public static string[] Normalize(string[] args)
    {
        if (args.Length == 0)
            return args;

        if (args[0].Equals("--self-test", StringComparison.OrdinalIgnoreCase))
            return ["self-test", ..args.Skip(1)];

        if (KnownCommands.Contains(args[0]))
            return LegacyFlagToSubcommand(args);

        if (LooksLikeReplayPath(args[0]))
            return LegacyFlagToSubcommand(["read", ..args]);

        return args;
    }

    private static string[] LegacyFlagToSubcommand(string[] args)
    {
        var list = args.ToList();
        var filePath = list.FirstOrDefault(a =>
            !KnownCommands.Contains(a) &&
            !a.StartsWith('-')) ?? string.Empty;

        if (list.Count > 0 && list[0].Equals("read", StringComparison.OrdinalIgnoreCase))
            list.RemoveAt(0);

        if (TryTakeFlag(list, "--self-test", out _))
            return ["self-test"];

        if (TryTakeFlag(list, "--inspect", out _))
            return string.IsNullOrEmpty(filePath) ? ["inspect", ..list] : ["inspect", filePath];

        if (TryTakePair(list, "--export-timeline", out var output))
            return string.IsNullOrEmpty(filePath)
                ? ["export-timeline", ..list, output]
                : ["export-timeline", filePath, output];

        if (args[0].Equals("read", StringComparison.OrdinalIgnoreCase))
            return ["read", filePath, ..list.Where(a => a != filePath)];

        return list.ToArray();
    }

    private static bool LooksLikeReplayPath(string value)
    {
        if (value.StartsWith('-'))
            return false;

        var ext = Path.GetExtension(value);
        if (ReplayExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            return true;

        return File.Exists(value);
    }

    private static bool TryTakeFlag(List<string> args, string flag, out int index)
    {
        index = args.FindIndex(a => a.Equals(flag, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return false;

        args.RemoveAt(index);
        return true;
    }

    private static bool TryTakePair(List<string> args, string flag, out string value)
    {
        value = string.Empty;
        var index = args.FindIndex(a => a.Equals(flag, StringComparison.OrdinalIgnoreCase));
        if (index < 0 || index + 1 >= args.Count)
            return false;

        value = args[index + 1];
        args.RemoveAt(index + 1);
        args.RemoveAt(index);
        return true;
    }
}
