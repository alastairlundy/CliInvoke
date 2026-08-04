---
title: Refactor CmdProcessInvoker and PowershellProcessInvoker to delegate to the pipeline
classification: Independent
blocked_by: ["003-process-invocation-pipeline.md", "004-internals-visible-to.md"]
parent: docs/decisions/DECISIONS-process-invocation-pipeline.md
---

## Goal

Collapse the three duplicated 5-line execution bodies in `CmdProcessInvoker` and `PowershellProcessInvoker` into thin wrappers that compose the runner configuration, then delegate to `ProcessInvocationPipeline`. The pipeline stays runner-blind.

## What to build

Modify two files in `src/CliInvoke.Specializations/Invokers/`:

### `CmdProcessInvoker.cs`

- Add a private read-only field `ProcessInvocationPipeline _pipeline`.
- In the existing constructor, after assigning `_externalProcessFactory`, construct `_pipeline = new ProcessInvocationPipeline(externalProcessFactory);`.
- In each of `ExecuteAsync`, `ExecuteBufferedAsync`, and `ExecutePipedAsync`:
  - Call `ThrowIfUnsupported()` first (unchanged).
  - Compute `using ProcessConfiguration runnerConfiguration = _runnerConfigurationFactory.CreateRunnerConfiguration(processConfiguration, <platform-specific config>);` (the platform-specific config preserves the existing per-method argument shape - `CmdProcessConfiguration` is built from the existing per-mode arguments).
  - Build the context using the **original** `processConfiguration` (the runner configuration is built for side effects; the pipeline still receives the user's `processConfiguration`).
  - Return `await _pipeline.InvokeAsync<T>(ctx)` with the matching `T` (`ProcessResult`, `BufferedProcessResult`, or `PipedProcessResult`).

### `PowershellProcessInvoker.cs`

- Same field, constructor wiring, and method-body shape as `CmdProcessInvoker`.
- The `GetPowershellProcessConfiguration(bool)` private helper stays unchanged - it continues to produce the per-mode `PowershellProcessConfiguration` consumed by `_runnerConfigurationFactory.CreateRunnerConfiguration`.

The pipeline is runner-blind - it does not know about runner configurations. The two extra lines per method (the `using runnerConfiguration = ...` block) are the runner-config compose step and are intentional, not duplication to be removed.

## Size

- **Files** - 2

## Recommended Workflow

### Step 1 — Wire the pipeline field in CmdProcessInvoker

Where: `src/CliInvoke.Specializations/Invokers/CmdProcessInvoker.cs`

- Add the `ProcessInvocationPipeline _pipeline` field and the constructor assignment.
- In each of the three `Execute*Async` methods, keep the `ThrowIfUnsupported()` call, the `using runnerConfiguration = _runnerConfigurationFactory.CreateRunnerConfiguration(...)` block (with the same per-mode `CmdProcessConfiguration` argument as today), and replace the `factory -> start -> capture -> dispose` body with a context construction and `_pipeline.InvokeAsync<T>(ctx)` call.

Verify: `dotnet build src/CliInvoke.sln` succeeds. The three methods are 4-line wrappers (platform check, runner compose, context construction, return).

### Step 2 — Wire the pipeline field in PowershellProcessInvoker

Where: `src/CliInvoke.Specializations/Invokers/PowershellProcessInvoker.cs`

- Add the `ProcessInvocationPipeline _pipeline` field and the constructor assignment.
- In each of the three `Execute*Async` methods, keep the `ThrowIfUnsupported()` call, the `using runnerConfiguration = _runnerConfigurationFactory.CreateRunnerConfiguration(processConfiguration, GetPowershellProcessConfiguration(...))` block (the existing `GetPowershellProcessConfiguration` helper stays), and replace the `factory -> start -> capture -> dispose` body with a context construction and `_pipeline.InvokeAsync<T>(ctx)` call.

Verify: `dotnet build src/CliInvoke.sln` succeeds. The `GetPowershellProcessConfiguration` helper is unchanged.

### Step 3 — Verify the specialisation tests still pass

Where: `tests/CliInvoke.Specializations.Tests/`

- Run `dotnet test tests/CliInvoke.Specializations.Tests/CliInvoke.Specializations.Tests.csproj` from the repository root on a Windows host (these tests require the `cmd.exe` runner).
- Confirm the existing `CmdInvokerTests` and any PowerShell invoker tests pass.

Verify: All existing specialisation tests pass. The behaviour from a caller's perspective is unchanged.

## Context pointers

**Files** - `src/CliInvoke.Specializations/Invokers/CmdProcessInvoker.cs` (modify) and `PowershellProcessInvoker.cs` (modify) - the deliverables. `src/CliInvoke/ProcessInvocationPipeline.cs` - the dependency from TK003. `src/CliInvoke/ProcessInvoker.cs` - the simpler sibling wrapper from TK005 (same pattern, no runner compose).

**Domain terms** - Resource-Owning Type (the `IExternalProcess` and the `using runnerConfiguration` block both qualify; the pipeline's `finally` block in TK003 owns the process disposal).

**Ledger records** - `DECISIONS-CliInvoke-process-invocation-pipeline.md#T005` (field-cached pipeline, runner-config compose in each method, pipeline stays runner-blind). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D005` (compose happens in the wrapper, not the pipeline). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D001` (the wrappers delegate to the internal pipeline; the public `IProcessInvoker` contract is unchanged). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D004` (the `CliInvoke.Specializations` assembly reaches the pipeline through the `InternalsVisibleTo` grant in TK004).

## Acceptance criteria

- [ ] Both `CmdProcessInvoker` and `PowershellProcessInvoker` hold a `ProcessInvocationPipeline _pipeline` field constructed in the constructor.
- [ ] Each of the three `Execute*Async` methods in both invokers calls `ThrowIfUnsupported()` first.
- [ ] Each of the three `Execute*Async` methods in both invokers composes the runner configuration via `_runnerConfigurationFactory.CreateRunnerConfiguration(...)` using the same per-mode argument shape as today.
- [ ] Each of the three `Execute*Async` methods in both invokers builds a `ProcessInvocationContext` and returns `await _pipeline.InvokeAsync<T>(ctx)`.
- [ ] The pipeline class is not modified - it remains runner-blind.
- [ ] Existing specialisation tests pass on a Windows host.

## Dependencies

**Blocked by** - `003-process-invocation-pipeline.md` (the pipeline class must exist) and `004-internals-visible-to.md` (the `CliInvoke.Specializations` assembly must be granted access to the internal pipeline).
