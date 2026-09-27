---
name: cliinvoke-inner-loop
description: Use when developing or working on CliInvoke. Runs the ConfigureAwait guard, builds the solution, and runs the test suite in the same order as CI.
---

# CliInvoke Inner Loop

The daily development and validation sequence. It mirrors `.github/workflows/test.yml` step for step, so a green inner loop means a green CI build.

## When to Use

- Implementing a feature or fixing a bug.
- Checking work before committing or opening a pull request.

## When Not to Use

- Packing or publishing to NuGet. Use `cliinvoke-publish-and-package`.
- Architecture analysis. No build required.

## Prerequisites

- The .NET 10 SDK pinned by `global.json`.
- Git Bash or WSL on Windows, for the guard script in Step 1.

## Workflow

Run these in order from the repository root.

### Step 1: ConfigureAwait Guard (Required)

This is a hard CI gate, and it runs before the build. Every `await` under `src/` needs `.ConfigureAwait(false)`. `await foreach` and `await using` are exempt, and the script already allows for them.

```bash
bash scripts/guard-configureawait.sh src
```

On Windows, run it through Git Bash or WSL. A `bash` that resolves to a plain POSIX `sh` fails on the script's `set -euo pipefail`.

### Step 2: Build the Solution

```bash
dotnet build src/CliInvoke.sln
```

Build the solution, not a single project. It covers all three shipped packages plus the test projects, so a per-project build quietly skips `CliInvoke.Specializations`. Restore is implicit here; you do not need a separate `dotnet restore`.

### Step 3: Run the Tests

```bash
dotnet test
```

Run this from `tests/CliInvoke.Tests/`. That is the only test project CI executes. There is no test project anywhere under `src/`, so `dotnet test src/CliInvoke/` fails with `No test projects were found.`

The suite spawns real executables (`dotnet`, `powershell.exe`, `pwsh`), so it takes considerably longer than the build. Tests that need PowerShell Core call `SkipTestIfNull` when `pwsh` is off `PATH`, which is the situation on GitHub-hosted Windows runners but not on most dev machines. A skip is not a failure, so compare the passed count against your own baseline rather than expecting a fixed total.

### Step 4: Release Build

Only needed when you are heading toward a release. Otherwise skip it.

```bash
dotnet build src/CliInvoke.sln -c Release /p:ContinuousIntegrationBuild=true
```

Release builds turn on SourceLink and symbol packages, so this catches packaging problems before CI does. `cliinvoke-publish-and-package` owns this step for actual release work.

## Validation

- [ ] The ConfigureAwait guard passed.
- [ ] `dotnet build src/CliInvoke.sln` reported 0 errors. The build is warning-noisy (CA1416 platform-compatibility and TUnit assertion hints, mostly in tests), so track errors rather than expecting a clean warning count.
- [ ] `dotnet test` in `tests/CliInvoke.Tests/` passed.
