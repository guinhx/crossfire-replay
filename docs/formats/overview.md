# Visão geral dos formatos

CrossFire persiste replays em três extensões principais. Este projeto trata o **bytes on disk** observado em clients recentes, validado por round-trip — não uma spec oficial.

## Mapa rápido

```
.cfr  ──► SimpleProtocol (cfrversion + mensagens serializadas)
.cfo  ──► [Brotli wrapper] ──► inner PacketSimulator OU CFR
.cfn  ──► [AES + Brotli wrapper] ──► inner PacketSimulator (moderno)
```

| Extensão | Container | Payload típico | Classe documento |
|----------|-----------|----------------|------------------|
| `.cfr` | nenhum | SimpleProtocol | `CfrDocument` |
| `.cfo` | Brotli | PacketSimulator legacy ou CFR | `PacketSimulatorReplayDocument` / `CfrDocument` |
| `.cfn` | AES + Brotli | PacketSimulator moderno | `PacketSimulatorReplayDocument` |

## Onde os arquivos vivem no PC

Pasta padrão observada no client (Windows):

```
%USERPROFILE%\Documents\Cross Fire\Replay\
```

## Duas famílias de protocolo

### SimpleProtocol (`.cfr`)

Eventos de partida de alto nível: round start/end, scores, map info, etc.  
Cada mensagem: `id` + `version` + `timestamp` + corpo binário.

→ [cfr-simple-protocol.md](cfr-simple-protocol.md)

### PacketSimulator + ILT (`.cfn` inner)

Gravação de simulação de pacotes com timestamps Lithtech (bitstream ILT). Inclui archives de spectating/game/sfx, middle blob e (no layout moderno) metadata 1024 B.

→ [cfn-packet-simulator.md](cfn-packet-simulator.md)

## Validação

Cada layout documentado deve ter pelo menos um destes:

- teste unitário com bytes sintéticos;
- round-trip writer → reader byte-a-byte;
- teste em fixture `.cfn` real (skip se arquivo ausente).

## Lacunas conhecidas

- Nem todo `EMessageId` ILT tem decoder semântico — IDs desconhecidos viram `LtUnknownDecoded` com nome do catálogo.
- Alguns pacotes CS (`VelAndRot`, `SetWeaponSlot` curto) têm variantes de tamanho ainda em investigação.
- World position decode (`ReadCompPos`) usa bounds padrão quando o mapa não fornece min/max.

Reporte gaps com o issue template **Decode / format gap**.
