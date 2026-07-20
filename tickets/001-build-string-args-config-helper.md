---
title: Extract `BuildStringArgsConfig` helper for the 3 string-arg overloads
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CliInvoke-clirun-shape.md
---

## Goal

Introduce a single private static helper that builds a `ProcessConfiguration` and a `ProcessExitConfiguration` from string arguments, eliminating the three duplicated five-line config-building blocks in `RunAsync(string, ...)`, `RunBufferedAsync(string, ...)`, and `RunPipedAsync(string, ...)`. The helper is pure — it does not pre-resolve the target file path.

## What to build

Add a helper method to `src/CliInvoke/Extensions/CliRun.cs` with the following signature (the signature below is from the F4 implementation blueprint at `IMPLEMENTATION-clirun-shape.md`):

```csharp
private static ProcessConfiguration BuildStringArgsConfig(
    string targetFilePath,
    string arguments,
    string? workingDirectory,
    bool redirectStandardOutput,
    TimeSpan? timeoutTimeSpan,
    out ProcessExitConfiguration exitConfiguration)
```

The helper:

- Defaults `workingDirectory` to `Environment.CurrentDirectory` when null.
- Builds the `ProcessConfiguration` via `ProcessConfigurationFactory.Create(targetFilePath, arguments, workingDirectory, redirectStandardOutput)`.
- Defaults `timeoutTimeSpan` to `ProcessTimeoutPolicy.Default.TimeoutThreshold` when null.
- Builds the `ProcessExitConfiguration` via `ProcessExitConfiguration.CreateGraceful(ProcessTimeoutPolicy.FromTimeSpan((TimeSpan)timeoutTimeSpan))`.

Each of the three string-argument overloads at lines 97, 164, and 224 calls the helper with the `using var` pattern and forwards to the matching config-argument overload. The `redirectStandardOutput` flag is `false` for `RunAsync(string, ...)` and `true` for the buffered and piped variants. Add a code-review note above the helper signature that records the D006 rationale and another note at the top of the helper body that records the T006 rationale.

## Size

- **Files** - 1

## Recommended Workflow

### Step 1 — Add the `BuildStringArgsConfig` helper below the existing private getters

Where: `src/CliInvoke/Extensions/CliRun.cs`

- Insert the helper method between `GetExternalProcessFactory()` and the first public `RunAsync` overload.
- Add a `// D006: out parameter is intentional; do not convert to tuple, the using declaration depends on it` comment above the signature.
- Add a `// T006: helper is pure; resolution happens at the factory level; do not pre-resolve in the helper` comment at the top of the method body.

Verify: The helper compiles in isolation; the body does not call `GetFilePathResolver()` and does not read or write `_filePathResolver`.

### Step 2 — Replace the config-building block in `RunAsync(string, ...)`

Where: `src/CliInvoke/Extensions/CliRun.cs`

- Replace the body with a single `using var configuration = BuildStringArgsConfig(targetFilePath, arguments, workingDirectory, redirectStandardOutput: false, timeoutTimeSpan, out var exitConfiguration);` followed by the existing forward to `RunAsync(configuration, exitConfiguration, cancellationToken)`.
- Remove the now-redundant `workingDirectory ??= Environment.CurrentDirectory;` and `timeoutTimeSpan ??= ProcessTimeoutPolicy.Default.TimeoutThreshold;` lines.

Verify: `RunAsync(string, ...)` still returns `Task<ProcessResult>`; the body is two lines (helper call + forward).

### Step 3 — Replace the config-building block in `RunBufferedAsync(string, ...)`

Where: `src/CliInvoke/Extensions/CliRun.cs`

- Replace the body with a single `using var configuration = BuildStringArgsConfig(targetFilePath, arguments, workingDirectory, redirectStandardOutput: true, timeoutTimeSpan, out var exitConfiguration);` followed by the existing forward to `RunBufferedAsync(configuration, exitConfiguration, cancellationToken)`.
- Remove the now-redundant `workingDirectory ??= ...` and `timeoutTimeSpan ??= ...` lines.

