---
name: Decode / format gap
about: Report an unknown ILT message ID, partial decode, or unsupported layout
title: "[decode] "
labels: format, decode
assignees: ''
---

## Summary

<!-- Describe the missing or incorrect decode and what you expected to learn from it. -->

## Message or layout

- Message ID: `MSG_...` / `0x____` / `EMessageId` member
- Replay format: modern `.cfn` / legacy `.cfn` / `.cfo` / `.cfr`
- Coverage counts, if measured: total=___ known=___ decoded=___
- Command used, if applicable:
  ```bash
  dotnet run --project samples/CrossFire.Replay.Cli -- coverage "path/to/replay.cfn"
  ```

## Replay context

- Map and game mode, if known:
- Game client/version and approximate replay date:
- Where the message occurs (for example, round start or player damage):

## Wire-layout evidence

Provide the first 64-128 bytes of the payload after the 16-bit message ID, including offsets. Replays and hex excerpts may contain player names, account or session identifiers, chat, or other private data. Redact sensitive bytes with `??`, preserve offsets, and describe the redaction. Do not upload the complete replay unless you are comfortable sharing its contents.

```text
0000: ...
```

Optional: include ImHex notes, field boundaries, byte order, repeated samples, or comparisons that support the proposed layout.

## Current behavior

- [ ] `LtUnknownDecoded`
- [ ] `ReplayParseException` (offset: )
- [ ] Partial or incorrect decode (describe below)
- [ ] Other (describe below)

```text
Current output or error...
```

## Proposed interpretation

<!-- If known, describe field order, sizes, signedness, byte order, and confidence. Include only short, necessary excerpts from material you are permitted to share. -->

## Impact

<!-- Explain how this decode would improve statistics, timelines, exports, debugging, or another concrete workflow. -->

## Checklist

- [ ] I searched existing issues for this message ID or layout.
- [ ] I checked whether the ID is present in `EMessageIdCatalog` and reported the result above.
- [ ] I included enough context or redacted wire data to investigate the gap.
- [ ] I removed or redacted private data from all attachments and excerpts.
- [ ] I reviewed the [reverse-engineering guide](https://github.com/guinhx/crossfire-replay/blob/main/docs/developer-guide/reverse-engineering.md).
