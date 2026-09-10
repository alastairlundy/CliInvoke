# CliInvoke Agent Guidelines

## What this repo is
CliInvoke — a .NET/C# library for launching and interacting with command-line processes (builders, configuration models, invokers, middleware, DI, and platform specializations).

- Single TFM: `net10.0` (see `global.json` and csproj files). Current release line: `3.0.0`.
- Docs portal lives in `site/` (built with Lunet, published on GitHub release). Root `getting-started.md` is legacy — prefer `site/docs/`.

## Codebase Organization
- **Solution**: `src/CliInvoke.sln`
- **Packages** (each carries its own `Version`/`PackageVersion` and `PackageReleaseNotes` in its csproj):
  - `src/CliInvoke.Core/` — abstractions & models (`ProcessConfiguration`, results, middleware interfaces)
  - `src/CliInvoke/` — implementation: `CliRun` facade, `ProcessInvoker`/`ProcessInvocationPipeline`, builders, `Extensions/` (middleware + DI helpers, including the `AddCliInvoke` DI entry point in `Extensions/DependencyInjection/AddCliInvokeExtensions.cs`, namespace `CliInvoke.Extensions`)
  - `src/CliInvoke.Specializations/` — PowerShell/CMD middleware; also defines the orthogonal add-on registration `AddCliInvokeSpecializations` (`DependencyInjectionExtensions.cs`, namespace `CliInvoke.Extensions`), which registers only its middleware types and is called alongside `AddCliInvoke` (same `ServiceLifetime`)
- `src/CliInvoke.Extensions/` is an **empty leftover directory** — that package no longer exists; its content was folded into the main `CliInvoke` package. `tests/CliInvoke.Extensions.Tests/` still exists and tests the folded-in extensions.
- **Tests**: `tests/` — TUnit on Microsoft.Testing.Platform (`UseTestingPlatformRunner=true`; test projects are `Exe`). CI runs only `tests/CliInvoke.Tests/`.
- **Benchmarks**: `benchmarks/`.
- **SDK**: `global.json` pins .NET 10 SDK (`rollForward: latestFeature`). Check `dotnet --version` vs `global.json` if builds fail with TFM errors.

## Build & Test (replicate CI order)
From repo root (see `.github/workflows/test.yml`):
1. `bash scripts/guard-configureawait.sh src` — **ConfigureAwait guard, hard CI gate run before build.** Every `await` in `src/` must have `.ConfigureAwait(false)`; `await foreach`/`await using` are exempt. Run via Git Bash/WSL on Windows.
2. `dotnet build src/CliInvoke.sln`
3. From `tests/CliInvoke.Tests/`: `dotnet test`

Other gates/quirks:
- **XML doc comments are build errors**: `Directory.Build.props` escalates CS1574/1580/1581/1584/1658/1734/1762 to errors. Keep `<summary>`/`<paramref>`/`<see>` valid on public API.
- **`ImplicitUsings` disabled**; `LangVersion` 14; Nullable enabled. Style: `src/.editorconfig` (no `var` for built-ins, separate using groups, 100-col max).
- **Central Package Management**: `src/`, `tests/`, `benchmarks/` each have their own `Directory.Packages.props`. New `PackageReference` requires a matching `PackageVersion` there — no `Version=` attributes in csprojs.
- **AOT & trimming**: src packages are `IsAotCompatible` + `IsTrimmable` — avoid reflection-dependent APIs.

