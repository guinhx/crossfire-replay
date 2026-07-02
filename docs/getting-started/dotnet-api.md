# API .NET — referência rápida

## Ponto de entrada

| Tipo | Uso |
|------|-----|
| `ReplayService.Default` | Detecta container, despacha para reader de payload |
| `ReplayWriteService.Default` | Empacota CFR em `.cfo` / `.cfn` |
| `CfrReader` / `CfrWriter` | CFR plain ou com checksum MD5 |
| `SimpleProtocolReader` | Parse de payload CFR já extraído |
| `PacketSimulatorPayloadReader` | Parse de inner PacketSimulator |

## Documentos (`IReplayDocument`)

| Implementação | `FormatKind` | Origem típica |
|---------------|--------------|---------------|
| `CfrDocument` | `SimpleProtocol` | `.cfr` |
| `PacketSimulatorReplayDocument` | `PacketSimulator` | inner de `.cfn` / `.cfo` |

## Containers (`ReplayContainerKind`)

| Kind | Arquivo | Pipeline |
|------|---------|----------|
| `None` | `.cfr` plain | bytes = payload |
| `BrotliWrapper` | `.cfo` | `u32 kind` + `u32 size` + Brotli |
| `EncryptedBrotliWrapper` | `.cfn` | header AES + `CF_ConDeV` + Brotli |

Decodificação: `ReplayContainerDecoder`.

## PacketSimulator — propriedades úteis

```csharp
PacketSimulatorReplayDocument ps = ...;

ps.InnerFormat              // LegacyV2022 | ModernV2026
ps.ModernDescriptor         // tamanhos de archives, labels
ps.UnifiedTimeline          // middle + spectating + game (ordenado)
ps.ExpandedUnifiedTimeline  // + pacotes de binary snapshot bodies
ps.DeduplicatedTimeline
ps.BinarySnapshots
ps.MiddleBlobCoverage       // % do middle blob interpretado
ps.ModernMetadata           // bloco 1024 B parseado
```

## ILT — decode

```csharp
using CrossFire.Replay.Protocol.Lt;

// Peek ID sem decode completo
if (LtMessageReader.TryPeekMessageId(payload, out var id))
    Console.WriteLine(EMessageIdCatalog.GetName((ushort)id));

// Decode semântico (ou LtUnknownDecoded)
var decoded = LtMessageReader.TryDecode(payload);

// Round-trip de mensagens suportadas
var bytes = LtMessageWriter.Encode(new LtRoundStartDecoded(0));
```

Catálogo de IDs: `EMessageIdCatalog` + `EMessageIdCatalog.Generated.cs`.

## Erros

`ReplayParseException` — offset e mensagem quando o layout não bate. A CLI retorna exit code `2`.

## Opções de leitura CFR

`SimpleProtocolReadOptions` — por exemplo `SkipMessageParseErrors` para tolerar mensagens ainda não implementadas em fixtures antigos.

## Extensão

Para novo formato de payload:

1. Implemente `IReplayPayloadReader` (`CanRead`, `Read`).
2. Registre em `ReplayService` (ou instancie seu próprio `ReplayService`).
3. Adicione testes round-trip e fixture real (opcional, skip se ausente).

Veja [architecture.md](../developer-guide/architecture.md).
