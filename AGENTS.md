# CliInvoke Agent Guidelines

## What this repo is
CliInvoke — a .NET/C# library for running and interacting with command-line processes (builders, configuration models, invokers, DI helpers and optional specializations).

- Languages & targets: C#/.NET. Primary targets: .NET 8, .NET 9, and .NET 10. 
- Codebase organization: Under src/, with solution file at src/CliInvoke.sln
- Structure: Small-to-medium .NET library with multiple projects under src/

## Key Facts
- Solution: `src/CliInvoke.sln`
- Projects: 
  - `src/CliInvoke.Core/` - Abstractions and models
  - `src/CliInvoke/` - Main implementation
  - `src/CliInvoke.Extensions/` - DI and helpers
  - `src/CliInvoke.Specializations/` - Platform-specific features
- Tests: Located in `tests/` folder, use TUnit framework
- .NET SDK: Respect `global.json` (currently .NET 10.0) - matches CI

## Important Conventions
- **Working directory**: CI runs from `src/CliInvoke/` (see `.github/workflows/test.yml`) - match this when reproducing issues
- **Target frameworks**: net8.0; net9.0; net10.0 (see csproj files)
- **Testing**: Uses TUnit framework - `dotnet test` discovers and runs tests
- **Resource disposal**: Key disposable types: `ProcessConfiguration`, `IExternalProcess`, `UserCredential`, `UserCredentialBuilder`, `PipedProcessResult`
- **Versioning**: Update csproj version and changelog for releases

## Common Gotchas
- SDK mismatch: Check `dotnet --version` vs `global.json` if build fails with TFM errors
- Packaging: When testing changes across projects, pack core first and restore others from local nupkg feed
- External processes: Tests calling executables may behave differently across OSes. Prefer running in Ubuntu-latest/containers to replicate CI failures accurately.
- SourceLink / CI: release builds in CI expect SourceLink and symbol generation. Use `/p:ContinuousIntegrationBuild=true` to replicate CI/release behavior.

## Developer Expectations for PRs
- Small, focused changes with tests passing.
- If adding features or changing behavior, include or update tests under tests/ and run dotnet test locally.
- Update docs/README where usage or public API changed.
- If modifying core that other projects consume, validate by packing core and restoring the other projects from that local feed.

## Repository Layout (Priority Order)
- Root files: README.md, CONTRIBUTING.md
- Build guidance: docs/docs/building-cliinvoke.md
- CI workflows: .github/workflows/* (test.yml, publish.yml, scorecard.yml)
- Source: 
  - src/CliInvoke.sln
  - src/CliInvoke.Core/
  - src/CliInvoke/
  - src/CliInvoke.Extensions/
  - src/CliInvoke.Specializations/
  - src/global.json
- Tests: tests/ — test projects (run with dotnet test)
- Other: benchmarks/, .assets/, THIRD_PARTY_NOTICES.txt

## Specialized Workflows
The following skills are available to handle specific operational tasks. Load them when the corresponding scenario arises:

| Scenario | Skill to Load |
|----------|--------------|
| Daily development: restoring, building, and running tests | `cliinvoke-inner-loop` |
| Local NuGet packing for cross-project testing | `cliinvoke-publish-and-package` |
| Production release: versioning and publishing to NuGet | `cliinvoke-publish-and-package` |

## Agent skills

### Issue tracker

Issues live in the repo's GitHub Issues. See `docs/agents/issue-tracker.md`.

### Triage labels

Standard canonical labels are used. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout. See `docs/agents/domain.md`.
