---
name: Bug report
about: Parse error, crash, round-trip quebrado ou teste falhando
title: "[bug] "
labels: bug
assignees: ''
---

## Descrição

<!-- O que aconteceu vs o que você esperava -->

## Reprodução

1. Comando ou código:
   ```bash
   dotnet run --project samples/CrossFire.Replay.Cli -- "caminho\arquivo.cfn"
   ```
2. ...

## Ambiente

- OS:
- .NET SDK (`dotnet --version`):
- Commit / versão do repo:

## Arquivo (metadados apenas)

- Extensão: `.cfr` / `.cfn` / `.cfo`
- Tamanho em bytes:
- **Não** cole o arquivo inteiro se for privado; hex dos primeiros 64 B é suficiente:
  ```
  paste aqui
  ```

## Saída / erro

```
cole ReplayParseException, stack trace ou saída do CLI
```

## `--inspect` (se aplicável)

```
cole saída de: Cli arquivo --inspect
```

## Checklist

- [ ] Rodei `dotnet test CrossFire.Replay.sln` na minha máquina
- [ ] Confirmei que o arquivo abre no client do jogo (não corrompido)
- [ ] Li [docs/developer-guide/contributing.md](../blob/main/docs/developer-guide/contributing.md)
