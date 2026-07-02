# Credits

## CrossFire Replay Toolkit

Independent research and implementation for reading CrossFire replay files (`.cfr`, `.cfn`, `.cfo`).

This project is **not** affiliated with, sponsored by, or endorsed by Smilegate or any CrossFire rights holder.

## How this work was produced

The formats documented and implemented here are the result of **years of reverse-engineering practice** applied to publicly observable client behavior and on-disk artifacts. Concretely, our workflow includes:

- **Binary inspection** with tools such as [ImHex](https://github.com/WerWolv/ImHex), hex editors, and structured parsers built incrementally by hand.
- **Interpretation** of bit layouts, compression wrappers, and message boundaries—validated by round-trip tests against real replay fixtures.
- **Correlation** with publicly known Lithtech / game-protocol patterns where applicable (for example compressed vectors documented in open Lithtech sources).
- **Automated regression tests** on real `.cfn` / `.cfr` files from local replay folders (never committed to the repository).

### What we did **not** use

- No Smilegate or CrossFire **internal** documentation, SDKs, or confidential materials.
- No leaked proprietary source trees shipped with this repository.
- No claim of completeness or official specification status.

## Attribution requirement

If you use, fork, or ship this code (including commercially), you **must** keep credits visible and honest. See [LICENSE](LICENSE) section 2.

Suggested attribution line:

> Replay parsing powered by [CrossFire Replay Toolkit](https://github.com/YOUR_ORG/crossfire-replay) — independent reverse-engineering research (ImHex, validation on real fixtures).

Replace the URL with your actual repository location.

## Third-party acknowledgements

- **Lithtech** compressed-vector algorithms — documented in open Lithtech SDK sources; reimplemented here from public descriptions.
- **.NET** — runtime and tooling.
- **ImHex** — hex structure exploration during RE sessions.

## Contributors

Add your name or handle when you contribute a merged change:

- Project maintainers and RE authors (see git history)
