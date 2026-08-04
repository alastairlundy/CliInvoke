---
title: Refactor ProcessInvoker to delegate to the pipeline
classification: Independent
blocked_by: ["003-process-invocation-pipeline.md"]
parent: docs/decisions/DECISIONS-CliInvoke-process-invocation-pipeline.md
---

## Goal

Collapse the three duplicated 5-line execution bodies in `ProcessInvoker` into 3-line wrappers that delegate to `ProcessInvocationPipeline`. The public `IProcessInvoker` contract stays unchanged.

## What to build

Modify `src/CliInvoke/ProcessInvoker.cs` as follows -

- Add a private read-only field `ProcessInvocationPipeline _pipeline`.
- In the existing constructor, after assigning `_externalProcessFactory`, construct `_pipeline = new ProcessInvocationPipeline(externalProcessFactory)`.
- In `ExecuteAsync`, replace the existing `factory -> start -> wait -> dispose` body with `var ctx = new ProcessInvocationContext(processConfiguration, processExitConfiguration ?? ProcessExitConfiguration.Default, InvocationMode.Raw, cancellationToken); return await _pipeline.InvokeAsync<ProcessResult>(ctx);`. The method stays `async`.
- In `ExecuteBufferedAsync`, same pattern with `InvocationMode.Buffered` and the typed return `BufferedProcessResult`.
- In `ExecutePipedAsync`, same pattern with `InvocationMode.Piped` and the typed return `PipedProcessResult`.
- The XML doc comments, attributes (`[UnsupportedOSPlatform]`), and exception documentation stay unchanged. Only the method bodies change.

## Size

- **Files** - 1

## Recommended Workflow

### Step 1 — Add the pipeline field and constructor wiring

Where: `src/CliInvoke/ProcessInvoker.cs`

- Add `using CliInvoke.Core;` if not present (for `ProcessInvocationContext` and `InvocationMode`).
- Add `private readonly ProcessInvocationPipeline _pipeline;` next to the existing `_externalProcessFactory` field.
- In the constructor, after `_externalProcessFactory = externalProcessFactory;`, add `_pipeline = new ProcessInvocationPipeline(externalProcessFactory);`.

Verify: `dotnet build src/CliInvoke.sln` succeeds. The new field is assigned in the constructor; no other change to behaviour yet.

### Step 2 — Replace the three Execute*Async bodies

Where: `src/CliInvoke/ProcessInvoker.cs`

- For `ExecuteAsync`, replace the body with the three-line wrapper using `InvocationMode.Raw` and `ProcessResult`.
- For `ExecuteBufferedAsync`, same pattern with `InvocationMode.Buffered` and `BufferedProcessResult`.
- For `ExecutePipedAsync`, same pattern with `InvocationMode.Piped` and `PipedProcessResult`.
- Keep the `async` keyword on each method. Keep the `CancellationToken` parameter as part of the context construction (do not remove it from the signature).

Verify: `dotnet build src/CliInvoke.sln` succeeds. The three methods are now 3-line wrappers (context construction + `return await _pipeline.InvokeAsync<T>(ctx);`). The XML doc comments are preserved.

### Step 3 — Verify the integration tests still pass

Where: `tests/CliInvoke.Tests/`

- Run `dotnet test tests/CliInvoke.Tests/CliInvoke.Tests.csproj --framework net10.0` from the repository root.
- Confirm the existing `CliRunTests` (which drive `IExternalProcessFactory` through `CliRun`) still pass.

Verify: All existing tests pass. The behaviour of `ProcessInvoker` from a caller's perspective is unchanged.

## Context pointers

**Files** - `src/CliInvoke/ProcessInvoker.cs` (modify) - the deliverable. `src/CliInvoke/ProcessInvocationPipeline.cs` - the dependency from TK003.

**Domain terms** - Resource-Owning Type (the `IExternalProcess` is one; the pipeline's `finally` block in TK003 owns the disposal, removing the responsibility from this class).

**Ledger records** - `DECISIONS-CliInvoke-process-invocation-pipeline.md#T004` (field-cached pipeline, async methods, three Execute methods collapse to three-line wrappers). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D001` (the wrapper delegates to the pipeline; the public `IProcessInvoker` contract is unchanged). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D002` (the `ProcessInvocationContext` is built with the required fields). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D003` (the single `InvokeAsync<TResult>(ctx)` is the call site).

## Acceptance criteria

- [ ] `ProcessInvoker` holds a `ProcessInvocationPipeline _pipeline` field constructed in the constructor.
- [ ] `ExecuteAsync` builds a context with `InvocationMode.Raw` and returns `await _pipeline.InvokeAsync<ProcessResult>(ctx)`.
- [ ] `ExecuteBufferedAsync` builds a context with `InvocationMode.Buffered` and returns `await _pipeline.InvokeAsync<BufferedProcessResult>(ctx)`.
- [ ] `ExecutePipedAsync` builds a context with `InvocationMode.Piped` and returns `await _pipeline.InvokeAsync<PipedProcessResult>(ctx)`.
- [ ] Each method remains `async` and keeps its existing signature, attributes, and XML documentation.
- [ ] The existing `CliRunTests` continue to pass without modification.

## Dependencies

**Blocked by** - `003-process-invocation-pipeline.md` - the pipeline class must exist before the wrapper can use it.
