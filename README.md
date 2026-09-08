# CrossFire Replay Toolkit

CrossFire Replay Toolkit is a .NET 8 library for inspecting, reading, and writing observed CrossFire replay formats. It supports SimpleProtocol events and PacketSimulator recordings containing ILT/LithTech packet data.

This is an independent reverse-engineering and interoperability project. It is not affiliated with, sponsored by, or endorsed by Smilegate, a CrossFire publisher, or any other rights holder.

## Project Status

The project is pre-1.0. Public APIs, decoded models, and exported schemas may change between releases.

Current format support is based on files and layouts observed during development, not an official specification:

| Extension | Observed layout | Current support |
| --- | --- | --- |
| `.cfr` | Plain SimpleProtocol event stream | Read and write implemented messages |
| `.cfo` | Brotli-wrapped CFR or legacy PacketSimulator payload | Container decode/encode and supported inner-layout parsing |
| `.cfn` | AES- and Brotli-wrapped modern PacketSimulator payload | Container decode/encode, timeline parsing, and partial semantic ILT decoding |

Tests cover synthetic format cases and optional local replay fixtures. For configured modern `.cfn` fixtures, tests verify byte-for-byte reconstruction of the decoded inner payload; container tests verify encode/decode behavior. These checks do not establish semantic completeness or guarantee that generated files will be accepted by every game client, region, mode, or build. Use the CLI `coverage` command on representative files and validate client compatibility in your own environment.

See [Project Status](docs/project-status.md) for detailed limitations.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Quick Start

```bash
git clone https://github.com/guinhx/crossfire-replay.git
cd crossfire-replay

dotnet build CrossFire.Replay.sln
dotnet test CrossFire.Replay.sln
```

### CLI

```bash
dotnet run --project samples/CrossFire.Replay.Cli -- read replay.cfn
dotnet run --project samples/CrossFire.Replay.Cli -- inspect replay.cfn --hex
dotnet run --project samples/CrossFire.Replay.Cli -- coverage replay.cfn
dotnet run --project samples/CrossFire.Replay.Cli -- export-timeline replay.cfn out.json
dotnet run --project samples/CrossFire.Replay.Cli -- self-test
```

### API

```csharp
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;

var document = ReplayService.Default.Read(@"D:\Replays\match.cfn");

if (document is PacketSimulatorReplayDocument replay)
{
    Console.WriteLine(replay.InnerFormat);
    Console.WriteLine($"Timeline packets: {replay.UnifiedTimeline.Count}");
    Console.WriteLine($"Semantically decoded: {replay.UnifiedTimeline.Count(p => p.Decoded is not null)}");
}
```

Additional examples are available in the [quick-start guide](docs/getting-started/quickstart.md).

### Local Package

```bash
dotnet pack src/CrossFire.Replay/CrossFire.Replay.csproj -c Release
```

The current version is exposed through `ReplayToolkitVersion.Current`. No official public NuGet package is documented at this time.

## Documentation

| Topic | Document |
| --- | --- |
| Documentation index | [docs/README.md](docs/README.md) |
| Quick start and API | [docs/getting-started/quickstart.md](docs/getting-started/quickstart.md) |
| Replay formats | [docs/formats/overview.md](docs/formats/overview.md) |
| Architecture | [docs/developer-guide/architecture.md](docs/developer-guide/architecture.md) |
| Reverse-engineering methodology | [docs/developer-guide/reverse-engineering.md](docs/developer-guide/reverse-engineering.md) |
| Contributing | [CONTRIBUTING.md](CONTRIBUTING.md) |
| Native ILT codecs | [docs/protocol/ilt-native-codecs.md](docs/protocol/ilt-native-codecs.md) |
| Status and limitations | [docs/project-status.md](docs/project-status.md) |
| Local test fixtures | [docs/getting-started/fixtures.md](docs/getting-started/fixtures.md) |

## Repository Layout

```text
CrossFire.Replay.sln
src/CrossFire.Replay/          # Library
samples/CrossFire.Replay.Cli/  # Inspection CLI
tests/CrossFire.Replay.Tests/  # Unit and optional fixture tests
docs/                          # Technical documentation
```

## License and Attribution

Use, modification, distribution, sublicensing, and commercial use are permitted subject to the conditions in [LICENSE](LICENSE). Source and binary distributions must preserve the required visible attribution; see [CREDITS.md](CREDITS.md) for details.

Report defects and format gaps through [GitHub Issues](https://github.com/guinhx/crossfire-replay/issues). Contribution requirements are documented in [CONTRIBUTING.md](CONTRIBUTING.md).
