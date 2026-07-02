# CrossFire Replay Toolkit

Biblioteca C# (.NET 8) para **ler e reescrever** replays do CrossFire — `.cfr` (eventos simplificados), `.cfn` / `.cfo` (PacketSimulator com ILT/Lithtech).

Foco atual: **formato primeiro** (parse fiel, round-trip, decoders semânticos). Analytics, coach export e pipelines de produção ficam fora de escopo por enquanto.

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
dotnet run --project samples/CrossFire.Replay.Cli -- "C:\caminho\replay.cfn"
dotnet run --project samples/CrossFire.Replay.Cli -- replay.cfn --inspect
dotnet run --project samples/CrossFire.Replay.Cli -- replay.cfn --dump
dotnet run --project samples/CrossFire.Replay.Cli -- replay.cfn --export-timeline out.json
dotnet run --project samples/CrossFire.Replay.Cli -- --self-test
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

## Documentação

| Tópico | Link |
|--------|------|
| Índice geral | [docs/README.md](docs/README.md) |
| Quickstart & API | [docs/getting-started/quickstart.md](docs/getting-started/quickstart.md) |
| Formatos `.cfr` / `.cfn` | [docs/formats/overview.md](docs/formats/overview.md) |
| Arquitetura | [docs/developer-guide/architecture.md](docs/developer-guide/architecture.md) |
| Metodologia RE | [docs/developer-guide/reverse-engineering.md](docs/developer-guide/reverse-engineering.md) |
| Contribuir & issues | [docs/developer-guide/contributing.md](docs/developer-guide/contributing.md) |

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
