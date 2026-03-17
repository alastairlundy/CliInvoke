# Copilot / Coding Agent Onboarding: CliInvoke
Purpose
- Give a coding agent the minimal, high-value information needed to make correct, fast changes without searching the repo every time.
- Trust these instructions as the canonical “how to build, test and validate” summary. Only search the repo if something below is missing or fails.

Quick summary
- What this repo is: CliInvoke — a .NET/C# library for running and interacting with command-line processes (builders, configuration models, invokers, DI helpers and optional specializations).
- Languages & targets: C#/.NET. Primary targets: .NET Standard 2.0, .NET 8, .NET 9, and .NET 10. The codebase is organized under src/, with a solution file at src/CliInvoke.sln.
- Approximate repo structure: small-to-medium .NET library (multiple projects under src/). Top-level docs, tests and CI workflows exist.

Key facts an agent needs immediately
- Solution and important project folders:
  - src/CliInvoke.sln — root solution for local builds.
  - src/CliInvoke.Core/ — core abstractions and models.
  - src/CliInvoke/ — implementation package (main package).
  - src/CliInvoke.Extensions/ — DI and helper extensions.
  - src/CliInvoke.Specializations/ — optional platform/shell specializations.
- Docs:
  - docs/docs/building-cliinvoke.md — authoritative build guidance (short summary in this file).
  - README.md — examples and usage snippets.
- CI / Workflows:
  - .github/workflows/test.yml — CI that runs restore, build and test for src/CliInvoke/ (runs on push/PR to main).
  - .github/workflows/publish.yml — manual workflow used to pack/publish NuGet packages (uses local ./nupkgs feed and requires core-version/main-version inputs).
  - .github/workflows/scorecard.yml — OpenSSF scorecard.
  - .github/dependabot.yml — dependency automation.
- global.json exists in src/ — respect SDK pinning if present (use `dotnet --version` and prefer the SDK it requests).

Essential environment & versions (always check on the runner)
- Preferred SDK for development: .NET 10 SDK (docs state .NET 10 is required to target all TFMs). CI Actions currently use setup-dotnet with `dotnet-version: 10.0.x` but library targets include .NET 10/.NET 9/.NET 8/.NET Standard 2.0.
- Rule: Always match global.json if present. If uncertain, use the repo docs value. Confirm with `dotnet --version`.

Bootstrap (one-time per environment)
1. Install .NET SDK that matches src/global.json or the repo docs (recommended: .NET 10).
2. From repository root:
   - Clean local artifacts: git clean -fdx (if you want a pristine environment).
   - Restore: dotnet restore src/CliInvoke/   (work from repo root is fine; CI sets working-directory to src/CliInvoke/)
Why: CI restores/builds/tests inside src/CliInvoke/ — doing the same reduces surprises.

Common commands (reproduce CI locally)
- Reproduce CI Test job (recommended sequence — this mirrors the test workflow):
  1. cd <repo-root>
  2. dotnet restore src/CliInvoke/
  3. dotnet build --no-restore -c Debug -v minimal --project src/CliInvoke/   (or `dotnet build src/CliInvoke.sln -c Debug`)
  4. dotnet test --no-build --verbosity normal --project src/CliInvoke/
- Full solution build:
  - dotnet build src/CliInvoke.sln -c Debug
- Release build (validate packaging / SourceLink similar to CI/publish):
  - dotnet build -c Release /p:ContinuousIntegrationBuild=true
  - To pack: dotnet pack src/CliInvoke.Core -c Release -o ./nupkgs

Packaging / publish reproduction (follow publish.yml)
- The publish workflow packs core into a local feed and uses that feed when building Specializations/Main/Extensions:
  1. mkdir -p ./nupkgs
  2. dotnet build src/CliInvoke.Core -c Release
  3. dotnet pack src/CliInvoke.Core -c Release -o ./nupkgs
  4. dotnet restore src/CliInvoke.Specializations -s ./nupkgs -s https://api.nuget.org/v3/index.json
  5. dotnet build src/CliInvoke.Specializations -c Release -p:CliInvokeCoreVersion=<core-version>
  6. Repeat restore/build for src/CliInvoke and src/CliInvoke.Extensions, passing `/p:CliInvokeCoreVersion=<core-version>` and `/p:CliInvokeVersion=<main-version>` where appropriate.
