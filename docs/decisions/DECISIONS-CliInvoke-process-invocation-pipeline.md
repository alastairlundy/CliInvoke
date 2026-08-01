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
- **Normalized Requirement**: The `ProcessInvocationContext` type shall carry five fields:
  - `Configuration` (`ProcessConfiguration`, `required init`)
  - `ExitConfiguration` (`ProcessExitConfiguration`, `required init`)
  - `Mode` (`InvocationMode`, `required init`)
  - `CancellationToken` (`CancellationToken`, `init`)
  - `Result` (`ProcessResult?`, nullable, set by the pipeline after leaf execution)
  The pipeline shall accept a single `ProcessInvocationContext` and mutate `Result` during execution; the cancellation token reads from the context, not from a separate parameter.
- **Constraints**: `ProcessInvocationContext` shall live in `CliInvoke.Core`. `InvocationMode` is an enum with values `Raw`, `Buffered`, `Piped`, `FireAndForget`. Fields that are `required` are set at construction; `CancellationToken` and `Result` are optional/init-only. The type is shared between F1 and the future middleware system.

### [D003] — how the pipeline dispatches by invocation mode

- **Resolved Answer**: "Option 1 — Generic method, internal switch on `ctx.Mode`."
- **Normalized Requirement**: The pipeline shall expose one method `Task<TResult> InvokeAsync<TResult>(ProcessInvocationContext ctx) where TResult : ProcessResult`; the body shall switch on `ctx.Mode` and return the typed result. The four `Execute*Async` invoker wrappers shall each call this single method. The pipeline reads `ctx.CancellationToken` for cancellation — it is not a separate parameter.
- **Constraints**: A `switch` on `ctx.Mode` lives inside the pipeline. Adding a fifth `InvocationMode` value requires editing the switch. Until a sixth mode is added, the switch is the honest representation of the small domain.

### [D004] — where the pipeline module lives

- **Resolved Answer**: "Option 1 — Pipeline in `CliInvoke`, `InternalsVisibleTo` for `CliInvoke.Specializations`."
- **Normalized Requirement**: The `ProcessInvocationPipeline` class shall be `internal` in the `CliInvoke` assembly; the `CliInvoke.Specializations` assembly shall have `InternalsVisibleTo` access to `CliInvoke`'s internals. The pipeline shall consume the concrete `ExternalProcessFactory` (not the `IExternalProcessFactory` abstraction).
- **Constraints**: Adding a future specialization package outside `CliInvoke.Specializations` requires updating the `InternalsVisibleTo` list. The pipeline does not become a public type in this deepening pass.

### [D005] — where the runner-config compose happens

- **Resolved Answer**: "Option 1 — Compose inside the invoker wrapper, before the pipeline call."
- **Normalized Requirement**: Each specialization wrapper (e.g. `CmdProcessInvoker.ExecuteAsync`) shall call `_runnerConfigurationFactory.CreateRunnerConfiguration(processConfiguration)` and pass the resulting effective `ProcessConfiguration` into a `ProcessInvocationContext`; the pipeline shall receive a context whose `ProcessConfiguration` is already runner-wrapped and shall not know about runner configs.
- **Constraints**: A future runner that needs process-runtime state (e.g. `Process.Id` after start) cannot be expressed in this shape and would require the compose to move into the pipeline via a richer hook.

### [D006] — how CliRun participates in F1

- **Resolved Answer**: "Option A — CliRun joins F1, static pipeline instance."
- **Normalized Requirement**: CliRun shall hold a `private static ProcessInvocationPipeline? _pipeline` field; the lazy-initialisation shall follow the same double-check locking pattern as `_filePathResolver` (T014); `GetExternalProcessFactory()` shall be the single source of the factory for the pipeline; the 3 `ProcessConfiguration` overloads shall become 2-line wrappers (`new ProcessInvocationContext(...); return await _pipeline.InvokeAsync<T>(ctx)`); the 3 string overloads shall delegate to their `ProcessConfiguration` counterpart after building config + exitConfig.
- **Constraints**: Tests that swap `_externalProcessFactory` via `UseExternalProcessFactory` must also sync `_pipeline` or the new factory is silently ignored. The T014 lock-discipline pattern for `_filePathResolver` is duplicated for `_pipeline`.

### [D007] — how the pipeline gets tested

