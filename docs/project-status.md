# Project Status and Known Limitations

This page describes the repository's current implementation and the checks required when evaluating it. It does not certify compatibility with any client release or use case.

## Current Scope

| Area | Implemented behavior |
|------|----------------------|
| CFR SimpleProtocol | Reads and writes the message types implemented by `ProtocolDeserializer` and `ProtocolSerializer` |
| CFN/CFO containers | Decodes and encodes the supported Brotli and encrypted Brotli wrappers |
| PacketSimulator | Reads observed legacy and modern inner layouts and exposes metadata, packet sections, snapshots, and timelines where recognized |
| ILT | Identifies plausible message IDs, semantically decodes supported messages, and preserves unsupported packet payloads where the surrounding format permits |
| Timeline export | Writes JSON and CSV; JSON currently reports schema version `1` and the toolkit version |
| Test suite | Runs synthetic unit and round-trip tests without external replay files; additional tests use optional local fixtures |

The assembly version is exposed as `ReplayToolkitVersion.Current` and is currently pre-1.0. Public APIs and exported schemas may therefore change between releases.

## Evidence and Compatibility

The file and message layouts in this project were inferred through independent reverse engineering. They are implementation hypotheses supported by the test cases and replay samples available to contributors, not official specifications from Smilegate or another rights holder.

A byte-for-byte round-trip demonstrates preservation only for the exact data and code path tested. It does not establish semantic correctness, completeness, compatibility with other regions or client builds, or preservation of every replay variant.

Before adopting the library, evaluate it against a representative, privately held corpus from the relevant client builds, regions, maps, and game modes. Include malformed and unsupported inputs, and repeat the evaluation whenever the capture client or toolkit version changes.

## ILT Decode Coverage

`EMessageIdCatalog` recognizes more IDs than the semantic decoder supports. Recognized but unsupported messages may be represented by `LtUnknownDecoded`; payloads without a plausible ID may remain undecoded. Coverage is data-dependent and a result from one fixture must not be generalized to other replays.

Measure a replay with the sample CLI:

```bash
dotnet run --project samples/CrossFire.Replay.Cli -- coverage replay.cfn
```

Or use the library API:

```csharp
using CrossFire.Replay.Protocol.Lt;

var report = IltDecodeCoverage.Analyze(packetSimulatorDocument);
Console.WriteLine($"{report.SemanticPacketRatio:P1} packets semantic");
```

Treat `ReplayParseException`, `LtUnknownDecoded`, and undecoded payloads as expected outcomes for unverified data. Report reproducible unsupported layouts with the **Decode / format gap** issue template, following the privacy guidance in [Contributing](developer-guide/contributing.md).

## Fixture-Backed Tests

Tests that require real `.cfn` files rely on replay files stored outside the repository. When no suitable file is found, many fixture-dependent test methods return without exercising their fixture assertions; this is not evidence that a local replay was tested.

| Environment variable | Purpose |
|----------------------|---------|
| `CROSSFIRE_REPLAY_FIXTURE_CFN` | Path to one modern `.cfn` reference file |
| `CROSSFIRE_REPLAY_FIXTURE_FOLDER` | Directory containing `*.cfn` files for parameterized tests |

Some tests also look for the specific default path documented in [Local fixtures](getting-started/fixtures.md). That convenience path is machine-specific and is not expected to exist for most contributors or CI systems.

## Deliberate Boundaries

The repository currently provides a .NET library, a sample CLI, tests, and documentation. It does not provide:

- A published NuGet package; the project can be packed locally with `dotnet pack src/CrossFire.Replay/CrossFire.Replay.csproj -c Release`.
- A stable 1.0 API or a compatibility matrix covering client builds.
- Operational features such as authentication, queues, rate limiting, monitoring, or service-level support.
- A guarantee that every replay field or ILT message has a semantic interpretation.

Use the CLI `coverage` and `inspect` commands, round-trip tests on copies of representative files, and application-level failure handling to determine whether the current implementation meets your requirements. The project makes no claim of commercial suitability. Any distribution or product incorporating the software must follow the attribution and no-endorsement requirements in [LICENSE](../LICENSE) and [CREDITS.md](../CREDITS.md).
