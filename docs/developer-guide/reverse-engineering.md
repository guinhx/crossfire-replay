# Metodologia de engenharia reversa

Este projeto documenta formatos de replay **sem acesso a materiais internos** da Smilegate ou parceiros. O conhecimento vem de prática acumulada em RE de clients Windows e validação empírica.

## Ferramentas

| Ferramenta | Uso neste projeto |
|------------|-------------------|
| **ImHex** | Padrões hex, estruturas provisórias, diff entre replays |
| **dotnet test** | Round-trip e regressão |
| **IltMessageScan** | Cobertura de decode por `EMessageId` |
| **ProbeCfn / scripts Python** | Hipóteses de Brotli/AES (dev local) |

## Ciclo de validação

```
Hipótese (ImHex / notas) → Implementação C# → Teste sintético
        ↓
Fixture .cfn real (local) → IltMessageScan → ajuste
        ↓
Round-trip writer (quando existe) → bytes idênticos
```

Uma hipótese só é considerada **aceita** quando pelo menos um teste automatizado a cobre. Documentação em `docs/formats/` segue o código, não o contrário.

## O que copiamos vs o que reimplementamos

| Fonte | Uso permitido no repo |
|-------|----------------------|
| Algoritmos públicos (ex. Lithtech `CompVector` em SDK aberto) | Reimplementação com crédito em CREDITS.md |
| Strings/símbolos de mensagens (nomes públicos em binários) | Catálogo gerado; não copiamos código proprietário |
| Replays do jogador | Testes locais; **não** commitados |

## ImHex — dicas práticas

1. Comece pelo magic/container (primeiros 16–32 B de `.cfn`).
2. Após decompressão mental/on-disk, procure `0x7FFFFFFE` (modern) ou `6` + `1020107` (legacy).
3. ILT: primeiros 16 bits do payload costumam ser `EMessageId` little-endian bitstream.
4. Compare dois replays da mesma mapa para campos estáveis vs ruído.

## Limitações honestas

- IDs ILT raros de modos PvE/evento podem aparecer só em partidas específicas.
- `ReadCompPos` sem bounds do mapa usa heurística (`±4096`).
- Versões futuras do client podem alterar middle blob — issues com fixture são bem-vindas.

## Citar este trabalho

Mantenha [CREDITS.md](../../CREDITS.md) visível. Sugestão:

> Format research: CrossFire Replay Toolkit — independent RE (ImHex, empirical validation). Not affiliated with Smilegate.
