# CrossFire Replay Toolkit

Biblioteca C# (.NET 8) para **ler e reescrever** replays do CrossFire — `.cfr` (eventos simplificados), `.cfn` / `.cfo` (PacketSimulator com ILT/Lithtech).

Foco atual: **formato primeiro** (parse fiel, round-trip, decoders semânticos).

> Trabalho independente de engenharia reversa. Não é oficial Smilegate / CrossFire.  
> Veja [CREDITS.md](CREDITS.md) e [LICENSE](LICENSE) — uso comercial e fork são permitidos **desde que os créditos permaneçam visíveis**.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Início rápido

```bash
git clone <seu-repo>
cd crossfire

dotnet build CrossFire.Replay.sln
dotnet test CrossFire.Replay.sln
```

### CLI de amostra

```bash
dotnet run --project samples/CrossFire.Replay.Cli -- read replay.cfn
dotnet run --project samples/CrossFire.Replay.Cli -- inspect replay.cfn --hex
dotnet run --project samples/CrossFire.Replay.Cli -- coverage replay.cfn
dotnet run --project samples/CrossFire.Replay.Cli -- export-timeline replay.cfn out.json
dotnet run --project samples/CrossFire.Replay.Cli -- self-test
```

### API mínima

```csharp
using CrossFire.Replay.Core;
using CrossFire.Replay.Formats.PacketSimulator;

var doc = ReplayService.Default.Read(@"D:\Replays\partida.cfn");

if (doc is PacketSimulatorReplayDocument ps)
{
    Console.WriteLine(ps.InnerFormat);
    Console.WriteLine($"Timeline: {ps.UnifiedTimeline.Count} pacotes");
    Console.WriteLine($"ILT decodificados: {ps.UnifiedTimeline.Count(p => p.Decoded is not null)}");
}
```

Mais exemplos: [docs/getting-started/quickstart.md](docs/getting-started/quickstart.md).

### Empacotar NuGet (local)

```bash
dotnet pack src/CrossFire.Replay/CrossFire.Replay.csproj -c Release
```

Versão atual: `ReplayToolkitVersion.Current` (pré-1.0, sem garantia de API estável).

## Documentação

> **Disclaimer:** Parte da documentação em `docs/` foi escrita com auxílio de IA, apenas para agilizar o processo e permitir que eu me concentrasse em partes mais importantes do projeto (implementação, testes e validação de formatos). O conteúdo reflete o estado atual do código, mas pode conter imprecisões — correções via issue ou PR são bem-vindas.

| Tópico | Link |
|--------|------|
| Índice geral | [docs/README.md](docs/README.md) |
| Quickstart & API | [docs/getting-started/quickstart.md](docs/getting-started/quickstart.md) |
| Formatos `.cfr` / `.cfn` | [docs/formats/overview.md](docs/formats/overview.md) |
| Arquitetura | [docs/developer-guide/architecture.md](docs/developer-guide/architecture.md) |
| Metodologia RE | [docs/developer-guide/reverse-engineering.md](docs/developer-guide/reverse-engineering.md) |
| Contribuir & issues | [docs/developer-guide/contributing.md](docs/developer-guide/contributing.md) |
| Codecs ILT nativos | [docs/protocol/ilt-native-codecs.md](docs/protocol/ilt-native-codecs.md) |
| **Status e limitações** | [docs/project-status.md](docs/project-status.md) |
| Fixtures de teste | [docs/getting-started/fixtures.md](docs/getting-started/fixtures.md) |

## Limitações conhecidas (resumo)

- **ILT:** cobertura depende do replay — use `coverage` na CLI; detalhes em [project-status.md](docs/project-status.md)
- **Formato não oficial;** pré-1.0, sem SLA/NuGet publicado; testes reais exigem fixtures locais

Detalhes, roadmap e orientação para uso comercial: [docs/project-status.md](docs/project-status.md).

## Estrutura do repositório

```
CrossFire.Replay.sln
src/CrossFire.Replay/          # biblioteca principal
samples/CrossFire.Replay.Cli/  # CLI de inspeção
tests/CrossFire.Replay.Tests/  # testes (incl. fixtures locais opcionais)
docs/                          # documentação
```

## Licença

[LICENSE](LICENSE) — permissiva (uso comercial, fork, modificação) com **atribuição obrigatória e não ocultável**. Leia também [CREDITS.md](CREDITS.md).

## Issues

Use os templates em [`.github/ISSUE_TEMPLATE/`](.github/ISSUE_TEMPLATE/) ou siga o guia em [docs/developer-guide/contributing.md](docs/developer-guide/contributing.md).
