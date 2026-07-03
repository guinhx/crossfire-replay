# Documentação

Índice da documentação do **CrossFire Replay Toolkit**.

## Começando

| Documento | Descrição |
|-----------|-----------|
| [quickstart.md](getting-started/quickstart.md) | Build, testes, CLI, exemplos de API |
| [dotnet-api.md](getting-started/dotnet-api.md) | Tipos principais, fluxo de leitura/escrita |

## Formatos de arquivo

| Documento | Descrição |
|-----------|-----------|
| [overview.md](formats/overview.md) | `.cfr`, `.cfn`, `.cfo` e containers |
| [cfr-simple-protocol.md](formats/cfr-simple-protocol.md) | Stream `cfrversion` + mensagens SimpleProtocol |
| [cfn-packet-simulator.md](formats/cfn-packet-simulator.md) | Layout moderno/legacy, ILT, middle blob |

## Desenvolvimento

| Documento | Descrição |
|-----------|-----------|
| [architecture.md](developer-guide/architecture.md) | Camadas, namespaces, extensão |
| [reverse-engineering.md](developer-guide/reverse-engineering.md) | Como validamos layouts (ImHex, round-trip) |
| [contributing.md](developer-guide/contributing.md) | PRs, issues, convenções |
| [project-status.md](project-status.md) | Limitações, uso comercial, roadmap |
| [fixtures.md](getting-started/fixtures.md) | Replays locais para testes de integração |

## Legal

- [LICENSE](../LICENSE)
- [CREDITS](../CREDITS.md)

## Estado do projeto

| Área | Status |
|------|--------|
| Leitura `.cfr` SimpleProtocol | Estável para mensagens implementadas |
| Container Brotli / AES `.cfn` | Round-trip nos testes |
| PacketSimulator moderno (2026) | Layout + timeline + ILT parcial |
| Decoders ILT semânticos | Em expansão — ver `coverage` na CLI |
| Analytics / export coach | Fora de escopo atual |

Detalhes: [project-status.md](project-status.md). Para lacunas de decode, abra uma issue com o template **Decode / format gap**.
