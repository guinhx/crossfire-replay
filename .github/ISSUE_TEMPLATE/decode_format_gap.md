---
name: Decode / format gap
about: ID ILT ou layout não decodificado
title: "[decode] "
labels: format, decode
assignees: ''
---

## Mensagem / layout

- **ID**: `MSG_...` / `0x____` / enum `EMessageId`
- **Frequência** (se mediu em replays): known=___ decoded=___
- **Formato de arquivo**: `.cfn` moderno / legacy / `.cfr`

## Contexto de partida

- Mapa / modo (se souber):
- Cliente / data aproximada do replay:

## Evidência de wire layout

Hex do payload (após message id 16-bit), primeiros 64–128 B:

```
0000: ...
```

Opcional: notas ImHex (offsets, campos prováveis).

## Comportamento atual

- [ ] `LtUnknownDecoded`
- [ ] `ReplayParseException` (offset: )
- [ ] Decode parcial (descreva):

## Referência nativa (opcional)

Se você já mapeou o layout por RE (ImHex, etc.), descreva ordem de campos e tamanhos — **sem** colar trechos longos de código proprietário.

## Checklist

- [ ] Verifiquei que o ID está em `EMessageIdCatalog`
- [ ] Estou disposto a compartilhar **trecho hex** (não o replay completo) se solicitado

## Impacto

<!-- Por que esse decode importa para você: stats, timeline, ferramenta X -->
