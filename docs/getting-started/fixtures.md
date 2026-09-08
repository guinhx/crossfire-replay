# Local replay fixtures

Fixture-backed integration tests use local `.cfn` replay files. These files are not committed because they may be large and may contain private match data.

## Test behavior

- Synthetic unit and round-trip tests run without external replay files.
- Many `[Fact]` methods named `UserCfn_*` return immediately when the primary fixture cannot be resolved. Test runners generally report these as passed, not skipped.
- Fixture theories obtain cases from the resolved replay folder. If no files are found, no fixture cases are produced; the repository's configured xUnit v2 runner reports a theory with no data as a failure.
- A successful test run without fixtures therefore does not imply that real replay parsing was exercised.

The repository does not guarantee a fixed test count; the count changes as tests and local fixture cases are added.

## Configure one primary fixture

In PowerShell:

```powershell
$env:CROSSFIRE_REPLAY_FIXTURE_CFN = 'C:\Replays\CFReplay20260701_0000.cfn'
dotnet test CrossFire.Replay.sln
```

`CROSSFIRE_REPLAY_FIXTURE_CFN` must name an existing file. It is used by tests that request the primary modern fixture and also provides its parent folder when no fixture-folder variable is set.

## Configure a fixture folder

```powershell
$env:CROSSFIRE_REPLAY_FIXTURE_FOLDER = Join-Path $env:USERPROFILE 'Documents\Cross Fire\Replay'
dotnet test CrossFire.Replay.sln
```

The folder resolver enumerates its top-level `*.cfn` files in case-insensitive path order. It does not recurse into subdirectories. Setting only the folder variable does not select a primary fixture for `[Fact]` tests. Conversely, a valid primary fixture supplies its parent directory to the theory resolver when `CROSSFIRE_REPLAY_FIXTURE_FOLDER` is not set.

## Default primary fixture

When `CROSSFIRE_REPLAY_FIXTURE_CFN` is unset or invalid, the primary resolver checks this exact path under the current user's Documents folder:

```text
Cross Fire\Replay\CFReplay20260701_0000.cfn
```

No general search of the Replay folder is performed for the primary fixture.

## What fixture tests cover

Depending on which fixtures are available, tests exercise areas such as:

- detection and parsing of the project-classified `ModernV2026` inner layout;
- byte-preserving reconstruction of inner payloads and archive components;
- middle-blob parsing, coverage metrics, and binary-snapshot extraction;
- JSON and CSV timeline export;
- selected semantic ILT decoders.

These tests establish only their explicit assertions. Byte-for-byte round trips show preservation of observed bytes, not that all fields are understood semantically or that generated files are accepted by a game client.

## CI

For a pipeline that requires fixture coverage:

1. Store replay files in private artifact or secret-backed storage, not in Git.
2. Set `CROSSFIRE_REPLAY_FIXTURE_CFN`, `CROSSFIRE_REPLAY_FIXTURE_FOLDER`, or both for the test process.
3. Add an explicit pipeline precondition if missing fixtures must fail the job; the test suite does not consistently enforce their presence.

## Privacy

Replays may contain nicknames, identifiers, and match metadata. Do not attach complete replay files to public issues. Prefer a minimal hexadecimal payload excerpt and use the **Decode / format gap** issue template.
