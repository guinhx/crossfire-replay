# Quickstart

## Prerequisites

- .NET 8 SDK
- Optional local replay files for fixture-backed tests; see [fixtures.md](fixtures.md)

## Build and test

Run these commands from the repository root:

```powershell
dotnet restore CrossFire.Replay.sln
dotnet build CrossFire.Replay.sln -c Release
dotnet test CrossFire.Replay.sln -c Release
```

Synthetic tests run without external files. Some integration tests use local `.cfn` fixtures configured through `CROSSFIRE_REPLAY_FIXTURE_CFN` or `CROSSFIRE_REPLAY_FIXTURE_FOLDER`.

## CLI

The CLI project is `samples/CrossFire.Replay.Cli`. Invoke it from the repository root as follows:

```powershell
dotnet run --project samples/CrossFire.Replay.Cli -- read replay.cfn
```

| Arguments after `--` | Effect |
|---|---|
| `read replay.cfn` | Print a parsed document summary |
| `read replay.cfn --dump` | Include a sample of messages or packets |
| `inspect replay.cfn` | Inspect the container and detected inner layout |
| `inspect replay.cfn --hex` | Also print a short hexadecimal file-header preview |
| `coverage replay.cfn` | Report ILT semantic decode coverage |
| `coverage replay.cfn --top 40` | List up to 40 unknown message IDs |
| `export-timeline replay.cfn timeline.json` | Export the ILT timeline as JSON |
| `export-timeline replay.cfn timeline.csv` | Export the ILT timeline as CSV |
| `self-test` | Run built-in CFR and container round-trip checks |

The argument normalizer also accepts legacy forms such as `replay.cfn --inspect`, `replay.cfn --export-timeline timeline.json`, and `--self-test`. Prefer the subcommands above.

`ModernV2026` and `LegacyV2022` in CLI output are names assigned by this project to distinguish observed layouts. They are not official protocol version names.

## Read a replay

The following self-contained example reads a path supplied on the command line, then handles either supported document type:

```csharp
using CrossFire.Replay;
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Formats.SimpleProtocol;

if (args.Length != 1)
    throw new ArgumentException("Pass one replay path.");

var path = args[0];
var service = ReplayService.Default;
var document = service.Read(path);

switch (document)
{
    case CfrDocument cfr:
        foreach (var message in cfr.Messages.Take(20))
            Console.WriteLine($"{message.Timestamp} {message.MessageId}");
        break;

    case PacketSimulatorReplayDocument packetSimulator:
        foreach (var packet in packetSimulator.UnifiedTimeline.Take(20))
            Console.WriteLine($"{packet.Timestamp} {packet.MessageId}");
        break;
}
```

To read bytes already in memory, use `service.Read(bytes, sourcePath)`. Supplying `sourcePath` is optional but improves diagnostics and file-kind reporting.

## Inspect without parsing the document

```csharp
using CrossFire.Replay.Core;

var path = args.Single();
var inspection = ReplayService.Default.Inspect(path);

Console.WriteLine(inspection.ContainerKind);
Console.WriteLine(inspection.PacketSimulatorInnerFormat);
```

For a typical encrypted `.cfn`, `ContainerKind` is `EncryptedBrotliWrapper`; detection is based on bytes, not solely on the extension.

## Decode and export PacketSimulator data

```csharp
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Protocol.Lt;

var path = args.Single();
var document = ReplayService.Default.Read(path);

if (document is not PacketSimulatorReplayDocument packetSimulator)
    throw new InvalidOperationException("The replay is not a PacketSimulator document.");

foreach (var packet in packetSimulator.ExpandedUnifiedTimeline.Take(20))
{
    if (packet.Decoded is LtDamageDecoded damage)
        Console.WriteLine($"damage={damage.Damage} attacker={damage.AttackerIndex}");
}

PacketSimulatorTimelineExporter.WriteJson("timeline.json", packetSimulator);
PacketSimulatorTimelineExporter.WriteCsv("timeline.csv", packetSimulator);
```

`ExpandedUnifiedTimeline` includes ILT packets heuristically extracted from binary-snapshot bodies. Use `UnifiedTimeline` when those embedded packets should remain represented only by their parent snapshots.

## Write replay data

```csharp
using CrossFire.Replay;
using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Core;

var cfr = CfrReader.ReadFile("input.cfr");
CfrWriter.WriteFile("copy.cfr", cfr, prependChecksum: true);

var wrapped = ReplayWriteService.Default.Write(cfr, ReplayWriteOptions.CfnWrapper);
await File.WriteAllBytesAsync("wrapped.cfn", wrapped);
```

`ReplayWriteService` can write both `CfrDocument` and `PacketSimulatorReplayDocument` instances as an unwrapped payload or in `.cfo`/`.cfn` containers. The optional checksum prefix applies only to plain CFR output. Writer and round-trip tests establish parser/writer consistency and, for fixture-preserving paths, byte equality; they do not establish that newly synthesized files are accepted by a game client.

## Reference the library

Add a project reference from another project, adjusting the relative path as needed:

```xml
<ProjectReference Include="..\src\CrossFire.Replay\CrossFire.Replay.csproj" />
```

Or create a local package from the repository root:

```powershell
dotnet pack src/CrossFire.Replay/CrossFire.Replay.csproj -c Release
```

## Next steps

- [.NET API reference](dotnet-api.md)
- [Format overview](../formats/overview.md)
