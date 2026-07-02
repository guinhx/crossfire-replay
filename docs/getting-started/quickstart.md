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

Muitos testes de integração fazem **skip automático** se o fixture `CFReplay20260701_0000.cfn` não existir no caminho hardcoded do desenvolvedor. Os testes unitários e round-trip sintéticos rodam sem arquivos externos.

## CLI

Projeto: `samples/CrossFire.Replay.Cli`

| Comando | Efeito |
|---------|--------|
| `Cli replay.cfn` | Resumo do documento parseado |
| `Cli replay.cfn --inspect` | Metadados de container (sem parse completo profundo) |
| `Cli replay.cfn --dump` | Amostra de mensagens / pacotes |
| `Cli replay.cfn --export-timeline out.json` | Export JSON da timeline ILT |
| `Cli replay.cfn --export-timeline out.csv` | Export CSV |
| `Cli --self-test` | Round-trip CFR + containers CFO/CFN |

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
