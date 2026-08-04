---
title: Wire CliRun static pipeline with cache invalidation
classification: Independent
blocked_by: ["003-process-invocation-pipeline.md", "004-internals-visible-to.md"]
parent: docs/decisions/DECISIONS-CliInvoke-process-invocation-pipeline.md
---

## Goal

Make `CliRun` participate in the F1 deepening by holding a static `ProcessInvocationPipeline` instance, lazily constructed through the same double-check locking pattern as `_filePathResolver` (T014 lock discipline), and explicitly invalidated when `UseExternalProcessFactory` swaps the factory.

## What to build

Modify `src/CliInvoke/Extensions/CliRun.cs` as follows -

- Add a private static field `private static ProcessInvocationPipeline? _pipeline;` next to the existing `_externalProcessFactory` and `_filePathResolver` static fields.
- Add a private static helper `GetPipeline()` that uses the double-check pattern under the existing `_syncRoot` lock. The first time it is called, it constructs `_pipeline = new ProcessInvocationPipeline(GetExternalProcessFactory())`. The lock is held only across the read-and-assign; the pipeline construction itself happens inside the lock to keep the invariant simple.
- Modify `UseExternalProcessFactory(IExternalProcessFactory factory)` to set the factory **and** clear `_pipeline` under the same `_syncRoot` lock. This prevents a window where `_pipeline` is non-null but `_externalProcessFactory` has just been swapped - the next `GetPipeline()` call will see `_pipeline == null` and rebuild with the new factory.
- The three `ProcessConfiguration` overloads (`RunAsync(ProcessConfiguration, ...)`, `RunBufferedAsync(ProcessConfiguration, ...)`, `RunPipedAsync(ProcessConfiguration, ...)`) become 2-line wrappers - build the context, then `return await GetPipeline().InvokeAsync<T>(ctx);`.
- The three `string` overloads are unchanged in their internal logic - they continue to call `BuildStringArgsConfig` and then delegate to the corresponding `ProcessConfiguration` overload (which now routes through the pipeline).
- The two `FireAndForget` overloads stay unchanged (they continue to call `GetExternalProcessFactory()` directly - the F1 plan does not route `FireAndForget` through the pipeline).
- The existing `RunInternalAsync<T>` private helper is removed once the three `ProcessConfiguration` overloads route through the pipeline - the helper is no longer reachable.

## Size

- **Files** - 1

## Recommended Workflow

### Step 1 — Add the static pipeline field and the double-check GetPipeline helper

Where: `src/CliInvoke/Extensions/CliRun.cs`

- Add `private static ProcessInvocationPipeline? _pipeline;` next to the existing static fields.
- Add a private static `GetPipeline()` method that uses the same `_syncRoot`-guarded double-check pattern as `GetFilePathResolver()` (lines 60-71 today). Inside the locked block, construct `_pipeline = new ProcessInvocationPipeline(GetExternalProcessFactory())`.

Verify: `dotnet build src/CliInvoke.sln` succeeds. `GetPipeline()` returns a non-null instance on first call and the same instance on subsequent calls until invalidation.

### Step 2 — Invalidate the pipeline cache in UseExternalProcessFactory

Where: `src/CliInvoke/Extensions/CliRun.cs`

- In `UseExternalProcessFactory(IExternalProcessFactory factory)`, wrap the assignment to `_externalProcessFactory` in a `lock (_syncRoot)` block. Inside the same lock, set `_pipeline = null`.
- This change is intentional - it synchronises the factory swap with the cache invalidation so a concurrent `GetPipeline()` call cannot observe a stale factory in a non-null pipeline.

Verify: `dotnet build src/CliInvoke.sln` succeeds. The new test `DefaultFactory_ReEvaluatedOnEachCall` in `CliRunTests` (lines 155-189 today) continues to pass; the test's three factory swaps each trigger a pipeline rebuild.

