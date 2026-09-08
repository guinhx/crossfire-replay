# Contributing

Contributions that improve format accuracy, decoder coverage, tests, and documentation are welcome. Open an issue before undertaking a large change so that scope and evidence can be discussed.

## Development Setup

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), clone the repository, and run:

```bash
dotnet build CrossFire.Replay.sln
dotnet test CrossFire.Replay.sln
```

Tests that use real replay files require local fixtures and return without executing fixture assertions when those files are unavailable. Configure fixtures with `CROSSFIRE_REPLAY_FIXTURE_CFN` or `CROSSFIRE_REPLAY_FIXTURE_FOLDER`; do not commit personal replays, proprietary assets, credentials, or confidential material.

## Change Requirements

- Keep changes focused and follow the existing C# style. The projects target .NET 10 and enable nullable reference types.
- Include a unit, round-trip, or fixture-based regression test for format and decoder changes.
- Distinguish byte preservation from semantic correctness. A byte-identical inner-payload round trip does not prove that decoded fields are complete or that a game client accepts rewritten output.
- Document the client region, build, map, or mode when compatibility depends on it.
- Preserve unknown data rather than assigning unsupported semantics.
- Update public documentation when behavior, limitations, or exported schemas change.
- Retain the notices and visible attribution required by [LICENSE](LICENSE) and [CREDITS.md](CREDITS.md).

## Issues

Use [GitHub Issues](https://github.com/guinhx/crossfire-replay/issues) and the repository templates for bug reports, decode or format gaps, and feature requests. Provide the smallest non-sensitive evidence that reproduces the issue, such as:

- .NET version, operating system, replay extension, and file size.
- The exception and stack trace for parser failures.
- Relevant message IDs, bounded hexadecimal excerpts, and CLI `coverage` output for decoder gaps.
- Client region, build, map, and game mode when known.

Do not post complete replay files publicly unless you have the right to distribute them and have reviewed them for sensitive data.

## Pull Requests

1. Create a focused branch with a descriptive name.
2. Explain the observed behavior, the proposed interpretation, and the evidence supporting it.
3. Add or update tests and documentation as appropriate.
4. Run `dotnet build CrossFire.Replay.sln` and `dotnet test CrossFire.Replay.sln`.
5. Confirm that no unrelated files, replay fixtures, generated dumps, or secrets are included.

The [architecture](docs/developer-guide/architecture.md), [reverse-engineering methodology](docs/developer-guide/reverse-engineering.md), and [format documentation](docs/formats/overview.md) provide additional technical context.

By contributing, you agree that your contribution may be distributed under the repository's [license](LICENSE). Do not submit code or documentation derived from confidential materials, leaked proprietary source code, or internal Smilegate or CrossFire documentation.
