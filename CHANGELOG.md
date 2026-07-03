# Changelog

Formato baseado em [Keep a Changelog](https://keepachangelog.com/). Versionamento [SemVer](https://semver.org/) pré-1.0.

## [0.1.0] - 2026-07-02

### Added
- Biblioteca `CrossFire.Replay` — leitura/escrita `.cfr`, `.cfn`, `.cfo`
- Decoders ILT semânticos parciais + catálogo de IDs
- `IltDecodeCoverage` e comando CLI `coverage`
- `ReplayToolkitVersion` e `schemaVersion` no export de timeline JSON
- `ReplayFixturePaths` para testes de integração via env vars
- Documentação `docs/project-status.md` e `docs/getting-started/fixtures.md`
- Metadados NuGet no `.csproj` (`dotnet pack` local)

### Known limitations
- Ver [docs/project-status.md](docs/project-status.md)
