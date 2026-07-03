# Quickstart

## Pré-requisitos

- .NET 8 SDK
- (Opcional) Replays reais em `%USERPROFILE%\Documents\Cross Fire\Replay\` para testes de integração locais

## Build e testes

```bash
dotnet restore CrossFire.Replay.sln
dotnet build CrossFire.Replay.sln -c Release
dotnet test CrossFire.Replay.sln -c Release
```

Testes de integração usam fixtures locais — configure `CROSSFIRE_REPLAY_FIXTURE_CFN` ou veja [fixtures.md](fixtures.md). Testes sintéticos rodam sem arquivos externos.

## CLI

Projeto: `samples/CrossFire.Replay.Cli`

| Comando | Efeito |
|---------|--------|
| `read replay.cfn` | Resumo do documento parseado |
| `read replay.cfn --dump` | Amostra de mensagens / pacotes |
| `inspect replay.cfn` | Metadados de container + structs de layout |
| `inspect replay.cfn --hex` | Idem + preview hex |
| `coverage replay.cfn` | Relatório de cobertura ILT semântica |
| `export-timeline replay.cfn out.json` | Export JSON da timeline ILT (schema v1) |
| `export-timeline replay.cfn out.csv` | Export CSV |
| `self-test` | Round-trip CFR + containers CFO/CFN |

Sintaxe legada ainda aceita: `replay.cfn --inspect`, `replay.cfn --export-timeline out.json`.

### Exemplo de saída (`.cfn` moderno)

```
Format: PacketSimulator
Inner format: ModernV2026
Unified timeline: 12450 packets
Deduplicated timeline: 11800 packets
Binary snapshots: 12
```

## Uso em código

### Ler qualquer replay suportado

```csharp
using CrossFire.Replay.Core;
using CrossFire.Replay;

var service = ReplayService.Default;

// Por caminho
var doc = service.Read(@"D:\Replays\CFReplay20260701_0000.cfn");

// Por bytes
var bytes = await File.ReadAllBytesAsync(path);
var doc2 = service.Read(bytes, path);
```

### Inspecionar container sem parse completo

```csharp
var info = ReplayService.Default.Inspect(path);
Console.WriteLine(info.ContainerKind);           // EncryptedBrotliWrapper para .cfn
Console.WriteLine(info.PacketSimulatorInnerFormat);
```

### CFR (SimpleProtocol)

```csharp
using CrossFire.Replay.Formats.SimpleProtocol;

if (doc is CfrDocument cfr)
{
    foreach (var msg in cfr.Messages)
        Console.WriteLine($"{msg.Timestamp} {msg.MessageId}");
}
```

### CFN (PacketSimulator + ILT)

```csharp
using CrossFire.Replay.Formats.PacketSimulator;
using CrossFire.Replay.Protocol.Lt;

if (doc is PacketSimulatorReplayDocument ps)
{
    foreach (var pkt in ps.UnifiedTimeline.Take(20))
    {
        if (pkt.Decoded is LtDamageDecoded dmg)
            Console.WriteLine($"dmg={dmg.Damage} from={dmg.AttackerIndex}");
    }

    // Timeline com pacotes embutidos em binary snapshots
    Console.WriteLine(ps.ExpandedUnifiedTimeline.Count);
}
```

### Exportar timeline

```csharp
PacketSimulatorTimelineExporter.WriteJson("timeline.json", ps);
PacketSimulatorTimelineExporter.WriteCsv("timeline.csv", ps);
```

### Escrever CFR / containers

```csharp
using CrossFire.Replay.Abstractions;
using CrossFire.Replay.Core;

var cfr = CfrReader.ReadFile("input.cfr");
CfrWriter.WriteFile("copy.cfr", cfr, prependChecksum: true);

var wrapped = ReplayWriteService.Default.Write(
    cfr,
    ReplayWriteOptions.CfnWrapper);
await File.WriteAllBytesAsync("out.cfn", wrapped);
```

## Referenciar a biblioteca

Adicione referência ao projeto:

```xml
<ProjectReference Include="..\src\CrossFire.Replay\CrossFire.Replay.csproj" />
```

Ou empacote localmente:

```bash
dotnet pack src/CrossFire.Replay/CrossFire.Replay.csproj -c Release
```

## Próximos passos

- [dotnet-api.md](dotnet-api.md) — referência de tipos
- [../formats/overview.md](../formats/overview.md) — mapa de formatos
