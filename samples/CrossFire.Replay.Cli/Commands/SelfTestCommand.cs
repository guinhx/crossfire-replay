using CrossFire.Replay.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CrossFire.Replay.Cli.Commands;

public sealed class SelfTestCommand : Command
{
    public override int Execute(CommandContext context)
    {
        var failures = SelfTestRunner.RunAll(msg => AnsiConsole.MarkupLine($"[red]{Markup.Escape(msg)}[/]"));
        if (failures == 0)
            AnsiConsole.MarkupLine("[green]Self-test passed.[/]");
        else
            AnsiConsole.MarkupLine($"[red]Self-test failed ({failures} checks).[/]");

        return failures == 0 ? 0 : 3;
    }
}
