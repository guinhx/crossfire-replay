# CFN — PacketSimulator (inner)

Layout observado em replays `CFReplay20260701_*.cfn` (cliente 2026).

## Assinatura moderna

```
u32 signature = 0x7FFFFFFE
u32 metadataSize   (tipicamente 1024)
u32 extraSize      (tipicamente 4)
[metadata block]
[extra block]
[descriptor + labels]
[middle blob]
[spectating archive][game archive][special FX archive]  @ fim do arquivo
```

Constantes: `PacketSimulatorLayout` em código.

## Metadata (1024 B)

- Primeiros 512 B: header de partida (`PacketSimulatorHeaderBlock`) — mapId, goal, clan, etc.
- Tail 512 B: extensão (majoritariamente zeros nos fixtures atuais)
- Room info **não** está necessariamente no prefixo 0/512 — fontes em `ModernDescriptor` + `MiddleBlobHeader`

## Archives ILT

Cada archive: stream de pacotes timestampados

```
repeat:
  u32 payloadSize
  u32 timestamp
  u8[payloadSize]   // bitstream ILT, muitas vezes com messageId 16-bit no início
```

Thresholds:

- `>= 65_536` B → binary snapshot (`BinarySnapshotReader`)
- nested mini-archives em payloads grandes

## Middle blob

Segmentos + gap containers (`u32 size` + payload LT).  
Métricas: `MiddleBlobCoverage`, `MiddleBlobGapContainers`.

## Timeline

| Propriedade | Conteúdo |
|-------------|----------|
| `UnifiedTimeline` | middle + spectating + game |
| `ExpandedUnifiedTimeline` | + ILT extraído de binary snapshots |
| `DeduplicatedTimeline` | dedup por fingerprint |

Fontes tagueadas: `PacketTimelineSource` (MiddleBlob, SpectatingArchive, GameArchive, BinarySnapshotBody).

## Legacy (2022)

```
u32(6)
u32(1020107)
u32 headerLen
header 512 B
u32 spectatingRoomSize
room-info metadata prefix
archives ILT + Brotli...
```

Detectado como `PacketSimulatorInnerFormat.LegacyV2022`.

## ILT decode

`LtMessageReader.TryDecode` — catálogo até `0x082C` (`EMessageIdCatalog.Generated.cs`).

## Round-trip

Writers modernos permitem síntese sem middle blob raw quando layout decomposto está preenchido (`PacketSimulatorModernWriter`, testes em `ModernSynthesisTests`).
