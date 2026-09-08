# Reverse-Engineering Method

CrossFire Replay Toolkit is an independent interoperability project. The repository documents behavior inferred from publicly observable client behavior and legally obtained on-disk replay artifacts. It does not contain an official CrossFire format specification or a bundled proprietary source reference.

## Repository Tools

| Tool | Use |
|------|-----|
| [ImHex](https://github.com/WerWolv/ImHex) or another hex editor | Compare files, inspect boundaries, and test provisional structures |
| `dotnet test CrossFire.Replay.sln` | Run deterministic regression and round-trip tests |
| `dotnet run --project samples/CrossFire.Replay.Cli -- inspect replay.cfn --hex` | Inspect a container and print a bounded hex view |
| `dotnet run --project samples/CrossFire.Replay.Cli -- coverage replay.cfn` | Measure semantic ILT coverage for one PacketSimulator replay |

The repository does not include `references/cshell/`, `IltMessageScan`, or `ProbeCfn`. Source comments and type names that mention native or CShell behavior record the implementation's terminology and hypotheses; they are not links to reference material distributed with this project.

## Investigation Cycle

1. Form a narrow hypothesis by comparing multiple replay samples and identifying stable boundaries, lengths, tags, or bit patterns.
2. Implement the smallest reader or writer change that can test that hypothesis.
3. Add a deterministic synthetic test that records the expected behavior and relevant edge cases.
4. Evaluate the change against private local fixtures from the applicable client build, region, map, and mode.
5. If a writer exists, compare its output with the original bytes and account for every difference.
6. Record uncertainty in code or documentation rather than generalizing beyond the tested samples.

An automated test establishes repeatability for its inputs. A passing round-trip establishes byte preservation only for the exercised path and data; it does not by itself prove that field names or semantic interpretations are correct.

## Evidence Standards

- Prefer several independent samples over a single replay.
- Separate observed facts, such as byte offsets and lengths, from inferred meanings.
- Test truncation, invalid lengths, optional sections, and values near field boundaries.
- Preserve unknown bytes whenever the format model supports doing so.
- Report the client build and capture context when known, but do not include personal or account data.
- Treat numeric names such as `LegacyV2022` and `ModernV2026` as repository classifications for observed layouts, not guarantees that every client from that year uses the layout.

## External Material and Replay Data

Publicly documented algorithms may be independently reimplemented when licensing permits, with attribution recorded in [CREDITS.md](../../CREDITS.md). Do not contribute leaked source code, confidential documentation, proprietary dumps, game assets, or material that you are not authorized to share.

Replay files remain local because they can contain player names, identifiers, timestamps, match metadata, and other sensitive information. Follow the redaction and consent requirements in [Contributing](contributing.md), including for hex excerpts and screenshots.

## Current Uncertainties

- Rare ILT IDs and game-mode-specific variants may not be represented in the available tests.
- `LtNativeSerializers` uses default world bounds of `-4096` to `4096` when encoding or decoding compressed positions without map-specific bounds. This is an implementation assumption.
- Middle-blob and archival payload layouts may vary across client builds. Unrecognized bytes must not be assigned a meaning without additional evidence.

## Attribution

The project license requires visible attribution in distributions, forks, derivative works, products, services, and user-facing documentation. Preserve [LICENSE](../../LICENSE) and [CREDITS.md](../../CREDITS.md) as required. A suggested attribution is:

> Replay parsing powered by [CrossFire Replay Toolkit](https://github.com/guinhx/crossfire-replay), an independent reverse-engineering and interoperability project. Not affiliated with, sponsored by, or endorsed by Smilegate or any CrossFire rights holder.
