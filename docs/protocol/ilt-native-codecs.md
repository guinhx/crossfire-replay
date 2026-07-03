# ILT native codecs

Replay ILT read/write follows the same bitstream rules as CShell `GAMEPROTO_*::ReadData` / `WriteData`. Reference decompiled layouts live under `references/cshell/` (read-only); implementation stays in this repo only.

## Layering

| Layer | Responsibility |
|-------|------------------|
| `LtBitstreamReader` / `LtBitstreamWriter` | LSB-first Lithtech bit I/O |
| `LtCsPacketReader` | CS packet header (5-bit dummy + 8-bit seq) and optional hash trailer |
| `LtCompressedVectorCodec` | `ReadCompLtVector` / `ReadCompWorldPos` |
| `LtNativeSerializers` | Faithful per-message encode/decode (DamageSite, AiScore, Rappel, …) |
| `LtMessageReader` / `LtMessageWriter` | Routing, peek validation, round-trip API |

## Rules

1. **Always bitstream after the 16-bit message id.** Do not use `BitConverter` or fixed byte offsets for message bodies.
2. **Port from decompiled `ReadData`** in `references/cshell/1.1 - CShell_x64_part02.dll.cpp` (or other CShell dumps in `references/cshell/`).
3. **Concatenated replay payloads** (several ILT messages in one archive slice) are decoded sequentially: read one native message, advance by consumed bits/bytes, repeat. Do not scan for message-id byte pairs inside the payload.
4. **Round-trip tests** (`LtNativeSerializers.Encode*` + `TryDecode*`) are the acceptance check for new codecs.
5. **Peek limits** reject false positives (oversized blobs that happen to start with a valid id).

## Reference struct layouts (CShell)

- `GAMEPROTO_SC_DAMAGESITE`: `TVector3 vDimension` (96 bits) + `int nDamageSiteType` + `bool bRenderEffect`
- `GAMEPROTO_SC_DAMAGESITE_STATE`: `LTObject hObject` (16 bits) + `bool bIsOn`
- `GAMEPROTO_SC_AI_SCORE`: `char tBotIndex` (8) + five `uint16` score fields
- `GAMEPROTO_SC_RAPPEL_VELANDROT`: `uint8 byAreaIndex` + `int16 tCharIndex` + compressed vel/pos + `float fRatio`
- `GAMEPROTO_SC_FORCELEAVE_POLLSTART`: `ReadString(13)` requester + `ReadString(13)` target + `uint8` reason (live wire); replay bundles may use `uint32`×3 archival entries instead
- `MSG_SC_ARCADIA_CORE_SWITCHSTATE` live wire (`OnCoreSwitchState`): `uint32` switch slot + `int32` core table key + `uint32` CurHP
- `MSG_SC_ARCADIA_CORE_SWITCHSTATE` replay archival (140 B): reserved header + sentinel (`0xFFFFFFF8`, `7`) + 33 B context + CompLtVector + CompWorldPos + guard/padding/validMask + tail (`coreObjectId`, padding, `curHp`, `eventKind`, `subState`, timestamp, relatedObjectId)

- `GAMEPROTO_SC_DAMAGE_CALCULATION_REQUEST`: attacker, shot position, weapon type, gun rotation quat, many float/int fields, `TVector3 a3rdPlayerPos[16]`

## Remaining work

- Arcadia replay payloads longer than 140 B (chained extension records in 255 B bundle)