- Note: publish.yml expects CI inputs `core-version` and `main-version`. When changing package version numbers, update the corresponding csproj properties and changelog as required.

Tests and validation to run before a PR
- Always run:
  - dotnet test --no-build --project src/CliInvoke/
  - If you changed a project or public API, run `dotnet build src/CliInvoke.sln -c Release` and run tests again.
- For package/API changes:
  - Update the csproj version and changelog where appropriate.
  - Run the Release build with `/p:ContinuousIntegrationBuild=true` to surface packaging/SourceLink problems.
- For changes to CliInvoke.Core that other projects depend on:
  - Locally `dotnet pack` core to ./nupkgs and restore the other projects from ./nupkgs (see packaging steps) to ensure consumers build against the same package artifacts.

Common gotchas and guidance
- Working directory matters: many CI steps run with working-directory = src/CliInvoke/. Reproduce the same layout locally when trying to replicate CI failures.
- SDK mismatch: If builds fail with TFM/target errors, check `dotnet --version` vs src/global.json. Use the SDK requested by global.json (or .NET 10 per docs).
- Packaging dependencies: publish workflow uses a local nupkg feed. When testing local changes that affect multiple projects, pack core first and restore the others from that local feed.
- SourceLink / CI: release builds in CI expect SourceLink and symbol generation. Use `/p:ContinuousIntegrationBuild=true` to replicate CI/release behavior.
- Tests that call external processes may behave differently on different OSes. Prefer running CI-style on Ubuntu-latest/container if reproducing CI failures.

Repository layout quick map (priority order)
- Root files (important):
  - README.md — examples, usage snippets and links to build docs.
  - CONTRIBUTING.md, CODE_OF_CONDUCT.md, SECURITY.md
  - docs/docs/building-cliinvoke.md — build and release guidance (read first).
  - .github/workflows/* — CI and publish workflows (test.yml, publish.yml, scorecard.yml).
  - src/ — solution and projects:
    - src/CliInvoke.sln
    - src/CliInvoke.Core/
    - src/CliInvoke/
    - src/CliInvoke.Extensions/
    - src/CliInvoke.Specializations/
    - src/global.json
  - tests/ — test projects (run with dotnet test)
  - benchmarks/, .assets/, THIRD_PARTY_NOTICES.txt, VERSION_HISTORY.md
- When making a change, prefer to open/modify files under src/ and run the tests that target src/CliInvoke/ to get fast feedback.

What checks run on PRs (so your PR must pass these)
- .github/workflows/test.yml runs on push and pull_request to main:
  - Setup .NET on ubuntu-latest (Action uses dotnet-version: 10.0.x)
  - dotnet restore (working-directory: src/CliInvoke/)
  - dotnet build --no-restore (working-directory: src/CliInvoke/)
  - dotnet test --no-build (working-directory: src/CliInvoke/)
- Scorecard and Dependabot run independently (scorecard scheduled/push).
- Publishing to NuGet is manual and requires secrets / OIDC; do not attempt to publish from a PR.

If you change public API or package versions
- Update package version properties in the appropriate .csproj files and the project changelog(s).
- Run CI-equivalent builds and tests locally before opening a PR.
- If modifying core that other projects consume, validate by packing core and restoring the other projects from that local feed (see packaging steps).

Developer expectations for PRs
- Small, focused changes with tests passing.
- If adding features or changing behavior, include or update tests under tests/ and run dotnet test locally.
- Update docs/README where usage or public API changed.
- If the change affects packaging versions, ensure publish steps will succeed by following the publish reproduction sequence locally.

Search guidance (when to search the repo)
- Trust these instructions first.
- Search the repo only when:
  - You need the exact csproj property name to update (search for `<PackageVersion>` or relevant msbuild property).
  - You need to read a specific test, public API or implementation file to implement behavior.
  - A reproduced build/test step fails and the cause isn’t obvious from the logs.
- Preferred quick look targets before broad search:
  - docs/docs/building-cliinvoke.md
  - src/global.json
  - .github/workflows/test.yml and publish.yml
  - src/CliInvoke.sln and the specific project .csproj files
