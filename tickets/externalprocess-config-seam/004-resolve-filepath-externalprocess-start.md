---
title: Resolve file path at ExternalProcess.Start/StartAsync without mutating Configuration
classification: Independent
blocked_by: ["002-init-only-externalprocess-configuration", "003-update-processwrapper-ctor-resolved-fileinfo"]
parent: IMPLEMENTATION-externalprocess-config-seam.md
---

## Goal

Stop `ExternalProcess` from writing back to `Configuration.TargetFilePath` after the user submits it, while keeping the resolved file path accurate in `ProcessResult.ExecutedFilePath`. The path is resolved by `_filePathResolver` at Start time and passed directly to `ProcessWrapper`.

## What to build

In `src/CliInvoke/Processes/ExternalProcess.cs`:

1. **Drop the `Configuration.TargetFilePath = filePath.FullName;` write-back on line 127** (in `Start()`). Resolve `Configuration.TargetFilePath` via `_filePathResolver.ResolveFilePath(...)` and pass to a fresh `ProcessWrapper(Configuration, resolvedFilePath)`.
2. **Drop the equivalent `Configuration.TargetFilePath = filePath.FullName;` write-back on line 185** (in `StartAsync(ProcessConfiguration, CancellationToken)`). Resolve `configuration.TargetFilePath` (the **parameter**, not the field) and pass to `new ProcessWrapper(configuration, resolvedFilePath)`. Note: the current code resolves `Configuration.TargetFilePath` (field) instead of `configuration.TargetFilePath` (parameter) — fix this as part of T008.
3. **In `StartAsync(CancellationToken)`** (the parameterless overload at line 154), replace the `await StartAsync(Configuration, cancellationToken);` redirect with direct field-based resolution (per the blueprint's T008 note about indirection obscuring intent).
4. **Update all three ctors** (lines 29-39, 52-62, 70-80) to construct `ProcessWrapper` with the new `(configuration, resolvedFilePath)` shape. Drop the separate `configuration.ResourcePolicy` argument (the new ctor reads it from `configuration` itself).
5. **Add `<remarks>` blocks** above `Start`, `StartAsync(CancellationToken)`, and `StartAsync(ProcessConfiguration, CancellationToken)` stating "Configuration is not mutated; the resolved file path is returned via the result." plus `<see cref="ProcessResult.ExecutedFilePath"/>`.

## Size

- Files: 1

## Recommended Workflow

### Step 1 — Update Start() body

Where: `src/CliInvoke/Processes/ExternalProcess.cs`

- Remove `Configuration.TargetFilePath = filePath.FullName;` (line 127).
- Construct `new ProcessWrapper(Configuration, resolvedFilePath)` instead of `new ProcessWrapper(Configuration, Configuration.ResourcePolicy)`.
- Verify: `Start()` compiles; no in-place mutation of `Configuration`.

### Step 2 — Update StartAsync(ProcessConfiguration, CT) body

Where: `src/CliInvoke/Processes/ExternalProcess.cs`

- Resolve `configuration.TargetFilePath` (the parameter), NOT `Configuration.TargetFilePath` (the field) — fix the existing T008 bug.
- Remove `Configuration.TargetFilePath = filePath.FullName;` (line 185).
- Construct `new ProcessWrapper(configuration, resolvedFilePath)` instead of `new ProcessWrapper(configuration, configuration.ResourcePolicy)`.
- Verify: The parameter overload no longer reads or mutates the field `Configuration`; the field's `TargetFilePath` remains untouched.

### Step 3 — Replace parameterless StartAsync redirect with direct body

Where: `src/CliInvoke/Processes/ExternalProcess.cs`

- Replace `await StartAsync(Configuration, cancellationToken);` with the same body as the parameter overload, but reading `Configuration.TargetFilePath` (the field) instead of `configuration.TargetFilePath` (the parameter).
- Verify: Parameterless overload resolves the field; no redirect indirection remains.

### Step 4 — Update the three ctors

Where: `src/CliInvoke/Processes/ExternalProcess.cs`

- Replace `new ProcessWrapper(configuration, configuration.ResourcePolicy)` and `new ProcessWrapper(Configuration, ProcessResourcePolicy.Default)` with the new `(configuration, resolvedFilePath)` shape. For ctors that don't yet have a `resolvedFilePath`, pass a `resolvedFilePath = new FileInfo(Configuration.TargetFilePath)` so the rooted-path fast-path is used (rooted inputs return immediately via `FilePathResolver`); the public `Start`/`StartAsync` paths always re-resolve via the resolver.
- Verify: All three ctor call sites construct `ProcessWrapper` with the new two-argument shape.

### Step 5 — Add no-mutation remarks

Where: `src/CliInvoke/Processes/ExternalProcess.cs`

- Above each of `Start`, `StartAsync(CancellationToken)`, and `StartAsync(ProcessConfiguration, CancellationToken)`, add a `<remarks>` block with the one-line text "Configuration is not mutated; the resolved file path is returned via the result." plus `<see cref="ProcessResult.ExecutedFilePath"/>`.
- Verify: XML doc builds cleanly (`dotnet build src/CliInvoke.sln`).

### Step 6 — Build and run existing tests

Where: `src/CliInvoke.sln` and `tests/CliInvoke.Tests/`

- Run `dotnet build src/CliInvoke.sln` — should now succeed (TK003's ctor shape matches).
- Run `dotnet test tests/CliInvoke.Tests/` — existing tests should still pass; behavioural change is contained.
- Verify: Build clean; existing tests green.

## Context pointers

- Files:
  - `src/CliInvoke/Processes/ExternalProcess.cs` (target of change)
  - `src/CliInvoke/FilePathResolver.cs` (the `IFilePathResolver` implementation that short-circuits rooted paths per T001)
  - `src/CliInvoke.Core/Primitives/Results/ProcessResult.cs` (where `ExecutedFilePath` is declared — the `<see cref>` target)
- Domain terms: "Process Invocation Pipeline" (GLOSSARY.md — `ExternalProcess` is the direct-execution bypass pattern; the middleware pipeline applies to `IProcessInvoker`, not `ExternalProcess`)
- Ledger records:
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#T001` — always resolve at Start
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#T008` — `StartAsync(ProcessConfiguration, CT)` resolves parameter; field untouched
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#T012` — XML doc wording on Start/StartAsync
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#D002` — no-mutation contract
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#D003` — call-site documentation requirement

## Acceptance criteria

- [ ] No code path in `ExternalProcess.Start` or `ExternalProcess.StartAsync` writes to `Configuration.TargetFilePath`.
- [ ] `StartAsync(ProcessConfiguration, CT)` resolves `configuration.TargetFilePath` (the parameter), not `Configuration.TargetFilePath` (the field).
- [ ] All three `ExternalProcess` ctors construct `ProcessWrapper` with `(configuration, resolvedFilePath)`.
- [ ] Each of `Start`, `StartAsync(CancellationToken)`, `StartAsync(ProcessConfiguration, CT)` carries the `<remarks>` block with `<see cref="ProcessResult.ExecutedFilePath"/>`.
- [ ] `dotnet build src/CliInvoke.sln` succeeds.
- [ ] Existing tests in `tests/CliInvoke.Tests/` pass without modification.

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** — 002-init-only-externalprocess-configuration (same-file parallelization on `ExternalProcess.cs`; TK002 flips the `Configuration` property visibility, TK004 rewrites the method bodies), 003-update-processwrapper-ctor-resolved-fileinfo (`ExternalProcess` must pass the new `FileInfo` argument introduced by TK003).
