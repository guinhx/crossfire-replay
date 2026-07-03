# Fixtures de teste (replays locais)

Os testes de integração usam arquivos `.cfn` **reais** que **não** são commitados no repositório (privacidade + tamanho).

## Comportamento padrão

- Testes **sintéticos** (round-trip, decoders unitários) rodam sempre — 182+ testes sem arquivos externos.
- Testes marcados `UserCfn_*` ou com `[MemberData]` de fixtures **retornam cedo** se nenhum replay estiver disponível — o CI passa, mas a cobertura real depende da sua máquina.

## Configurar fixtures

### Um arquivo de referência

```powershell
set CROSSFIRE_REPLAY_FIXTURE_CFN=C:\Replays\CFReplay20260701_0000.cfn
dotnet test CrossFire.Replay.sln
```

### Pasta inteira de replays

```powershell
set CROSSFIRE_REPLAY_FIXTURE_FOLDER=C:\Users\%USERNAME%\Documents\Cross Fire\Replay
dotnet test CrossFire.Replay.sln
```

Sem variáveis, o runner tenta o caminho padrão do Windows:

`%USERPROFILE%\Documents\Cross Fire\Replay\CFReplay20260701_0000.cfn`

## O que validar localmente

Após configurar fixtures, estes testes passam de skip para execução real:

- Parse do layout moderno 2026
- Round-trip do inner payload
- Middle blob e binary snapshots
- Export de timeline JSON/CSV
- Decoders semânticos prioritários no replay real

## CI / orgs

Para pipelines que precisam de cobertura real:

1. Armazene replays em storage privado (não no git)
2. Injete `CROSSFIRE_REPLAY_FIXTURE_CFN` ou `CROSSFIRE_REPLAY_FIXTURE_FOLDER` no job
3. Opcional: falhe o job se a variável estiver ausente (política da sua org — o repo não força isso)

## Privacidade

Replays podem conter nicknames, IDs e metadados de partida. Não envie arquivos completos em issues públicas — use trechos hex do payload conforme o template **Decode / format gap**.
