---
name: cliinvoke-inner-loop
description: Daily development workflow for CliInvoke (restore, build, test)
---

# CliInvoke Inner Loop

This skill provides the a standardized sequence for daily development and local validation of the CliInvoke library.

## When to Use

- Implementing new features or fixing bugs.
- Verifying code changes before committing.
- Running a full clean build of the solution.

## When Not to Use

- When performing release packaging or publishing to NuGet.
- When analyzing high-level architecture.

## Workflow

### Step 1: Restore and Build Main Project
Run the main project restore and build to ensure the implementation is correct.
```bash
dotnet restore src/CliInvoke/
dotnet build --no-restore -c Debug src/CliInvoke/
```

### Step 2: Execute Tests
Run tests for the main project to verify functionality.
```bash
dotnet test --no-build src/CliInvoke/
```

### Step 3: Optional Full Solution Build
If changes affect multiple projects, build the entire solution.
```bash
dotnet build src/CliInvoke.sln -c Debug
```

### Step 4: Release Build Verification
Verify that the project builds in Release mode with SourceLink enabled.
```bash
dotnet build -c Release /p:ContinuousIntegrationBuild=true
```

## Validation

- [ ] `dotnet build` succeeds for `src/CliInvoke/`.
- [ ] All tests in `src/CliInvoke/` pass.
- [ ] Release build completes without errors.