- **Resolved Answer**: "Option 3 — Mixed surface."
- **Normalized Requirement**: `CliInvoke` shall grant `InternalsVisibleTo("CliInvoke.Tests")` for a focused pipeline-dispatch test class (switch arms, factory interaction, mode-to-result-type mapping). Existing invoker tests shall remain the integration surface running against a real or near-real `IExternalProcess`. `ProcessInvocationContext` tests need no `InternalsVisibleTo` — the type is public in `Core`. 
- **Constraints**: Both test surfaces run in the same CI gate. The dispatch tests cover every `InvocationMode` arm. The integration tests cover the full start/capture/dispose lifecycle against a real OS process (e.g. `which dotnet`, `echo`). `InternalsVisibleTo` is scoped to `CliInvoke.Tests` only — not granted for `CliInvoke.Specializations.Tests` unless individually justified.

## Technical Decisions

### [T001] — InvocationMode enum shape

- **Driver**: None.
- **Resolved Answer**: "Option 1 — Plain enum in the same namespace as the pipeline."
- **Normalized Requirement**: A `public enum InvocationMode` with values `Raw`, `Buffered`, `Piped`, `FireAndForget` shall be placed in `CliInvoke.Core` alongside `ProcessInvocationContext`. No attributes, no wrapper struct.
- **Constraints**: The enum is the dispatch key for the pipeline switch (D003). A C# `switch` exhaustiveness check catches missing arms at compile time when a switch expression is used.
- **Cites**: D002 (enum values), D004 (type lives in Core)

### [T002] — ProcessInvocationContext constructor shape

- **Driver**: "I want a constructor that works both for the Middleware plan and for the F1 plan."
- **Resolved Answer**: "Option 1 — Traditional constructor."
- **Normalized Requirement**: `ProcessInvocationContext` shall have a traditional constructor with three positional parameters (`ProcessConfiguration`, `ProcessExitConfiguration`, `InvocationMode`) and an optional `CancellationToken` with default value. `Result` shall be a `{ get; set; }` property.
- **Constraints**: The construction syntax works for both F1 (`new(config, exitConfig, InvocationMode.Raw)`) and Middleware (`new(config, exitConfig, InvocationMode.Buffered, cancellationToken: token)`). The codebase currently uses no primary constructors.
- **Cites**: D002 (five fields), D003 (pipeline reads CancellationToken from context)

### [T003] — ProcessInvocationPipeline class shape

- **Driver**: "I want the class to be intuitive to use internally and to have sensible method signatures."
- **Resolved Answer**: "Option 2 — Narrow constructor, generic return, FireAndForget returns a stub result."
- **Normalized Requirement**: `ProcessInvocationPipeline` shall be an `internal` class in `CliInvoke` with a constructor taking `ExternalProcessFactory`. It shall expose `Task<TResult> InvokeAsync<TResult>(ProcessInvocationContext ctx) where TResult : ProcessResult` with a switch on `ctx.Mode`; `Raw`, `Buffered`, and `Piped` return the typed result from the appropriate capture method; `FireAndForget` starts the process, does not wait, and returns a stub `ProcessResult` with the process ID populated and other fields default.
- **Constraints**: The class body is approximately 30 lines. The `FireAndForget` stub result has misleading fields (ExitCode 0, no ExitTime) — callers that want meaningful data from FireAndForget should inspect the process ID only.
- **Cites**: D001 (internal in CliInvoke), D003 (method signature), D004 (consumes concrete ExternalProcessFactory)

### [T004] — ProcessInvoker wrapper wiring

- **Driver**: "ProcessInvoker should have duplicate code de-duplicated and be easier to maintain with respect to the process invocation pipeline."
- **Resolved Answer**: "Option 1 — Pipeline cached as a field, constructed once in the constructor."
- **Normalized Requirement**: `ProcessInvoker` shall store `private readonly ProcessInvocationPipeline _pipeline;` constructed in the constructor from the injected `IExternalProcessFactory`. Each `Execute*Async` method shall be `async` and build a `ProcessInvocationContext` with the appropriate `InvocationMode`, then `return await _pipeline.InvokeAsync<T>(ctx);`.
- **Constraints**: The `async` keyword is retained on every method for consistent stack traces and to match the existing `IProcessInvoker` contract shape. The 5-line bodies collapse to 3 lines each (`var ctx = ...; return await _pipeline.InvokeAsync<T>(ctx);`).
- **Cites**: D001 (wrapper delegates to pipeline), D002 (context shape), D003 (method signature)

