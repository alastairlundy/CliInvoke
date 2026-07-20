---
title: Add `RunInternalAsync<T>` funnel and route the 3 config-arg overloads through it
classification: Independent
blocked_by: [001-build-string-args-config-helper]
parent: docs/decisions/DECISIONS-CliInvoke-clirun-shape.md
---

## Goal

Add a single private static generic funnel method `RunInternalAsync<T>` that handles `GetExternalProcessFactory().CreateExternalProcess(...)` → `StartAsync` → capture → dispose, eliminating the duplication across the three config-argument overloads. Each public config-argument overload becomes a one-line forward to the funnel.

## What to build

Add a funnel method to `src/CliInvoke/Extensions/CliRun.cs` with the following signature (the signature below is from the F4 implementation blueprint at `IMPLEMENTATION-clirun-shape.md`):

```csharp
private static async Task<T> RunInternalAsync<T>(
    ProcessConfiguration configuration,
    ProcessExitConfiguration? exitConfiguration,
    Func<IExternalProcess, CancellationToken, Task<T>> capture,
    CancellationToken cancellationToken)
```

The funnel:

- Calls `GetExternalProcessFactory().CreateExternalProcess(configuration, exitConfiguration ?? ProcessExitConfiguration.CreateGraceful())` (per D001; the funnel honours the field rather than constructing `new ExternalProcessFactory(resolver)` directly).
- Calls `await externalProcess.StartAsync(cancellationToken)`.
- Returns the result of `await capture(externalProcess, cancellationToken)`.

Add a forward-pointer comment at the top of the funnel body that documents the F1 follow-up: `// F1 follow-up: when the Process Invocation Pipeline ships, route through _pipeline.ExecuteAsync.` The comment must not reference any F1 ledger file or `Dxxx` record — the pipeline type name and method name are the only stable targets (per T004 and D007). The exact parameter list of `_pipeline.ExecuteAsync` is F1's decision per `docs/decisions/DECISIONS-CliInvoke-process-invocation-pipeline.md#D002`; F4 does not pre-specify it.

Each of the three config-argument public overloads at lines 129, 189, and 257 becomes a one-line forward:

- `RunAsync(ProcessConfiguration, ...)` → `RunInternalAsync(cfg, exit, (p, t) => p.WaitForExitOrTimeoutAsync(t), ct)`.
- `RunBufferedAsync(ProcessConfiguration, ...)` → `RunInternalAsync(cfg, exit, (p, t) => p.CaptureBufferedResultAsync(t), ct)`.
- `RunPipedAsync(ProcessConfiguration, ...)` → `RunInternalAsync(cfg, exit, (p, t) => p.CapturePipedResultAsync(t), ct)`.

The three lambdas are stateless (capture no closure variables) and the C# compiler caches them as static delegates; no per-call closure allocation is expected.

## Size

- **Files** - 1

## Recommended Workflow

### Step 1 — Add the `RunInternalAsync<T>` funnel below the helper from TK001

Where: `src/CliInvoke/Extensions/CliRun.cs`

- Insert the funnel method between `BuildStringArgsConfig` (from TK001) and the first public `RunAsync` overload.
- Add the `// F1 follow-up: when the Process Invocation Pipeline ships, route through _pipeline.ExecuteAsync.` comment at the top of the method body. Do not reference `DECISIONS-CliInvoke-process-invocation-pipeline.md` or any `Dxxx` ID in the comment.

Verify: The funnel compiles in isolation; the body uses `GetExternalProcessFactory()` rather than `new ExternalProcessFactory(resolver)` directly; the comment is a single `//` line with no ledger pointers.

### Step 2 — Collapse `RunAsync(ProcessConfiguration, ...)` to a one-line forward

Where: `src/CliInvoke/Extensions/CliRun.cs`

- Replace the body with a single `=> await RunInternalAsync(configuration, exitConfiguration, (p, t) => p.WaitForExitOrTimeoutAsync(t), cancellationToken);` expression-bodied member.

Verify: `RunAsync(ProcessConfiguration, ...)` still returns `Task<ProcessResult>`; the body is one line.

### Step 3 — Collapse `RunBufferedAsync(ProcessConfiguration, ...)` to a one-line forward

Where: `src/CliInvoke/Extensions/CliRun.cs`

- Replace the body with a single `=> await RunInternalAsync(configuration, exitConfiguration, (p, t) => p.CaptureBufferedResultAsync(t), cancellationToken);` expression-bodied member.

Verify: `RunBufferedAsync(ProcessConfiguration, ...)` still returns `Task<BufferedProcessResult>`; the body is one line.

