# Contribuindo

Obrigado por ajudar a melhorar leitura de replays CrossFire de forma aberta e ética.

## Antes de abrir PR

1. `dotnet test CrossFire.Replay.sln`
2. Mudanças de formato precisam de teste (unit ou round-trip).
3. Não commitar replays pessoais, chaves, ou dumps proprietários.

## Convenções de código

- C# / .NET 8, nullable enabled.
- Match estilo existente no arquivo (nomes, padrão de records para ILT).
- Comentários só para lógica não óbvia ou referência a layout nativo.
- Escopo mínimo — sem refatoração não relacionada.

## Tipos de contribuição bem-vindos

| Tipo | Onde |
|------|------|
| Decoder ILT semântico | `Protocol/Lt/` |
| Mensagem SimpleProtocol | `Protocol/ProtocolDeserializer*.cs` |
| Layout PacketSimulator | `Formats/PacketSimulator/` |
| Docs / DX | `docs/` |

## Abrir issues — escolha o template

| Template | Quando usar |
|----------|-------------|
| **Bug report** | Crash, parse error, round-trip quebrado, teste falhando |
| **Decode / format gap** | ID ILT conhecido mas não decodificado; layout novo; bytes estranhos |
| **Feature request** | Nova API, export, tooling (fora de escopo analytics ainda ok como discussão) |

### Informações que aceleram o diagnóstico

**Bug / parse**

- Versão do .NET e OS
- Extensão e tamanho do arquivo (não anexe o arquivo inteiro em público se não quiser)
- Trecho hex dos primeiros 64 B **ou** saída de `Cli --inspect`
- Stack trace / mensagem `ReplayParseException`

**Decode gap**

- `EMessageId` ou hex (`0x03`, `MSG_CS_VELANDROT`)
- Contagem aproximada (saída `IltMessageScan`)
- Payload hex dos primeiros 32–64 B após message id
- Mapa / modo de jogo se relevante

**Feature**

- Caso de uso concreto (quem consome, formato de saída desejado)
- Por que não resolve com API atual + `LtUnknownDecoded`

## Processo de PR

1. Fork / branch descritiva (`feat/decode-velandrot`, `fix/middle-blob-gap`).
2. Descreva **por que** (replay real, scan stats, ImHex screenshot opcional).
3. Mantenha LICENSE e CREDITS intactos; não remova atribuição em UI se você empacotar app.

## Código de conduta (resumo)

- Sem distribuição de assets proprietários do jogo.
- Sem afirmar oficialidade Smilegate/CrossFire.
- Créditos do projeto permanecem visíveis em derivados.

## Contato

Use GitHub Issues no repositório do projeto.
