---
name: Bug report
about: Report a parse error, crash, broken round-trip, or failing test
title: "[bug] "
labels: bug
assignees: ''
---

## Summary

<!-- Briefly describe the problem and its impact. -->

## Steps to reproduce

1. Command or minimal code sample:
   ```bash
   dotnet run --project samples/CrossFire.Replay.Cli -- read "path/to/replay.cfn"
   ```
2. ...

## Expected behavior

<!-- What should have happened? -->

## Actual behavior

<!-- What happened instead? Include whether the issue is consistent or intermittent. -->

## Environment

- OS and version:
- .NET SDK (`dotnet --version`):
- Repository commit or release:

## Replay metadata

- Format: `.cfr` / `.cfn` / `.cfo`
- File size in bytes:
- Game client/version and approximate replay date, if known:

Do not post a full replay unless you are comfortable sharing its contents. Replays and hex excerpts may contain player names, account or session identifiers, chat, file paths, or other private data. Redact sensitive values before posting; use `??` for redacted hex bytes, preserve offsets, and note what was redacted.

First 64 bytes, if relevant and safe to share:

```text
0000: ...
```

## Error output

<!-- Paste the complete ReplayParseException, stack trace, or CLI output as text. Redact sensitive paths and replay data. -->

```text
...
```

## Inspection output

If applicable, run:

```bash
dotnet run --project samples/CrossFire.Replay.Cli -- inspect "path/to/replay.cfn" --hex
```

Paste the output below after applying the same redaction guidance.

```text
...
```

## Checklist

- [ ] I searched existing issues for the same problem.
- [ ] I confirmed the replay opens in the game client, when possible.
- [ ] I ran `dotnet test CrossFire.Replay.sln`, or explained above why I could not.
- [ ] I removed or redacted private data from all attachments and excerpts.
- [ ] I reviewed the [contributing guide](https://github.com/guinhx/crossfire-replay/blob/main/docs/developer-guide/contributing.md).