### Step 4 — Collapse `RunPipedAsync(ProcessConfiguration, ...)` to a one-line forward

Where: `src/CliInvoke/Extensions/CliRun.cs`

- Replace the body with a single `=> await RunInternalAsync(configuration, exitConfiguration, (p, t) => p.CapturePipedResultAsync(t), cancellationToken);` expression-bodied member.

Verify: `RunPipedAsync(ProcessConfiguration, ...)` still returns `Task<PipedProcessResult>`; the body is one line.

### Step 5 — Build the full solution and run existing tests

Where: repository root

- Run `dotnet build src/CliInvoke.sln` to confirm zero errors and zero new warnings on net8.0, net9.0, and net10.0.
- Run `dotnet test tests/CliInvoke.Tests/CliInvoke.Tests.csproj` from `tests/CliInvoke.Tests/` to confirm no regressions in existing tests.

Verify: Build succeeds; existing tests pass; the funnel method is reachable through the three config-argument overloads (a temporary debug break or test invocation can confirm the call chain).

## Context pointers

**Files**
- `src/CliInvoke/Extensions/CliRun.cs` — the single file edited by this ticket; the funnel is added and the three config-argument overloads are collapsed to one-liners.
- `src/CliInvoke.Core/Factories/IExternalProcessFactory.cs` — the factory contract that `GetExternalProcessFactory()` returns; not edited.
- `src/CliInvoke/Factories/ExternalProcessFactory.cs` — the default factory implementation; not edited in this ticket (its parameterless ctor allocates its own `FilePathResolver` — TK003 honours `UseFilePathResolver` by switching the default delegate to `() => new ExternalProcessFactory(GetFilePathResolver())`).
- `IMPLEMENTATION-clirun-shape.md` — the F4 implementation prototype; the funnel's signature and the three capture-method forwards above are taken from the blueprint's `### 3. The 3 config-arg overloads funnel into one internal method` section.

**Domain terms**
- Resource-Owning Type (from `CONTEXT.md`) — `IExternalProcess` is a Resource-Owning Type; the funnel's `using` declaration in the body is the lifecycle-management idiom.
- Process Invocation Pipeline (from `CONTEXT.md`) — the F1 module that this ticket's forward-pointer comment anticipates; not built in F4.

**Ledger records**
- `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#D002` — funnel shape: `RunInternalAsync<T>(ProcessConfiguration, ProcessExitConfiguration?, Func<IExternalProcess, CancellationToken, Task<T>>, CancellationToken)`.
- `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#D007` — drop the F1 follow-up signature pre-spec from D005; let F1 decide the pipeline's input shape.
- `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#T004` — the F1 follow-up comment is module-only, not ledger-pointing.
- Cross-cite (F1 ledger): `docs/decisions/DECISIONS-CliInvoke-process-invocation-pipeline.md#D002` — F1's `ProcessInvocationContext` input shape decision; F4 does not lock this.
- Cross-cite (superseded): `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#D005` (5-line edit claim) — superseded by D007; covered by this ticket via the active successor.

## Acceptance criteria

- [ ] `RunInternalAsync<T>` is a `private static` method in `src/CliInvoke/Extensions/CliRun.cs` per `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#D002`.
- [ ] The funnel signature is `Task<T> RunInternalAsync<T>(ProcessConfiguration, ProcessExitConfiguration?, Func<IExternalProcess, CancellationToken, Task<T>>, CancellationToken)`.
- [ ] The funnel uses `GetExternalProcessFactory()` to honour the `_externalProcessFactory` field per `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#D001`.
- [ ] The funnel uses `ProcessExitConfiguration.CreateGraceful()` as the default when `exitConfiguration` is null.
- [ ] Each of the three config-argument public overloads is a one-line forward to `RunInternalAsync` with the appropriate capture delegate (`WaitForExitOrTimeoutAsync`, `CaptureBufferedResultAsync`, `CapturePipedResultAsync`).
- [ ] A forward-pointer comment at the top of the funnel references the F1 pipeline type name and method name only, with no ledger file path or `Dxxx` record ID per `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#T004` and `#D007`.
- [ ] The public API surface is unchanged: the six public `Run*Async` methods keep their existing signatures; no new public types are introduced.
- [ ] `dotnet build src/CliInvoke.sln` succeeds on net8.0, net9.0, and net10.0.
- [ ] Existing tests under `tests/CliInvoke.Tests/` continue to pass.

## Dependencies

**Blocked by** - 001-build-string-args-config-helper