## Invocation patterns (architecture)
Three patterns, documented in `DESIGN_PATTERNS.md` (includes a decision tree):
- **`CliRun`** — static, stateless, batteries-included facade; recommended default (`RunAsync`/`RunBufferedAsync` with string-args overloads). Allocates a fresh pipeline per call; no process-wide configurable state.
- **`IProcessInvoker`** — DI-centric; the only pattern that flows through the middleware chain.
- **`IExternalProcess` / `IExternalProcessFactory`** — process-like lifecycle control; does **not** flow through middleware (the bypass pattern).
- Built-in middleware: `UseLogging`, `UsePostExitValidation`, `UsePowerShell`, `UseCmd` (+ retry/truncation extensions). Middleware returns the process result **un-disposed** to the caller.
- Middleware state split: **DI** resolves framework services (loggers, validators, lifetimes); **`MiddlewareItems`** is the ad-hoc per-invocation bag shared between middleware (same split as `HttpContext.RequestServices` vs `HttpContext.Items`).
- Construction default: init construction with `required` `TargetFilePath`. `ProcessConfigurationBuilder` is only for argument escaping, `UserCredentialSpec`, and resource-policy callback flows — reaching for the builder by habit is "v2-style code" (see `GLOSSARY.md`).
- Load the `cliinvoke-pattern-validator` skill when adding or changing invocation code.

## Resource disposal
Exactly **three** `IDisposable` types: `IExternalProcess`, `UserCredential`, `UserCredentialSpec` (see README "Resource Disposal"). `ProcessConfiguration` is not disposable; `StandardInput`/`UserCredential` placed inside it remain the caller's responsibility.

## Domain conventions (details in GLOSSARY.md — do not "fix" these)
- `FilePathResolverBase.ResolveFilePath`: PATH lookup first, then directory recursion — a performance contract; reordering requires a new ADR.
- `Get*` returns materialized arrays; `Enumerate*` returns lazy `IEnumerable` — intentional naming asymmetry.
- Custom `GetPathFileExtensions` overrides must return **lowercased** extensions or matching silently fails.
- `Try*` methods must never propagate exceptions (catch `Exception` by convention).

## Packaging & releases
- Use the `cliinvoke-publish-and-package` skill for releases and cross-project testing (it requires running `cliinvoke-inner-loop` first).
- Release-style build: `dotnet build src/CliInvoke.sln -c Release /p:ContinuousIntegrationBuild=true` (SourceLink + snupkg expected in CI).
- Cross-project testing: pack Core to a local feed and restore dependents with `-s ./nupkgs`; `UsePublishedPackages=true` with `-p:CliInvokeCoreVersion=...` / `-p:CliInvokeVersion=...` switches project refs to package refs.
- `publish.yml` is manual (`workflow_dispatch` with `core-version` and `main-version` inputs).
- Update the csproj `PackageVersion`/`PackageReleaseNotes` **and** `CHANGELOG.md` for releases.

## Testing notes
- TUnit + FsCheck (property tests) + Bogus.
- Tests spawn real executables (`dotnet`, `powershell.exe`, `pwsh`); PowerShell middleware tests skip when `pwsh` is not on PATH (e.g., Windows CI). OS-dependent behavior — replicate CI on ubuntu-latest when in doubt.

## PRs & contributing
- Follow `CONTRIBUTING.md`; fill in every section of `.github/pull_request_template.md`.
- Small, focused changes with tests passing; update `site/docs`/README when usage or public API changes.
- **IVT grants are minimized**: new `InternalsVisibleTo` grants need justification (`docs/adr/0001-ivt-minimization.md`); unused grants are removed.
- **Writing public-facing text** (READMEs, docs, guides, skill descriptions): load the `unslop` skill and apply its checklist before committing. AI-sloppy prose is a recurring issue in this repo.

## Specialized workflows (skills)
| Scenario | Skill to Load |
|----------|--------------|
| Daily development: restore, build, and run tests | `cliinvoke-inner-loop` |
| Local NuGet packing / release publishing | `cliinvoke-publish-and-package` |
| Invocation-pattern changes | `cliinvoke-pattern-validator` |

## Agent skills

### Issue tracker

Issues live in GitHub Issues (uses the gh CLI). See `docs/agents/issue-tracker.md`.

### Triage labels

Uses default label names: needs-triage, needs-info, ready-for-agent, ready-for-human, wontfix. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one GLOSSARY.md + docs/adr/ at repo root. See `docs/agents/domain.md`.
