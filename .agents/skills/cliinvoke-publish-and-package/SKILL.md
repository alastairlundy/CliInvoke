---
name: cliinvoke-publish-and-package
description: Use when testing or performing release operations for CliInvoke.
---

# CliInvoke Publish and Package

The commands for packaging and publishing CliInvoke, along with the versioning and dependency-resolution details that go with them.

## When to Use

- Creating local NuGet packages for cross-project testing.
- Preparing a release for production publishing.
- Updating project versions for a new release.

## When Not to Use

- During standard feature development (use `cliinvoke-inner-loop`).
- For simple bug fixes that don't require a new package version.

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Release versions | Yes | Set `PackageVersion` in each csproj (`CliInvoke.Core`, `CliInvoke`, `CliInvoke.Specializations`). These are the single source of truth; no workflow inputs or version properties are passed. |
| Release notes | Yes | Update `PackageReleaseNotes` in the same three csprojs, and add a matching entry to `CHANGELOG.md`. Version bumps without notes are incomplete. |

## Workflow

### Step 0: Prerequisite
You MUST load and execute the `cliinvoke-inner-loop` skill first to ensure the codebase is stable and tests are passing.

### Step 1: Local Package Testing
To test a change in `CliInvoke.Core` against the packages that consume it, pack Core to a local feed and restore both dependents. `CliInvoke.Specializations` references Core as well as the main package, so testing only `src/CliInvoke` leaves half the consumers uncovered.
```bash
mkdir -p ./nupkgs
dotnet pack src/CliInvoke.Core -c Release -o ./nupkgs
dotnet restore src/CliInvoke -s ./nupkgs -s https://api.nuget.org/v3/index.json
dotnet restore src/CliInvoke.Specializations -s ./nupkgs -s https://api.nuget.org/v3/index.json
```
In PowerShell, create the directory with `New-Item -ItemType Directory -Force ./nupkgs` instead of `mkdir -p`.

One caveat worth knowing before you trust this result: the two dependents reach Core through a `ProjectReference`, and the project graph resolves that reference as a project. Adding a package source does not redirect it, so the packed nupkg may not be what actually gets compiled against.

### Step 2: Production Publishing Sequence
Follow the sequence in `.github/workflows/publish.yml`. Each project is a plain `dotnet pack`, run in dependency order:
1. Pack `src/CliInvoke.Core`.
2. Pack `src/CliInvoke`.
3. Pack `src/CliInvoke.Specializations`.

The three projects reference each other via ProjectReference (versioned by each csproj's `PackageVersion`), so no feed or version properties are needed.

### Step 3: Release Build Verification
Verify that the projects build in Release with SourceLink and symbol generation enabled, which is what CI does. Use the CI build flag to match it:
```bash
dotnet build src/CliInvoke.sln -c Release /p:ContinuousIntegrationBuild=true
```

## Validation

- [ ] `src/CliInvoke.Core` nupkg is generated in the output directory.
- [ ] Both `src/CliInvoke` and `src/CliInvoke.Specializations` restore successfully against the local feed.
- [ ] All three csprojs' `PackageVersion` properties match the intended release version.
- [ ] `PackageReleaseNotes` is updated in all three csprojs and `CHANGELOG.md` has a matching entry.
- [ ] No prerelease suffix on stable release versions. Note that the repo currently sits on a prerelease (`3.1.0-alpha.1`), so this item is expected to fail until you cut a stable release.
- [ ] Release build with `/p:ContinuousIntegrationBuild=true` completes without errors (SourceLink and symbol generation are enabled in the csproj).
