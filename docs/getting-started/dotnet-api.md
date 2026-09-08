# .NET API quick reference

## Entry points

| Type | Purpose |
|---|---|
| `ReplayService.Default` | Detect an outer container and dispatch its payload to a registered reader |
| `ReplayWriteService.Default` | Write a `CfrDocument` or `PacketSimulatorReplayDocument`, optionally in a `.cfo` or `.cfn` container |
| `CfrReader` / `CfrWriter` | Convenience API for CFR documents |
| `SimpleProtocolReader` | Parse an extracted SimpleProtocol payload |
| `PacketSimulatorPayloadReader` | Parse an extracted PacketSimulator payload |

## Documents

Both document classes implement `IReplayDocument`.

| Implementation | `FormatKind` | Common source |
|---|---|---|
| `CfrDocument` | `SimpleProtocol` | Plain `.cfr`, or a SimpleProtocol payload inside a wrapper |
| `PacketSimulatorReplayDocument` | `PacketSimulator` | PacketSimulator payload, commonly inside `.cfo` or `.cfn` |

The extension indicates an expected container, but `ReplayService` detects the actual bytes. A wrapper can contain either supported payload type.

## Containers

| `ReplayContainerKind` | Observed file use | Decoding pipeline |
|---|---|---|
| `None` | Usually `.cfr` | File bytes are the payload |
| `BrotliWrapper` | Usually `.cfo` | 8-byte header followed by Brotli-compressed data |
| `EncryptedBrotliWrapper` | Usually `.cfn` | 8-byte header, AES-CBC data, `CF_ConDeV`, then Brotli-compressed data |

Use `ReplayContainerDecoder` for direct container access or `ReplayService` for end-to-end reading.

## Writing

```csharp
using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Core;

IReplayDocument document = ReplayService.Default.Read("input.cfn");

if (!ReplayWriteService.Default.CanWrite(document))
    throw new NotSupportedException(document.GetType().Name);

ReplayWriteService.Default.WriteFile(
    "output.cfn",
    document,
    ReplayWriteOptions.CfnWrapper);
```

`ReplayWriteService` supports both built-in document classes. With `ContainerKind.None`, it writes the corresponding raw inner payload. `PrependChecksum` is supported only for an unwrapped `CfrDocument`; combining it with a wrapper throws `NotSupportedException`.

Generated wrappers can be decoded by this library, and fixture-preserving writer paths are tested for byte equality. Those tests do not prove that a game client accepts newly generated or modified replay files.

## PacketSimulator properties

```csharp
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;

var path = args.Single();
var packetSimulator = ReplayService.Default.Read(path) as PacketSimulatorReplayDocument
    ?? throw new InvalidOperationException("The replay is not a PacketSimulator document.");

Console.WriteLine(packetSimulator.InnerFormat);
Console.WriteLine(packetSimulator.UnifiedTimeline.Count);
Console.WriteLine(packetSimulator.ExpandedUnifiedTimeline.Count);
Console.WriteLine(packetSimulator.DeduplicatedTimeline.Count);
Console.WriteLine(packetSimulator.BinarySnapshots.Count);
```

Useful modern-layout properties include `ModernDescriptor`, `ModernMetadata`, `MiddleBlobCoverage`, and the decomposed middle/archive layout models. `ExpandedUnifiedTimeline` adds packets heuristically extracted from binary snapshots. Deduplication uses a project-defined packet key and sampled payload fingerprint; it is not a semantic equivalence guarantee.

`PacketSimulatorInnerFormat.LegacyV2022` and `ModernV2026` are project labels for layouts selected by observed signatures. They are not official client format names or a promise that every file from those years has the corresponding layout.

## ILT decoding

```csharp
using CrossFire.Replay.Protocol.Lt;

ReadOnlySpan<byte> payload = LtMessageWriter.Encode(new LtRoundStartDecoded(0));

if (LtMessageReader.TryPeekMessageId(payload, out var id))
    Console.WriteLine(EMessageIdCatalog.GetName((ushort)id));

var decoded = LtMessageReader.TryDecode(payload);
Console.WriteLine(decoded?.GetType().Name ?? "not decoded");
```

`LtMessageWriter.Encode` supports a defined subset of decoded message types and throws `NotSupportedException` for unsupported types. Unknown or unsupported valid IDs may be represented as `LtUnknownDecoded`. The ID catalog is implemented in `src/CrossFire.Replay/Protocol/Lt/EMessageIdCatalog.cs` and `src/CrossFire.Replay/Protocol/Lt/EMessageIdCatalog.Generated.cs`.

## CFR read options and checksums

`SimpleProtocolReadOptions.Lenient` skips unsupported message IDs. Set `SkipMessageParseErrors` to stop at a message parse error and return the preceding messages plus a `CfrParseError`; despite the property name, parsing cannot safely resume without a known message boundary.

Checksum-bearing CFR files have a 32-byte prefix. It is derived from the uppercase 32-character ASCII hexadecimal MD5 of the CFR body. Each byte is adjusted using one of the first four characters of the decimal body length, formatted to a minimum width of four digits, repeated across the block. It is not the raw 16-byte MD5 digest. `CfrDocument.ChecksumValid` is `null` when no prefix is present and otherwise records validation success.

## Errors

`ReplayParseException` includes a byte offset when a recognized layout cannot be parsed. CLI read, export, and coverage commands return exit code `2` for this exception; command-specific validation and other failures may use different behavior.

## Adding a payload format

1. Implement `IReplayPayloadReader.CanRead` and `IReplayPayloadReader.Read`.
2. Construct a `ReplayService` with the new reader. `ReplayService.Default` has a fixed built-in reader list.
3. Add focused synthetic tests and, where available, fixture-backed tests.

Round-trip tests demonstrate the invariants they assert, such as byte preservation or retained fields. They should not be treated as proof that every field has the correct game semantics.

See [architecture.md](../developer-guide/architecture.md).
