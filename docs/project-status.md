# Status do projeto e limitações conhecidas

Este documento descreve honestamente o que o **CrossFire Replay Toolkit** oferece hoje e o que **não** está coberto — útil para equipes que avaliam uso comercial, APIs ou pipelines.

## O que está maduro

| Área | Status |
|------|--------|
| Leitura/escrita `.cfr` SimpleProtocol | Estável para mensagens implementadas |
| Container Brotli / AES (`.cfn` / `.cfo`) | Round-trip nos testes sintéticos |
| PacketSimulator moderno (2026) | Layout, metadata, middle blob, timeline |
| Round-trip byte-a-byte | Foco principal do projeto |
| Testes unitários sintéticos | Rodam em CI sem fixtures externas |
| Decoders ILT nativos (prioritários) | Ver [protocol/ilt-native-codecs.md](protocol/ilt-native-codecs.md) |

## Limitações conhecidas

### 1. Cobertura ILT depende do replay

O catálogo de IDs (`EMessageIdCatalog`) cobre milhares de símbolos; **decoders semânticos** existem para dezenas de tipos prioritários (dano, posição, armas, round, score, modos especiais, etc.), alinhados ao wire nativo quando possível — ver [protocol/ilt-native-codecs.md](protocol/ilt-native-codecs.md).

A taxa de decode **varia por replay**: no fixture de referência usado nos testes de integração, a cobertura semântica chega a **~100%** dos pacotes ILT; em outros mapas, modos ou builds, pacotes ainda viram `LtUnknownDecoded`.

**Impacto:** consumo semântico amplo exige validar com replays reais da sua região e expandir decoders conforme necessário, ou consumir payloads brutos.

**Como medir hoje:**

```bash
dotnet run --project samples/CrossFire.Replay.Cli -- coverage replay.cfn
```

```csharp
using CrossFire.Replay.Protocol.Lt;

var report = IltDecodeCoverage.Analyze(packetSimulatorDocument);
Console.WriteLine($"{report.SemanticPacketRatio:P1} packets semantic");
```

Abra issues com template **Decode / format gap** para IDs frequentes sem decoder.

### 2. Formato não oficial

O toolkit é resultado de engenharia reversa independente. **Não há afiliação com Smilegate** nem especificação oficial.

**Impacto:** patches do cliente, regiões ou builds diferentes podem alterar layouts silenciosamente. Não há garantia de compatibilidade retroativa com todas as versões.

**Mitigação recomendada para produção:**

- Fixar versão do cliente usado na captura
- Testar replays reais da sua região antes de deploy
- Tratar `ReplayParseException` e `LtUnknownDecoded` como caminhos normais, não exceções raras

### 3. Escopo “formato primeiro”

O projeto **não** é um produto SaaS nem um SDK enterprise completo. Fora de escopo atual:

| Não incluído | Notas |
|--------------|-------|
| SLA / suporte comercial | Comunidade + issues no GitHub |
| Observabilidade (métricas, tracing) | Responsabilidade da aplicação consumidora |
| Versionamento estável de API pública | Pré-1.0: breaking changes possíveis |
| Pacote NuGet publicado oficialmente | `dotnet pack` local disponível; feed público TBD |
| Camada de produto | Auth, rate limit, filas, schema de export estável para terceiros |

**Versionamento:** `ReplayToolkitVersion.Current` (semver pré-1.0). Export JSON da timeline inclui `schemaVersion` — incrementado apenas em mudanças breaking do JSON.

### 4. Testes dependem de fixtures locais

Testes de integração com `.cfn` reais **não rodam** se você não tiver replays na máquina. Eles fazem skip silencioso (não falham o build).

**Variáveis de ambiente:**

| Variável | Uso |
|----------|-----|
| `CROSSFIRE_REPLAY_FIXTURE_CFN` | Caminho para um `.cfn` moderno de referência |
| `CROSSFIRE_REPLAY_FIXTURE_FOLDER` | Pasta com `*.cfn` para testes parametrizados |

Detalhes: [getting-started/fixtures.md](getting-started/fixtures.md).

## Roadmap sugerido (não comprometido)

1. Expandir decoders ILT por frequência (`coverage` + issues da comunidade)
2. Publicar NuGet alpha quando API estabilizar
3. Congelar `schemaVersion` do export de timeline após feedback
4. Adicionar matriz de compatibilidade por build de cliente (contribuições bem-vindas)

## Para uso comercial

Viável como **biblioteca fundacional** se a sua org:

- Aceita manter decoders e validar contra replays próprios
- Não exige garantia oficial de formato
- Envolve engenharia para API/pipeline, observabilidade e contratos de export

Não recomendado como **plug-and-play** sem equipe técnica por trás.
