# CFR SimpleProtocol

The project uses **SimpleProtocol** for the text-prefixed binary stream found in observed CFR files.

## Header

```text
ASCII "cfrversion" + four decimal version digits   // 14 bytes total

if fileVersion >= 32:
    i32 featureFlagCount
    repeat featureFlagCount:
        i32 nameLength
        byte[nameLength] name                       // interpreted as ASCII
```

For example, file version 32 starts with `cfrversion0032`. The feature-flag table is absent from earlier file versions. Flags select optional fields in some message layouts.

## Messages

Messages are stored sequentially without a general message-length field:

```text
u8 messageId
[u8 protocolVersion]   // present for the supported handlers when fileVersion >= 5
u32 timestamp
... message-specific body
```

The exact body and, for exceptional handlers, header handling are determined by `messageId`, the file version, the message protocol version, and feature flags. Because there is no universal length field, a parser generally cannot recover the next boundary after an unknown body layout.

## Checksum prefix

Some CFR files place a 32-byte checksum block before `cfrversion`. This block is not a raw 16-byte MD5 digest. `ReplayChecksum.ComputeBlock`:

1. Computes the body's MD5 digest.
2. Converts it to 32 uppercase hexadecimal ASCII characters.
3. Formats the decimal body length to a minimum width of four digits, then adds one of its first four characters, repeated across the block, to each ASCII byte.

`SimpleProtocolReader` detects the prefix by finding `cfrversion` at offset `0x20` and records validation in `CfrDocument.ChecksumValid`. A malformed prefix does not prevent parsing when the body layout remains readable; inspect `ChecksumValid` explicitly when integrity matters.

## Reading

```csharp
using CrossFire.Replay;
using CrossFire.Replay.Formats.SimpleProtocol;

var document = CfrReader.ReadFile("match.cfr");
Console.WriteLine($"messages={document.Messages.Count}");

var payloadBytes = await File.ReadAllBytesAsync("match.cfr");
var sameDocument = SimpleProtocolReader.ReadPayload(payloadBytes, "match.cfr");
```

Typed messages include `MapInfoMessage`, `RoundStartMessage`, and `PlayerDieMessage`. Several supported IDs use `GenericReplayMessage` with a `Fields` dictionary instead of a dedicated type. Unsupported message handling depends on `SimpleProtocolReadOptions`.

## Writing

```csharp
using CrossFire.Replay;

var document = CfrReader.ReadFile("match.cfr");
CfrWriter.WriteFile("plain.cfr", document);
CfrWriter.WriteFile("checksummed.cfr", document, prependChecksum: true);
```

When parsed messages retain their original `Payload` bytes, the writer reuses those bytes. Model-based serialization is available for implemented message types. Round-trip tests validate the asserted bytes and fields; they do not prove complete semantic interpretation or game-client acceptance.

## Wrapped payloads

`ReplayService.Default.Read` can detect a SimpleProtocol payload after decoding a `.cfo` or `.cfn` wrapper. PacketSimulator is a separate payload family; the reader does not treat a PacketSimulator layout as embedded CFR data.

## Implementation

| Area | Repository-relative path |
|---|---|
| IDs | `src/CrossFire.Replay/Protocol/SimpleProtocolId.cs` |
| Deserialization | `src/CrossFire.Replay/Protocol/ProtocolDeserializer.cs` and `src/CrossFire.Replay/Protocol/ProtocolDeserializer.Handlers.cs` |
| Serialization | `src/CrossFire.Replay/Protocol/ProtocolSerializer.cs` |
| Reader and writer | `src/CrossFire.Replay/Formats/SimpleProtocol/` |
| Checksum | `src/CrossFire.Replay/Compression/ReplayChecksum.cs` |

Handlers are registered in `src/CrossFire.Replay/Protocol/ProtocolDeserializer.Handlers.cs`.
