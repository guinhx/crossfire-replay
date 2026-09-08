# ILT Codecs Based on Observed Layouts

The ILT implementation reads and writes an LSB-first bitstream beginning with a 16-bit message ID. Message names and field layouts in this document describe the repository's current interpretation of tested replay data. They are not official protocol definitions, and similar names do not establish equivalence with every live network or client implementation.

The repository does not contain `references/cshell/` or decompiled client sources. Contributors must base changes on material they are legally permitted to use and must provide reproducible tests without adding proprietary source or dumps.

## Components

| Type | Responsibility |
|------|----------------|
| `LtBitstreamReader` / `LtBitstreamWriter` | LSB-first bit I/O and the 16-bit message ID |
| `LtCsPacketReader` | Parsing an observed CS packet header and optional hash trailer |
| `LtCompressedVectorCodec` | Compressed vector and world-position representations |
| `LtNativeSerializers` | Read/write implementations for selected messages |
| `LtMessageReader` | Message-ID plausibility checks and semantic decoder routing |
| `LtMessageWriter` | Encoder routing for supported decoded message types |
| `IltDecodeCoverage` | Per-replay semantic, unknown, and undecoded counts |

These types are under `src/CrossFire.Replay/Protocol/Lt/`.

## Codec Rules

1. Read message bodies through `LtBitstreamReader`; byte alignment after the message ID must be demonstrated rather than assumed.
2. Add bounds and plausibility checks for variable-length or ambiguous data. `LtMessageReader` uses message-specific size limits to reduce false-positive ID matches, but those limits are heuristics rather than proof of validity.
3. For bundled archival payloads, decode entries sequentially and advance by the measured number of consumed bits or bytes. Do not search arbitrary payload bytes for values that resemble message IDs.
4. Test decoding against known bytes and assert individual fields. If encoding is implemented, also test encode/decode behavior and byte preservation for representative values.
5. Do not describe a codec as complete or native-compatible solely because its encoder output can be decoded by the same implementation.

## Implemented Layout Interpretations

The following summaries correspond to methods currently implemented in `LtNativeSerializers`. Field widths and meanings remain subject to revision when additional evidence is available.

| Repository message name | Current interpretation |
|-------------------------|------------------------|
| `MsgScDamageSite` | Three 32-bit floating-point dimensions, a guarded 32-bit damage-site type, and a Boolean render flag |
| `MsgScDamageSiteState` | A 16-bit object ID and a Boolean enabled flag |
| `MsgScAiScore` | An 8-bit bot index followed by five 16-bit score-related values |
| `MsgCsRappelVelAndRot` | An 8-bit area index, a character index carried in a 16-bit field, compressed velocity and position, and a 32-bit floating-point ratio |
| `MsgScForceLeavePollStart` | A string-based form and an archival entry form are handled as distinct observed representations |
| `MsgScArcadiaCoreSwitchState` | Compact fields and an observed 140-byte archival form are supported; the archival decoder includes reserved, context, compressed-vector, mask, and tail fields |
| `MsgScDamageCalculationRequest` | Attacker and weapon data, vectors and rotation values, state fields, and 16 third-party positions |

Consult `LtNativeSerializers.cs`, `LtMessages.cs`, and tests in `tests/CrossFire.Replay.Tests/LtScReplayDecoderTests.cs` for the executable definition of current behavior. Symbolic names are retained for code navigation; they do not imply that the layout was supplied by a rights holder.

## Evaluation Limits

- Compressed world positions use default bounds of `-4096` to `4096` when map-specific bounds are unavailable. Values derived under those defaults should be treated as approximate.
- The observed 140-byte Arcadia archival form is tested. Longer payloads, including 255-byte bundles that may contain additional records, require independent boundary analysis before semantic interpretation.
- Decoder coverage varies by replay. Run `dotnet run --project samples/CrossFire.Replay.Cli -- coverage replay.cfn` on a representative private corpus.
- Round-trip equality applies only to the inputs and branches exercised by the test. It does not prove semantic accuracy or compatibility with untested client builds.