### Step 3 — Replace the three ProcessConfiguration overloads and remove RunInternalAsync

Where: `src/CliInvoke/Extensions/CliRun.cs`

- Replace the body of `RunAsync(ProcessConfiguration, ...)` with a 2-line wrapper: build the context with `InvocationMode.Raw` and `return await GetPipeline().InvokeAsync<ProcessResult>(ctx);`.
- Replace the body of `RunBufferedAsync(ProcessConfiguration, ...)` similarly with `InvocationMode.Buffered` and `BufferedProcessResult`.
- Replace the body of `RunPipedAsync(ProcessConfiguration, ...)` similarly with `InvocationMode.Piped` and `PipedProcessResult`.
- Remove the now-unreachable `RunInternalAsync<T>` private helper. The F1 follow-up comment at line 105 is also removed.

Verify: `dotnet build src/CliInvoke.sln` succeeds. The three `string` overloads continue to compile because they delegate to the `ProcessConfiguration` overloads (now thin wrappers).

### Step 4 — Run the CliRun tests

Where: `tests/CliInvoke.Tests/`

- Run `dotnet test tests/CliInvoke.Tests/CliInvoke.Tests.csproj --framework net10.0` from the repository root.
- Confirm all eleven existing `CliRunTests` pass without modification, including `DefaultFactory_ReEvaluatedOnEachCall` (verifies the cache invalidation) and the four `FireAndForget*` tests (verifies that `FireAndForget` still bypasses the pipeline).

Verify: All existing `CliRunTests` pass. No test in the file needs to change.

## Context pointers

**Files** - `src/CliInvoke/Extensions/CliRun.cs` (modify) - the deliverable. `src/CliInvoke/ProcessInvocationPipeline.cs` - the dependency from TK003. `tests/CliInvoke.Tests/CliRunTests.cs` - the existing test surface that exercises `CliRun` (especially `DefaultFactory_ReEvaluatedOnEachCall` and `FireAndForget_DisposesProcessOnStartFailure`); the test file is not modified but it must continue to pass.

**Domain terms** - Resource-Owning Type (the `IExternalProcess` and the `using` blocks around `ProcessConfiguration` and `exitConfiguration` qualify; the pipeline's `finally` block in TK003 now owns the process disposal for the three routed paths).

**Ledger records** - `DECISIONS-CliInvoke-process-invocation-pipeline.md#T006` (cache the pipeline, invalidate when the factory changes; the same `_syncRoot` lock guards both sides). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D006` (CliRun joins F1 with a static pipeline instance and the T014 lock discipline). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D001` (CliRun wraps the internal pipeline). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D002` (the context is built from the configuration and exit configuration). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D003` (the single `InvokeAsync<TResult>(ctx)` is the call site).

## Acceptance criteria

- [ ] `CliRun` holds a `private static ProcessInvocationPipeline? _pipeline` field.
- [ ] `GetPipeline()` constructs the pipeline on first call using the same double-check `_syncRoot` pattern as `GetFilePathResolver()`.
- [ ] `UseExternalProcessFactory(factory)` sets the factory **and** clears `_pipeline` under the same `_syncRoot` lock.
- [ ] The three `ProcessConfiguration` overloads are 2-line wrappers that call `GetPipeline().InvokeAsync<T>(ctx)`.
- [ ] The three `string` overloads are unchanged - they continue to build the configuration and delegate to the `ProcessConfiguration` overloads.
- [ ] The two `FireAndForget` overloads are unchanged - they do not route through the pipeline.
- [ ] The `RunInternalAsync<T>` private helper is removed.
- [ ] All existing `CliRunTests` pass without modification.

## Dependencies

**Blocked by** - `003-process-invocation-pipeline.md` (the pipeline class must exist) and `004-internals-visible-to.md` (the `CliInvoke` assembly is the pipeline's home, and although `CliRun` lives in the same assembly, the test surface in TK008 also depends on this grant).
