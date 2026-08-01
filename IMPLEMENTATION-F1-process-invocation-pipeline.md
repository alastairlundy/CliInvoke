# Implementation Plan — F1: Process Invocation Pipeline

**Scope Binding**
- **Linked Spec**: `docs/decisions/DECISIONS-CliInvoke-process-invocation-pipeline.md`
- **Decision Ledger**: `docs/decisions/DECISIONS-CliInvoke-process-invocation-pipeline.md` (records D001–D007, T001–T008)
- This plan is valid only for the linked spec. Do not use it as a context pointer for any other feature.

---

## New files

### `src/CliInvoke.Core/InvocationMode.cs`

- `public enum InvocationMode { Raw, Buffered, Piped, FireAndForget }` — T001

### `src/CliInvoke.Core/ProcessInvocationContext.cs`

- Public class with 5 fields: `Configuration` (`ProcessConfiguration`, required), `ExitConfiguration` (`ProcessExitConfiguration`, required), `Mode` (`InvocationMode`, required), `CancellationToken` (`CancellationToken`, optional), `Result` (`ProcessResult?`, settable) — T002
- Traditional constructor with 3 positional params + optional `CancellationToken` — T002

### `src/CliInvoke/ProcessInvocationPipeline.cs`

- Internal class in `CliInvoke` — D004
- Constructor takes `IExternalProcessFactory` — T003
- `Task<TResult> InvokeAsync<TResult>(ProcessInvocationContext ctx) where TResult : ProcessResult` — D003
- Switch on `ctx.Mode`: `Raw` → `WaitForExitOrTimeoutAsync`; `Buffered` → `CaptureBufferedResultAsync`; `Piped` → `CapturePipedResultAsync`; `FireAndForget` → start + return stub `ProcessResult` with process ID populated — T003
- Pipeline reads `ctx.CancellationToken` for cancellation — D003

### `tests/CliInvoke.Tests/PipelineDispatchTests.cs`

- Internal dispatch tests via `InternalsVisibleTo` — D007, T008
- Mock `IExternalProcessFactory`; construct pipeline directly
- Assert every `InvocationMode` arm returns the correct result type
- Assert `FireAndForget` stub has process ID populated
- Assert cancellation propagation

---

## Modified files

### `src/CliInvoke/AssemblyInfo.cs`

- Add `[assembly: InternalsVisibleTo("CliInvoke.Specializations")]` — D004, T007
- Add `[assembly: InternalsVisibleTo("CliInvoke.Tests")]` — D007, T007

### `src/CliInvoke/ProcessInvoker.cs`

- Add `private readonly ProcessInvocationPipeline _pipeline` field — T004
- Constructor: `_pipeline = new ProcessInvocationPipeline(externalProcessFactory)` — T004
- `ExecuteAsync`: build context with `InvocationMode.Raw`, `return await _pipeline.InvokeAsync<ProcessResult>(ctx)` — T004
- `ExecuteBufferedAsync`: build context with `InvocationMode.Buffered`, `return await _pipeline.InvokeAsync<BufferedProcessResult>(ctx)` — T004
- `ExecutePipedAsync`: build context with `InvocationMode.Piped`, `return await _pipeline.InvokeAsync<PipedProcessResult>(ctx)` — T004
- Methods retain `async` keyword — T004

### `src/CliInvoke.Specializations/Invokers/CmdProcessInvoker.cs`

- Add `private readonly ProcessInvocationPipeline _pipeline` field — T005
- Constructor: `_pipeline = new ProcessInvocationPipeline(externalProcessFactory)` — T005
- Each `Execute*Async`: (1) `ThrowIfUnsupported()`, (2) runner-config compose, (3) build context, (4) `return await _pipeline.InvokeAsync<T>(ctx)` — T005

### `src/CliInvoke.Specializations/Invokers/PowershellProcessInvoker.cs`

- Same pattern as `CmdProcessInvoker` — T005

### `src/CliInvoke/Extensions/CliRun.cs`

- Add `private static ProcessInvocationPipeline? _pipeline` field — D006, T006
- `UseExternalProcessFactory`: set `_externalProcessFactory` and clear `_pipeline` under `_syncRoot` lock — T006
- `GetPipeline()`: double-check lock, construct pipeline with `GetExternalProcessFactory()` — T006
- 3 `ProcessConfiguration` overloads: 2-line wrappers (`var ctx = new ProcessInvocationContext(...); return await GetPipeline().InvokeAsync<T>(ctx)`) — T006
- 3 string overloads: unchanged (build config + delegate to `ProcessConfiguration` overload)

### `tests/CliInvoke.Tests/ProcessInvokerIntegrationTests.cs`

- Integration tests driving `IProcessInvoker` against a real process — D007, T008
- Cover `ExecuteAsync`, `ExecuteBufferedAsync`, `ExecutePipedAsync`
- Cover cancellation path

---

## Dependency order

1. T001 (InvocationMode enum) — no dependencies
2. T002 (ProcessInvocationContext) — depends on T001
3. T003 (ProcessInvocationPipeline) — depends on T001, T002
4. T007 (InternalsVisibleTo) — no dependencies
5. T004 (ProcessInvoker wrapper) — depends on T003
6. T005 (Specialization wrappers) — depends on T003
7. T006 (CliRun wiring) — depends on T003
8. T008 (Tests) — depends on T003, T004, T005

---

## Ledger Reference

| Record | Summary |
|---|---|
| D001 | Pipeline internal; `IProcessInvoker` stays public |
| D002 | `ProcessInvocationContext` with 5 fields in Core |
| D003 | Generic `InvokeAsync<TResult>(ctx)` with internal switch on `ctx.Mode` |
| D004 | Pipeline in `CliInvoke`, `InternalsVisibleTo` for Specializations |
| D005 | Runner-config compose in wrapper, pipeline runner-blind |
| D006 | CliRun joins F1 with static pipeline instance, T014 discipline |
| D007 | Mixed test surface (dispatch + integration) |
| T001 | `InvocationMode` plain enum with 4 values |
| T002 | Traditional constructor with 3 positional params + optional CancellationToken |
| T003 | Pipeline class with FireAndForget stub result |
| T004 | ProcessInvoker field-cached pipeline, async methods |
| T005 | Specialization wrappers with runner-config compose |
| T006 | CliRun cached pipeline with explicit invalidation |
| T007 | Both `InternalsVisibleTo` grants scoped |
| T008 | One test class per concern |
