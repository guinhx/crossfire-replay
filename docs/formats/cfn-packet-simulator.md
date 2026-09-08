# CFN PacketSimulator payload

This page describes PacketSimulator inner payloads recognized by the project. A `.cfn` file normally adds an encrypted and compressed outer wrapper; `ReplayContainerDecoder` removes that wrapper before PacketSimulator parsing.

The labels `ModernV2026` and `LegacyV2022` are repository classifications derived from observed signatures and reference fixtures. They are not official format names, and the year suffixes do not define universal client-version boundaries.

## `ModernV2026` classification

The reader selects this layout when the inner payload begins with `0x7FFFFFFE`:

```text
u32 signature = 0x7FFFFFFE
u32 metadataSize
u32 extraSize
byte[metadataSize] metadata
byte[extraSize] extra
descriptor and labels
middle region
spectating archive
game archive
special-effect archive
```

The descriptor records the three archive sizes. The reader locates those archives from the end of the payload; bytes between the metadata/extra area and the archive suffix form the middle region and include descriptor data.

The reference fixture used by the repository has a 1,024-byte metadata block and a 4-byte extra block. The reader consumes the declared sizes rather than requiring those exact values, although model-based writing defaults to the observed constants in `PacketSimulatorLayout`.

## Metadata

For the observed 1,024-byte metadata block, the project interprets:

- the first 512 bytes as a `PacketSimulatorHeaderBlock` containing fields such as map ID, game goal, and clan-game data;
- the remaining bytes as an extension tail;
- a room-information prefix when one can be located by the current heuristics.

Room-related values may also be inferred from the descriptor and middle-region header. These are independent observed sources and may not always agree. See `src/CrossFire.Replay/Protocol/Mm/ModernMetadataBlockReader.cs`.

## Timestamp archives

The basic timestamp-packet representation parsed inside an archive is:

```text
u32 payloadSize
u32 timestamp
byte[payloadSize] payload
```

Observed archives may precede packets with a legacy count field or a project-recognized tagged header. The parser also uses bounded resynchronization in lenient mode. An ILT message ID is bit-packed at the start of payloads that pass `LtMessageReader.TryPeekMessageId`; it should not be described as an unconditional aligned 16-bit field.

Payload classification uses implementation thresholds and heuristics:

- payloads of at least 65,536 bytes are classified as binary snapshots;
- payloads of at least 2,048 bytes may be classified as nested archives when at least two plausible ILT packets are found.

These thresholds are parser classifications, not fields or limits declared by the replay format.

## Middle region

The project decomposes recognized portions of the middle region into a header, segments, and gap containers. A gap container is modeled as a `u32` size followed by candidate LT payload bytes. `MiddleBlobCoverage` reports the ratio of bytes accounted for by the current parser; it is a structural coverage metric, not semantic decode coverage.

## Timelines

| Property | Project-defined contents |
|---|---|
| `UnifiedTimeline` | Parsed middle, spectating, and game packets sorted by timestamp and source |
| `ExpandedUnifiedTimeline` | `UnifiedTimeline` plus packets heuristically extracted from binary-snapshot bodies |
| `DeduplicatedTimeline` | `UnifiedTimeline` filtered by timestamp, message ID, source, payload length, and a sampled payload fingerprint |
| `ExpandedDeduplicatedTimeline` | The expanded timeline filtered by the same key |

Sources are tagged with `PacketTimelineSource`, including `MiddleBlob`, `SpectatingArchive`, `GameArchive`, and `BinarySnapshotBody`. Deduplication is intentionally project-defined and does not establish semantic identity.

## `LegacyV2022` classification

The reader selects this layout when the first two words are `6` and `1020107`:

```text
u32 version = 6
u32 observedMagic = 1020107
u32 headerLength
byte[headerLength] header
u32 spectatingRoomInfoLength
byte[spectatingRoomInfoLength] spectatingRoomInfo
counted spectating timestamp section
counted game timestamp section
counted special-effect section
```

The common header size represented by the project is 512 bytes. Brotli and AES belong to the outer container and are not part of this inner layout.

## ILT decoding

`LtMessageReader.TryDecode` provides typed decoding for a subset of IDs in `EMessageIdCatalog`. Unsupported valid IDs may produce `LtUnknownDecoded`; malformed or unrecognized data may remain undecoded. Catalog presence is not equivalent to semantic decoder support.

Relevant implementation paths include:

- `src/CrossFire.Replay/Formats/PacketSimulator/PacketSimulatorPayloadReader.cs`
- `src/CrossFire.Replay/Formats/PacketSimulator/PacketSimulatorModernReader.cs`
- `src/CrossFire.Replay/Formats/PacketSimulator/PacketSimulatorModernWriter.cs`
- `src/CrossFire.Replay/Formats/PacketSimulator/IltPacketArchiveReader.cs`
- `src/CrossFire.Replay/Protocol/Lt/LtMessageReader.cs`

## Writing and validation

`ReplayWriteService` can write either PacketSimulator classification as a raw inner payload or place it in a `.cfo`/`.cfn` wrapper. For parsed modern documents, `InnerPayload` is retained and takes precedence, providing a byte-preserving path. Reconstruction from raw parts or decomposed models is also implemented and covered by focused tests.

Those tests show properties such as byte equality, packet-count preservation, or successful re-reading by this library. They do not establish complete semantic correctness, and the repository does not demonstrate that a game client accepts synthesized or modified output.
