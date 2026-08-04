---
title: Add ProcessInvocationPipeline class in CliInvoke
classification: Independent
blocked_by: ["001-invocation-mode.md", "002-process-invocation-context.md"]
parent: docs/decisions/DECISIONS-CliInvoke-process-invocation-pipeline.md
---

## Goal

Introduce `ProcessInvocationPipeline` - the internal class in the `CliInvoke` assembly that owns the five-line execution skeleton (factory -> start -> wait or capture -> dispose). The four invoker modules collapse to wrappers that call this class.

## What to build

Create a new internal class `ProcessInvocationPipeline` in the `CliInvoke` namespace (assembly `CliInvoke`, not `CliInvoke.Core`). The class has the following shape -

- An internal constructor that takes a single parameter of type `IExternalProcessFactory` and stores it as a private read-only field.
- A single public method `Task<TResult> InvokeAsync<TResult>(ProcessInvocationContext ctx) where TResult : ProcessResult` that switches on `ctx.Mode` and dispatches to the matching capture path:
  - `Raw` -> `IExternalProcess.WaitForExitOrTimeoutAsync(ctx.CancellationToken)`.
  - `Buffered` -> `IExternalProcess.CaptureBufferedResultAsync(ctx.CancellationToken)`.
  - `Piped` -> `IExternalProcess.CapturePipedResultAsync(ctx.CancellationToken)`.
  - `FireAndForget` -> start the process (do not wait), then return a stub `ProcessResult` with the process identifier populated and other fields default.
- The pipeline is runner-blind - it does not know about runner configurations. The wrapper handles runner-config composition before constructing the context (see TK005 and TK006).
- The pipeline reads `ctx.CancellationToken` for cancellation - it is not a separate parameter.

The class body is approximately thirty lines. It is `internal` because the four invoker wrappers in `CliInvoke` and `CliInvoke.Specializations` are the only consumers; the public surface stays the existing `IProcessInvoker` contract.

## Size

- **Files** - 1

## Recommended Workflow

### Step 1 — Create the ProcessInvocationPipeline class

Where: `src/CliInvoke/ProcessInvocationPipeline.cs` (new file)

- Add the file header license block matching the surrounding `CliInvoke` files.
- Declare an internal class `ProcessInvocationPipeline` in the `CliInvoke` namespace.
- Add an internal constructor that accepts `IExternalProcessFactory` and stores it as a private read-only field.
- Add a public method `Task<TResult> InvokeAsync<TResult>(ProcessInvocationContext ctx) where TResult : ProcessResult`.
- Inside the method, instantiate `IExternalProcess` via the factory, start it, then dispatch on `ctx.Mode` to the appropriate capture method, disposing the process in a `finally` block.
- For `FireAndForget`, do not wait - return a stub `ProcessResult` after starting, then dispose. The stub has the process identifier populated; other fields hold their default values.

Verify: `dotnet build src/CliInvoke.sln` succeeds. The class is visible only inside the `CliInvoke` assembly; `CliInvoke.Specializations` and `CliInvoke.Tests` will not see it until TK004 grants `InternalsVisibleTo`.

## Context pointers

**Files** - `src/CliInvoke/ProcessInvocationPipeline.cs` (new) - the deliverable. `src/CliInvoke/ProcessInvoker.cs` - the existing 5-line body (lines 57-69, 96-108, 131-143) that this class consolidates. `src/CliInvoke.Specializations/Invokers/CmdProcessInvoker.cs` and `PowershellProcessInvoker.cs` - the other three duplicated bodies (this ticket does not touch them; TK005 and TK006 do).

**ADRs** - None in this repository today. The design rationale lives in the Decision Ledger.

**Domain terms** - Process Invocation Pipeline (the layered interceptor pattern this class implements; the glossary term is the inspiration for the type name).

**Ledger records** - `DECISIONS-CliInvoke-process-invocation-pipeline.md#T003` (narrow constructor with `ExternalProcessFactory`, generic return, `FireAndForget` returns a stub result). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D001` (the pipeline is internal; the public `IProcessInvoker` contract stays unchanged). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D003` (generic `InvokeAsync<TResult>(ctx)` with an internal switch on `ctx.Mode`; cancellation token is read from the context). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D004` (the pipeline lives in `CliInvoke`; `InternalsVisibleTo` for `CliInvoke.Specializations` is granted in TK004).

## Acceptance criteria

- [ ] A new internal class `ProcessInvocationPipeline` exists in the `CliInvoke` assembly.
- [ ] The constructor accepts `IExternalProcessFactory` and stores it as a private read-only field.
- [ ] A single public method `Task<TResult> InvokeAsync<TResult>(ProcessInvocationContext ctx) where TResult : ProcessResult` is exposed.
- [ ] The method switches on `ctx.Mode` and dispatches to `WaitForExitOrTimeoutAsync` for `Raw`, `CaptureBufferedResultAsync` for `Buffered`, `CapturePipedResultAsync` for `Piped`, and a start-without-wait stub for `FireAndForget`.
- [ ] Cancellation propagates from `ctx.CancellationToken` to the start and capture methods.
- [ ] The process is disposed in a `finally` block on every code path.
- [ ] The class is not visible to external consumers of the `CliInvoke` NuGet package (it is `internal`).

## Dependencies

**Blocked by** - `001-invocation-mode.md`, `002-process-invocation-context.md` - the pipeline consumes both types.
