---
name: cliinvoke-publish-and-package
description: Use when testing or performing release operations for CliInvoke.
---

# CliInvoke Publish and Package

This skill provides the operational commands for packaging and publishing the CliInvoke library, ensuring correct versioning and dependency resolution.

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
| Core Version | Yes | The version string for CliInvoke.Core (e.g., 3.0.0) |
| Main Version | Yes | The version string for CliInvoke (e.g., 3.0.0) |

## Workflow

### Step 0: Prerequisite
You MUST load and execute the `cliinvoke-inner-loop` skill first to ensure the codebase is stable and tests are passing.

### Step 1: Local Package Testing
To test changes in `CliInvoke.Core` that impact the main library:
```bash
mkdir -p ./nupkgs
dotnet pack src/CliInvoke.Core -c Release -o ./nupkgs
dotnet restore src/CliInvoke -s ./nupkgs -s https://api.nuget.org/v3/index.json
```

### Step 2: Production Publishing Sequence
Follow the sequence defined in `.github/workflows/publish.yml`:
1. Pack `src/CliInvoke.Core`.
2. Restore and build dependent projects while passing version properties:
   `/p:CliInvokeCoreVersion=<<core-version>>` and `/p:CliInvokeVersion=<<main-version>>`.

### Step 3: Release Build Verification
Verify that the projects build in Release mode with SourceLink enabled. Release builds in CI expect SourceLink and symbol generation; use the CI build flag to replicate that behavior:
```bash
dotnet build src/CliInvoke.sln -c Release /p:ContinuousIntegrationBuild=true
```

## Validation

- [ ] `src/CliInvoke.Core` nupkg is generated in the output directory.
- [ ] `src/CliInvoke` restores successfully using the local feed.
- [ ] Version properties are correctly applied during the build.
- [ ] No prerelease suffix on stable release versions (e.g., `3.0.0`, not `3.0.0-beta.3`).
- [ ] Release build with `/p:ContinuousIntegrationBuild=true` completes without errors (SourceLink and symbol generation are enabled in the csproj).
