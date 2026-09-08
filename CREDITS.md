# Credits

CrossFire Replay Toolkit is an independent implementation for reading and writing observed CrossFire replay formats, including `.cfr`, `.cfn`, and `.cfo` files. It is not affiliated with, sponsored by, or endorsed by Smilegate, a CrossFire publisher, or any other rights holder.

## Maintainers and Contributors

Repository history identifies [guinhx](https://github.com/guinhx) as the original author and current maintainer. Additional contributors are credited through the repository's [commit history](https://github.com/guinhx/crossfire-replay/commits) and merged pull requests.

## Independent Research

The implementation was developed from publicly observable client behavior and on-disk replay artifacts. The documented workflow includes binary inspection, incremental parser development, analysis of bit layouts and container boundaries, comparison with publicly available LithTech protocol material where applicable, and regression testing with local replay fixtures.

The project does not claim to be an official or complete format specification. It was not developed from confidential Smilegate or CrossFire documentation, internal SDKs, or leaked proprietary source trees, and none of those materials are distributed with this repository.

## Acknowledgements

- [.NET](https://dotnet.microsoft.com/) provides the runtime and development toolchain.
- [ImHex](https://github.com/WerWolv/ImHex) supports binary structure inspection during format research.
- Publicly available LithTech source material informed the independently implemented compressed-vector codecs.

## Required Attribution

The project license permits use, modification, distribution, sublicensing, and commercial use subject to its conditions. Section 2 of [LICENSE](LICENSE) requires project credits to remain visible in source distributions, binary distributions, applications, services, and user-facing documentation. Attribution must not be removed, hidden, minimized, obfuscated, or misrepresented.

A suitable attribution statement is:

> Replay parsing powered by [CrossFire Replay Toolkit](https://github.com/guinhx/crossfire-replay), an independent reverse-engineering and interoperability project.
