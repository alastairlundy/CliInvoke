# CliInvoke — Process Invocation Pipeline Decisions

This ledger records the design decisions for the F1 deepening: collapsing the
"execute a process" skeleton currently duplicated 12 times across four
invoker modules. Each `Dxxx` entry is stable and cross-cited as
`DECISIONS-CliInvoke-process-invocation-pipeline.md#Dxxx`.

Context: the F1 candidate is documented in
`docs/agents/architecture-review-20260711-213732.html` (or the most recent
review in `%TEMP%/architecture-review-*.html`).

Domain vocabulary: see `CONTEXT.md` at the repo root — *Resource-Owning
Type*, *Process Invocation Pipeline*, *Process Invocation Context*.

## Records

### [D001] — where the pipeline module sits

- **Resolved Answer**: "Option 2 — Pipeline is internal; IProcessInvoker stays public."
- **Normalized Requirement**: A `ProcessInvocationPipeline` module shall own the 5-line execution skeleton (factory → start → wait/capture → dispose); the four invoker modules (`ProcessInvoker`, `CliRun`, `CmdProcessInvoker`, `PowershellProcessInvoker`) shall each become a 2-line wrapper that delegates to the pipeline; `IProcessInvoker` shall remain the public DI-friendly entry point with its current 3-method shape unchanged.
- **Constraints**: No breaking change to the public `IProcessInvoker` contract. The duplication of 12 near-identical bodies across the four invoker modules shall collapse into one pipeline module. If a future capture mode is added (e.g. `FireAndForget`, streaming), the public `IProcessInvoker` shape may need to grow — this is the documented forward risk of keeping the public shape.

### [D002] — what shape the pipeline's input takes

- **Resolved Answer**: "Option 1 — Single `ProcessInvocationContext` input."
- **Normalized Requirement**: The pipeline shall accept a single `ProcessInvocationContext` parameter that holds the effective `ProcessConfiguration`, `ProcessExitConfiguration`, and capture mode; the pipeline mutates the context as state flows through, and the populated result is read from the same context after the call.
- **Constraints**: A `ProcessInvocationContext` type shall be introduced in `CliInvoke.Core`; until the pipeline gains a mutating interceptor stage, the context is a thin wrapper around three existing parameters, which is acceptable because the type matches the glossary term.

### [D003] — how the pipeline dispatches by capture mode

- **Resolved Answer**: "Option 1 — Generic method, internal switch on `ctx.CaptureMode`."
- **Normalized Requirement**: The pipeline shall expose one method `Task<TResult> InvokeAsync<TResult>(ProcessInvocationContext ctx, CancellationToken ct) where TResult : ProcessResult`; the body shall switch on `ctx.CaptureMode` and return the typed result. The four `Execute*Async` invoker wrappers shall each call this single method.
- **Constraints**: A `switch` on `ctx.CaptureMode` lives inside the pipeline. Adding a fifth capture mode requires editing the switch. Until a sixth mode is added, the switch is the honest representation of the small domain.

### [D004] — where the pipeline module lives

- **Resolved Answer**: "Option 1 — Pipeline in `CliInvoke`, `InternalsVisibleTo` for `CliInvoke.Specializations`."
- **Normalized Requirement**: The `ProcessInvocationPipeline` class shall be `internal` in the `CliInvoke` assembly; the `CliInvoke.Specializations` assembly shall have `InternalsVisibleTo` access to `CliInvoke`'s internals. The pipeline shall consume the concrete `ExternalProcessFactory` (not the `IExternalProcessFactory` abstraction).
- **Constraints**: Adding a future specialization package outside `CliInvoke.Specializations` requires updating the `InternalsVisibleTo` list. The pipeline does not become a public type in this deepening pass.

### [D005] — where the runner-config compose happens

- **Resolved Answer**: "Option 1 — Compose inside the invoker wrapper, before the pipeline call."
- **Normalized Requirement**: Each specialization wrapper (e.g. `CmdProcessInvoker.ExecuteAsync`) shall call `_runnerConfigurationFactory.CreateRunnerConfiguration(processConfiguration)` and pass the resulting effective `ProcessConfiguration` into a `ProcessInvocationContext`; the pipeline shall receive a context whose `ProcessConfiguration` is already runner-wrapped and shall not know about runner configs.
- **Constraints**: A future runner that needs process-runtime state (e.g. `Process.Id` after start) cannot be expressed in this shape and would require the compose to move into the pipeline via a richer hook.

### [D006] — how CliRun participates in F1

- **Resolved Answer**: TBD
- **Normalized Requirement**: TBD
- **Resolved Answer**: "Option A — CliRun joins F1, static pipeline instance."
- **Normalized Requirement**: CliRun shall hold a `private static ProcessInvocationPipeline? _pipeline` field; the lazy-initialisation shall follow the same double-check locking pattern as `_filePathResolver` (T014); `GetExternalProcessFactory()` shall be the single source of the factory for the pipeline; the 3 `ProcessConfiguration` overloads shall become 2-line wrappers (`new ProcessInvocationContext(...); return await _pipeline.InvokeAsync<T>(ctx, ct)`); the 3 string overloads shall delegate to their `ProcessConfiguration` counterpart after building config + exitConfig.
- **Constraints**: Tests that swap `_externalProcessFactory` via `UseExternalProcessFactory` must also sync `_pipeline` or the new factory is silently ignored. The T014 lock-discipline pattern for `_filePathResolver` is duplicated for `_pipeline`.