### [T005] — Specialization invoker wrapper wiring

- **Driver**: "I want the specialized Invokers to remain compatible with IProcessInvoker and maintain their functionality whilst using Process Invocation Pipeline under the hood."
- **Resolved Answer**: "Option 1 — Field-cached pipeline + runner config in each method."
- **Normalized Requirement**: Both `CmdProcessInvoker` and `PowershellProcessInvoker` shall store a `ProcessInvocationPipeline` field constructed in the constructor. Each `Execute*Async` method shall (1) call `ThrowIfUnsupported()`, (2) call `_runnerConfigFactory.CreateRunnerConfiguration(processConfiguration)`, (3) build a `ProcessInvocationContext` with the effective `ProcessConfiguration`, (4) `return await _pipeline.InvokeAsync<T>(ctx)`. The pipeline remains runner-blind (D005).
- **Constraints**: The two extra lines per method compared to `ProcessInvoker` are the runner-config compose step — this is intentional and not duplication to be removed.
- **Cites**: D005 (compose in wrapper), D001 (pipeline internal, wrapper delegates), D002 (context shape), D003 (method signature)

### [T006] — CliRun static pipeline wiring

- **Driver**: "Keeping CliRun stable and avoiding regressions whilst introducing the pipeline" — chosen Option 1 (two static fields, lock discipline) with explicit cache invalidation to avoid the stale-factory concern.
- **Resolved Answer**: "Option B — Cache the pipeline, invalidate when the factory changes."
- **Normalized Requirement**: `CliRun` shall hold `private static ProcessInvocationPipeline? _pipeline` and `private static Func<IExternalProcessFactory> _externalProcessFactory` (the existing field). `UseExternalProcessFactory(factory)` shall set `_externalProcessFactory` *and* clear `_pipeline` under the same `_syncRoot` lock. `GetPipeline()` shall use the double-check pattern to construct the pipeline with the current factory. The 3 `ProcessConfiguration` overloads shall be 2-line wrappers (`var ctx = new ProcessInvocationContext(...); return await GetPipeline().InvokeAsync<T>(ctx);`).
- **Constraints**: The same `_syncRoot` lock must guard both `UseExternalProcessFactory` (clearing `_pipeline`) and `GetPipeline()` (constructing the pipeline) to prevent a window where `_pipeline` is non-null but `_externalProcessFactory` has just been swapped. This aligns with the T014 lock discipline.
- **Cites**: D006 (static pipeline, T014 discipline), D001 (CliRun wraps pipeline), D002 (context shape), D003 (method signature)

### [T007] — InternalsVisibleTo grants

- **Driver**: "I don't want to expose more than necessary via internalsvisibleto but I also want the pipeline to work."
- **Resolved Answer**: "Option 1 — Grant both, scoped."
- **Normalized Requirement**: `CliInvoke/AssemblyInfo.cs` shall contain `[assembly: InternalsVisibleTo("CliInvoke.Specializations")]` and `[assembly: InternalsVisibleTo("CliInvoke.Tests")]`. No other assemblies shall be granted InternalsVisibleTo.
- **Constraints**: The pipeline is the only internal type exposed. No other internal surface in `CliInvoke` is granted via these attributes.
- **Cites**: D004 (InternalsVisibleTo for CliInvoke.Specializations), D007 (InternalsVisibleTo for CliInvoke.Tests)

### [T008] — Test surface

- **Driver**: "I want to test the new pipeline whilst also retaining tests for public facing functionality."
- **Resolved Answer**: "Option 1 — One test class per concern."
- **Normalized Requirement**: `PipelineDispatchTests` shall construct `ProcessInvocationPipeline` with a mock `IExternalProcessFactory` and assert every `InvocationMode` arm, cancellation propagation, and the `FireAndForget` stub. `ProcessInvokerIntegrationTests` shall drive `IProcessInvoker` against a real process for all three modes. `ProcessInvocationContext` needs no dedicated test class — the type is public and its construction is tested by the other classes.
- **Constraints**: Both test surfaces run in the same CI gate. Dispatch tests use a mock factory; integration tests use a real process (e.g. `which dotnet`, `echo`).
- **Cites**: D007 (mixed surface), T001–T007 (types under test)
