# Arquitetura

## Camadas

```
┌─────────────────────────────────────────┐
│  CLI / seu app                          │
├─────────────────────────────────────────┤
│  Core: ReplayService, ReplayWriteService│
├─────────────────────────────────────────┤
│  Compression: AES, Brotli, checksum     │
├─────────────────────────────────────────┤
│  Formats: SimpleProtocol, PacketSimulator│
├─────────────────────────────────────────┤
│  Protocol: Lt, Mm, SimpleProtocol IDs   │
└─────────────────────────────────────────┘
```

## Namespaces

| Namespace | Responsabilidade |
|-----------|------------------|
| `CrossFire.Replay.Core` | Orquestração read/write |
| `CrossFire.Replay.Compression` | Containers `.cfn`/`.cfo` |
| `CrossFire.Replay.Formats.SimpleProtocol` | CFR |
| `CrossFire.Replay.Formats.PacketSimulator` | CFN inner, timeline, snapshots |
| `CrossFire.Replay.Protocol.Lt` | Bitstream ILT, decoders |
| `CrossFire.Replay.Protocol.Mm` | Room info / metadata MM |
| `CrossFire.Replay.Abstractions` | Interfaces e enums compartilhados |
| `CrossFire.Replay.IO` | `CfrBinaryReader` etc. |

## Fluxo de leitura

1. `ReplayService` testa CFR plain → senão `ReplayContainerDecoder`.
2. Payload inner passa por `IReplayPayloadReader` registrados (ordem: SimpleProtocol, PacketSimulator).
3. `PacketSimulatorPayloadReader` detecta legacy vs modern (`PacketSimulatorModernReader`).
4. Pacotes passam por `PacketSimulatorPacketEnricher` (decode ILT, nested expand, tags).

## Princípios de design

1. **Formato primeiro** — round-trip byte-a-byte como base estável para consumo downstream.
2. **Decode tolerante** — `LtUnknownDecoded` + catálogo de nomes nativos.
3. **Testes com fixtures reais opcionais** — não commitar replays do usuário.
4. **Sem dependência de código proprietário** no repo distribuível.

## Adicionar decoder ILT

1. Confirmar layout com ImHex e notas RE (ver [reverse-engineering.md](reverse-engineering.md)).
2. Adicionar record em `LtMessages.cs`.
3. Implementar em `LtSemanticDecoders` ou `LtCsSemanticDecoders`.
4. Registrar em `LtMessageReader.TryDecode` switch.
5. Teste unitário + opcional fixture integration.

## Adicionar mensagem SimpleProtocol

1. Entrada em `SimpleProtocolId`.
2. Handler em `ProtocolDeserializer.Handlers.cs`.
3. Teste round-trip CFR.

## Projetos na solution

| Projeto | Tipo |
|---------|------|
| `CrossFire.Replay` | biblioteca |
| `CrossFire.Replay.Tests` | xUnit |
| `CrossFire.Replay.Cli` | sample exe |
