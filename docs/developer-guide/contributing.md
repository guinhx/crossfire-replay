# Contributing

Contributions that improve replay interoperability through reproducible, lawful research are welcome.

## Before Opening a Pull Request

1. Keep the change focused and avoid unrelated refactoring.
2. Add deterministic tests for format or codec changes. Round-trip assertions demonstrate preservation only for the tested values and path, so add field-level assertions when claiming semantic behavior.
3. Run `dotnet test CrossFire.Replay.sln` and state whether any fixture-backed tests actually exercised local replay files.
4. Review the diff for replay data, personal information, credentials, local paths, and proprietary material.
5. Preserve `LICENSE` and `CREDITS.md` and comply with their attribution and no-endorsement requirements.

## Code Conventions

- Target C# and .NET 10 with nullable reference types enabled.
- Follow the naming and formatting already used in the edited file.
- Use decoded records for ILT message models where that matches the existing code.
- Comment only where a non-obvious algorithm, assumption, or observed layout needs explanation.
- Describe reverse-engineered layouts as observations or hypotheses, not official specifications.

## Contribution Areas

| Contribution | Primary location |
|--------------|------------------|
| Semantic ILT decoder | `src/CrossFire.Replay/Protocol/Lt/` |
| SimpleProtocol message | `src/CrossFire.Replay/Protocol/ProtocolDeserializer.Handlers.cs` and `ProtocolSerializer.cs` |
| PacketSimulator layout | `src/CrossFire.Replay/Formats/PacketSimulator/` |
| Tests | `tests/CrossFire.Replay.Tests/` |
| Documentation and developer experience | `docs/` |

## Privacy and Data Handling

Assume replay files and derived output are sensitive. They may expose player names, account or object identifiers, timestamps, chat or match metadata, source file paths, and behavioral data.

- Do not commit or publicly attach replay files, timeline exports, packet captures, memory dumps, screenshots, or logs containing data from other people.
- Obtain permission from affected participants before sharing any replay-derived data, even in a private issue or discussion.
- Prefer a synthetic reproducer. If real bytes are essential, extract the smallest relevant range and verify that the range does not contain names, identifiers, timestamps, paths, tokens, keys, or unrelated payloads.
- Redact screenshots and command output. File names and directory paths can identify a user or machine.
- Do not assume a short hex excerpt is anonymous. Encoded strings and identifiers may not be visually obvious.
- Never submit encryption keys, credentials, access tokens, confidential documents, leaked source, proprietary binaries, or game assets.
- If safe redaction would remove the information needed to diagnose a problem, describe the behavior without attaching the data and ask maintainers whether a secure alternative exists. Do not post first and request deletion later.

Repository history and third-party mirrors may retain removed material. If sensitive data is disclosed, rotate any affected secret immediately and contact the relevant hosting provider; deleting a comment or commit is not sufficient containment.

## Opening an Issue

| Template | Use it for |
|----------|------------|
| **Bug report** | Crashes, parse errors, preservation failures, or failing tests |
| **Decode / format gap** | Unsupported ILT IDs, newly observed layouts, or unexplained bytes |
| **Feature request** | New APIs, exports, or tooling |

For a bug or parse failure, include the .NET version, operating system, file extension and approximate size, sanitized output from the following command, and the exception message or stack trace:

```bash
dotnet run --project samples/CrossFire.Replay.Cli -- inspect replay.cfn --hex
```

For a decode gap, include the `EMessageId` value or catalog name, sanitized output from the coverage command, and the relevant game mode or map when known:

```bash
dotnet run --project samples/CrossFire.Replay.Cli -- coverage replay.cfn
```

Only include a minimal payload excerpt when it is necessary, lawful to share, and reviewed under the privacy rules above. State its exact boundary, whether the 16-bit message ID is included, and how it was obtained.

For a feature request, explain the concrete consumer, desired output, and why the current API and `LtUnknownDecoded` handling are insufficient.

## Pull Request Content

Explain the evidence for the change, the client context in which it was observed, the tests added, and the remaining uncertainty. ImHex screenshots or real fixture statistics are optional and must be sanitized before publication. Do not claim broad compatibility from one fixture.

The repository contains no `references/cshell/` directory. Do not add links to unavailable local dumps or rely on proprietary source material as a contribution prerequisite.

## License and Attribution

The project license requires visible, human-readable attribution in source and binary distributions, applications, services, and user-facing documentation. Source distributions must retain `LICENSE` and `CREDITS.md`, or equivalent content. Do not remove, hide, minimize, obfuscate, or misrepresent project credits, and do not imply endorsement by Smilegate, CrossFire publishers, or any rights holder.

## Contact

Use [GitHub Issues](https://github.com/guinhx/crossfire-replay/issues) in the project repository.
