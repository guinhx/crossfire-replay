# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html). Before 1.0, breaking API changes may be introduced in minor releases.

## [Unreleased]

### Changed

- Revised the public repository documentation to clarify observed format support, byte round-trip validation, semantic decoding limits, and client compatibility.

## [0.1.0] - 2026-07-02

### Added

- Added the `CrossFire.Replay` library for observed `.cfr`, `.cfn`, and `.cfo` layouts.
- Added partial semantic ILT decoders and a message ID catalog.
- Added `IltDecodeCoverage` and the CLI `coverage` command.
- Added `ReplayToolkitVersion` and `schemaVersion` to JSON timeline exports.
- Added `ReplayFixturePaths` for integration tests configured through environment variables.
- Added project status and local fixture documentation.
- Added package metadata and support for local `dotnet pack` builds.

Known limitations are tracked in [docs/project-status.md](docs/project-status.md).