Verify: `RunBufferedAsync(string, ...)` still returns `Task<BufferedProcessResult>`; the body is two lines.

### Step 4 — Replace the config-building block in `RunPipedAsync(string, ...)`

Where: `src/CliInvoke/Extensions/CliRun.cs`

- Replace the body with a single `using var configuration = BuildStringArgsConfig(targetFilePath, arguments, workingDirectory, redirectStandardOutput: true, timeoutTimeSpan, out var exitConfiguration);` followed by the existing forward to `RunPipedAsync(configuration, exitConfiguration, cancellationToken)`.
- Remove the now-redundant `workingDirectory ??= ...` and `timeoutTimeSpan ??= ...` lines.

Verify: `RunPipedAsync(string, ...)` still returns `Task<PipedProcessResult>`; the body is two lines.

### Step 5 — Build the full solution and run existing tests

Where: repository root

- Run `dotnet build src/CliInvoke.sln` to confirm zero errors and zero new warnings on net8.0, net9.0, and net10.0.
- Run `dotnet test tests/CliInvoke.Tests/CliInvoke.Tests.csproj` from `tests/CliInvoke.Tests/` to confirm no regressions in existing tests.

Verify: Build succeeds; existing tests pass with no test count or assertion regressions.

## Context pointers

**Files**
- `src/CliInvoke/Extensions/CliRun.cs` — the single file edited by this ticket; the helper is added and the three string-argument overloads are simplified.
- `src/CliInvoke.Core/Factories/ProcessConfigurationFactory.cs` — the factory called by the helper; not edited, but its 4-argument `Create` overload is the contract the helper depends on.
- `IMPLEMENTATION-clirun-shape.md` — the F4 implementation prototype; the helper's signature and behaviour above are taken from the blueprint's `### 1. The 3 string-arg overloads share a pure helper` section.

**Domain terms**
- Resource-Owning Type (from `CONTEXT.md`) — `ProcessConfiguration` is a Resource-Owning Type and the `using var` declaration in the helper's callers is the lifecycle-management idiom.

**Ledger records**
- `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#D003` — extract `BuildStringArgsConfig` and call it from all three string-argument overloads.
- `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#D006` — use the `out ProcessExitConfiguration` parameter pattern; do not convert to a tuple return.
- `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#T006` — helper is pure; resolution happens at the factory level; do not pre-resolve in the helper.
- Cross-cite (superseded): `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#D004` (tuple return — superseded by D006) and `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#T001` (eager resolver reading — superseded by T006) — both covered by this ticket via their active successors.

## Acceptance criteria

- [ ] `BuildStringArgsConfig` is a `private static` method in `src/CliInvoke/Extensions/CliRun.cs` per `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#D003`.
- [ ] The helper uses the `out ProcessExitConfiguration` parameter pattern per `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#D006`; no tuple or record return.
- [ ] The helper is pure — it does not call `GetFilePathResolver()` and does not pre-resolve the target file path per `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#T006`.
- [ ] `RunAsync(string, ...)`, `RunBufferedAsync(string, ...)`, and `RunPipedAsync(string, ...)` each call the helper and forward to the matching config-argument overload.
- [ ] The `redirectStandardOutput` flag is `false` for `RunAsync(string, ...)` and `true` for the buffered and piped string-argument overloads.
- [ ] Code-review notes at the helper document the D006 and T006 rationales.
- [ ] The public API surface is unchanged: the six public `Run*Async` methods keep their existing signatures; no new public types are introduced.
- [ ] `dotnet build src/CliInvoke.sln` succeeds on net8.0, net9.0, and net10.0.
- [ ] Existing tests under `tests/CliInvoke.Tests/` continue to pass.

## Dependencies

**Blocked by** - None - can start immediately
