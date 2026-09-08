# Architecture

## Components

```text
Sample CLI or consuming application
    Core: ReplayService and ReplayWriteService
    Compression: container detection, Brotli, AES, and checksums
    Formats: SimpleProtocol and PacketSimulator readers/writers
    Protocol: SimpleProtocol IDs, ILT bitstreams, and MM metadata
```

The layers describe responsibilities rather than strict dependency boundaries. Format readers use protocol and I/O types directly, while `ReplayService` coordinates container decoding and payload-reader selection.

## Namespaces

| Namespace | Responsibility |
|-----------|----------------|
| `CrossFire.Replay.Core` | Top-level read and write orchestration |
| `CrossFire.Replay.Compression` | Supported `.cfn` and `.cfo` container operations |
| `CrossFire.Replay.Formats.SimpleProtocol` | CFR payload recognition and reading |
| `CrossFire.Replay.Formats.PacketSimulator` | PacketSimulator layouts, timelines, snapshots, and payload writing |
| `CrossFire.Replay.Protocol.Lt` | ILT bitstreams, message IDs, codecs, and coverage reporting |
| `CrossFire.Replay.Protocol.Mm` | Room information and MM metadata |
| `CrossFire.Replay.Abstractions` | Shared interfaces, format types, options, and toolkit version |
| `CrossFire.Replay.IO` | Binary-reading helpers such as `CfrBinaryReader` |

## Read Path

1. `ReplayService.ResolvePayload` accepts a plain CFR payload, decodes a recognized replay container with `ReplayContainerDecoder`, or passes unrecognized bytes through for payload detection.
2. `ReplayService` tries its registered `IReplayPayloadReader` instances in order. `ReplayService.Default` registers `SimpleProtocolPayloadReader` before `PacketSimulatorPayloadReader`.
3. `PacketSimulatorPayloadReader` distinguishes the observed legacy and modern layouts. Modern payloads are delegated to the internal `PacketSimulatorModernReader`.
4. PacketSimulator readers retain raw data in model properties where supported and build timelines, metadata, and decoded packet views. `PacketSimulatorPacketEnricher` performs ILT enrichment for applicable packet streams.

Unknown PacketSimulator inner data can produce a document with `PacketSimulatorInnerFormat.Unknown`; callers must not assume that recognition implies complete semantic decoding.

## Design Constraints

1. **Preserve before interpreting.** Raw fields and payloads are retained where the implementation supports them. Byte equality proves preservation only for tested inputs and paths.
2. **Tolerate unknown messages.** A plausible but unsupported ILT message can produce `LtUnknownDecoded` instead of a semantic model.
3. **Keep real fixtures local.** Synthetic tests run without external files. Fixture-backed tests are optional and replay data must not be committed.
4. **Document observations as observations.** Names and layouts inferred from replay data are not official protocol definitions.

## Adding an ILT Decoder

1. Establish a layout hypothesis from multiple legally obtained replay samples and document the evidence and uncertainty. See [Reverse-engineering method](reverse-engineering.md).
2. Add or reuse a decoded record in `src/CrossFire.Replay/Protocol/Lt/LtMessages.cs`.
3. Implement decoding in the appropriate file, such as `LtSemanticDecoders.cs`, `LtCsSemanticDecoders.cs`, `LtScReplayDecoders.cs`, or `LtNativeSerializers.cs`.
4. Route the message in `LtMessageReader.TryDecode`. If encoding is supported, route it in `LtMessageWriter.Encode` as well.
5. Add focused synthetic tests. Use private local fixtures as additional evidence, not as a substitute for deterministic tests that run in CI.

## Adding a SimpleProtocol Message

1. Confirm the ID in `src/CrossFire.Replay/Protocol/SimpleProtocolId.cs`.
2. Add the read handler in `src/CrossFire.Replay/Protocol/ProtocolDeserializer.Handlers.cs` and any required serialization support in `ProtocolSerializer.cs`.
3. Add tests for the implemented read/write behavior, including preservation assertions where applicable.

## Solution Projects

| Project | Path | Role |
|---------|------|------|
| `CrossFire.Replay` | `src/CrossFire.Replay/CrossFire.Replay.csproj` | Library |
| `CrossFire.Replay.Cli` | `samples/CrossFire.Replay.Cli/CrossFire.Replay.Cli.csproj` | Sample command-line application |
| `CrossFire.Replay.Tests` | `tests/CrossFire.Replay.Tests/CrossFire.Replay.Tests.csproj` | xUnit test project |
