# Documentation

Documentation index for the **CrossFire Replay Toolkit**.

## Getting Started

| Document | Contents |
|----------|----------|
| [Quick start](getting-started/quickstart.md) | Build and test commands, CLI usage, and API examples |
| [.NET API](getting-started/dotnet-api.md) | Main types and read/write workflows |
| [Local fixtures](getting-started/fixtures.md) | Optional replay files for integration tests |

## File Formats

| Document | Contents |
|----------|----------|
| [Format overview](formats/overview.md) | `.cfr`, `.cfn`, `.cfo`, and their containers |
| [CFR SimpleProtocol](formats/cfr-simple-protocol.md) | The `cfrversion` stream and SimpleProtocol messages |
| [CFN PacketSimulator](formats/cfn-packet-simulator.md) | Observed legacy and modern layouts, ILT data, and the middle blob |
| [ILT codecs](protocol/ilt-native-codecs.md) | Codecs based on observed ILT bitstream layouts |

## Development

| Document | Contents |
|----------|----------|
| [Architecture](developer-guide/architecture.md) | Components, namespaces, and extension points |
| [Reverse-engineering method](developer-guide/reverse-engineering.md) | How format hypotheses are investigated and tested |
| [Contributing](developer-guide/contributing.md) | Pull requests, issues, conventions, and privacy requirements |
| [Project status](project-status.md) | Current scope, limitations, and evaluation guidance |

## Legal

- [LICENSE](../LICENSE)
- [CREDITS.md](../CREDITS.md)

This is an independent reverse-engineering and interoperability project. The documented layouts are observations implemented by this repository, not official CrossFire specifications. Review [project status](project-status.md) before relying on the toolkit, and use the **Decode / format gap** issue template for unsupported data.
