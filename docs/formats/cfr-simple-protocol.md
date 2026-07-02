# CFR — SimpleProtocol

Arquivo texto-binário iniciando com `cfrversion` seguido de flags de feature e mensagens sequenciais.

## Header

```
"cfrversion" (10 bytes ASCII)
u32 fileVersion
u32 featureFlagCount
repeat featureFlagCount:
    u32 nameLen
    bytes name (ASCII)
```

Feature flags controlam campos opcionais em mensagens (ex.: clan refine, versões de protocolo).

## Mensagens

Cada mensagem:

```
u8  messageId   (SimpleProtocolId)
u8  protocolVersion (se fileVersion >= 5)
u32 timestamp
... body (depende do id)
```

Alguns arquivos têm **checksum MD5** de 16 bytes prefixando o bloco acima (`CfrReader` valida quando presente).

## API

```csharp
var doc = CfrReader.ReadFile("match.cfr");
// ou após extrair container:
var doc2 = SimpleProtocolReader.ReadPayload(payloadBytes);
```

Mensagens tipadas: `MapInfoMessage`, `RoundStartMessage`, `PlayerDieMessage`, etc.  
Fallback: `GenericReplayMessage` com `Fields` dictionary.

## Escrita

```csharp
CfrWriter.WriteFile("out.cfr", doc);
CfrWriter.WriteFile("out.cfr", doc, prependChecksum: true);
```

## Relação com `.cfn`

Um `.cfn` descomprimido **pode** conter inner legacy que embute CFR-like data, mas o fluxo principal moderno é PacketSimulator puro. Use `ReplayService.Default.Read` para auto-detectar.

## Implementação

| Área | Caminho |
|------|---------|
| IDs | `Protocol/SimpleProtocolId.cs` |
| Deserializer | `Protocol/ProtocolDeserializer*.cs` |
| Reader/Writer | `Formats/SimpleProtocol/` |

Handlers são adicionados manualmente em `ProtocolDeserializer.Handlers.cs`.
