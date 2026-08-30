# CliInvoke Architecture

This page describes the internal data-flow of CliInvoke and maps it
onto concrete source files. It is written for contributors who need
to reason about where a change belongs.

## Scope

Out of scope: configuration model reference (see
[site/docs/guides/configuration.md](../site/docs/guides/configuration.md)),
pattern selection (see [PATTERNS.md](../PATTERNS.md)), and disposal
rules (see README § Resource Cleanup). This page documents the
**runtime** data-flow that turns a configuration into a process
result.

## Conceptual Model

CliInvoke models process invocation as a four-stage pipeline:

```text
   ┌────────────┐    ┌────────────┐    ┌────────────┐    ┌────────────┐
   │ Configur-  │    │  Invoke    │    │  OS        │    │  Result    │
   │ ation      │ ─► │  (orchestr-│ ─► │  Process   │ ─► │  (capture  │
   │            │    │   ation)   │    │  (System.  │    │   + exit   │
   │            │    │            │    │  Diagnos-  │    │   mapping) │
   │            │    │            │    │  tics.     │    │            │
   │            │    │            │    │  Process)  │    │            │
   └────────────┘    └────────────┘    └────────────┘    └────────────┘
       Value           Behaviour          Side-effect         Value
```

| # | Stage | Role | Owns |
|---|-------|------|------|
| 1 | **Configuration** | Pure data describing *what* to run and *how* to start it. | `ProcessConfiguration` (+ optional `ProcessExitConfiguration`, `UserCredential`, `ProcessResourcePolicy`) |
| 2 | **Invoke** | The orchestration layer. Accepts a configuration, builds an `IExternalProcess`, drives start → capture, and disposes. | The invoker implementation and the factory. |
| 3 | **OS Process** | The actual child process started by the OS via `System.Diagnostics.Process`. The library does not own the child after `Start()` returns; it observes it. | The OS process handle, the redirected pipes, and the captured exit code / start time / exit time. |
| 4 | **Result** | Pure data describing *what happened*. | `ProcessResult` / `BufferedProcessResult`. |

The arrow from each stage to the next is a data hand-off. Stage 1
hands a value to Stage 2; Stage 2 hands a `System.Diagnostics.Process`
side-effect to Stage 3; Stage 3 hands exit metadata back to Stage 2;
Stage 2 hands a value to Stage 4 and to the caller.

### Execution modes

The Invoke stage has three execution modes. The mode is selected by
the caller (which method on the invoker / `CliRun` they invoke) and
determines the result type:

| Mode | Caller entry point | Result type | Captures |
|------|--------------------|-------------|----------|
| Basic | `IProcessInvoker.ExecuteAsync` / `CliRun.RunAsync` | `ProcessResult` | Exit code, PID, start/exit time, executed file path. |
| Buffered | `IProcessInvoker.ExecuteBufferedAsync` / `CliRun.RunBufferedAsync` | `BufferedProcessResult` | Basic + stdout/stderr as `string`. |

The mode is implicit in which `IExternalProcess` method is called
(`WaitForExitOrTimeoutAsync` / `CaptureBufferedResultAsync`).

## Implementation Mapping

This section pins each conceptual stage to a concrete type and file
in the source tree.

### Stage 1 — Configuration

| Type | File |
|------|------|
| `ProcessConfiguration` | `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs` |
| `ProcessExitConfiguration` | `src/CliInvoke.Core/Primitives/ProcessExitConfiguration.cs` |
| `UserCredential` | `src/CliInvoke.Core/Primitives/UserCredential.cs` |
| `ProcessResourcePolicy` | `src/CliInvoke.Core/Primitives/Policies/ProcessResourcePolicy.cs` |
| Builders (interfaces) | `src/CliInvoke.Core/Builders/` |
| Builders (concrete implementations) | `src/CliInvoke/Builders/` |

The configuration is value-bearing. The invoker reads it; it is
never mutated by the orchestration, with the single exception of
`TargetFilePath`, which the file path resolver is allowed to rewrite
from a relative path to an absolute one (see
`ExternalProcess.StartAsync`).

### Stage 2 — Invoke

| Type | Role | File |
|------|------|------|
| `IProcessInvoker` (interface) | Abstraction consumers depend on. | `src/CliInvoke.Core/IProcessInvoker.cs` |
| `ProcessInvoker` | Default implementation. Owns an `IExternalProcessFactory` and orchestrates start → capture → dispose. | `src/CliInvoke/ProcessInvoker.cs` |
| `IExternalProcessFactory` (interface) | Creates configured `IExternalProcess` instances. | `src/CliInvoke.Core/Factories/IExternalProcessFactory.cs` |
| `ExternalProcessFactory` | Default factory. Constructs the `ExternalProcess` for a given configuration. | `src/CliInvoke/Factories/ExternalProcessFactory.cs` |
| `CliRun` | Static façade. Builds a `ProcessConfiguration` from positional parameters and delegates to the same factory. | `src/CliInvoke/Extensions/CliRun.cs` |

`ProcessInvoker` is a thin orchestrator. Its body is the same
four-line pattern repeated three times, once per execution mode:

```csharp
IExternalProcess externalProcess = _externalProcessFactory
    .CreateExternalProcess(processConfiguration,
        processExitConfiguration ?? ProcessExitConfiguration.Default);

try
{
    await externalProcess.StartAsync(cancellationToken);
    return await externalProcess.CaptureXxxResultAsync(cancellationToken);
}
finally
{
    externalProcess.Dispose();
}
```

### Stage 3 — OS Process

| Type | Role | File |
|------|------|------|
| `IExternalProcess` (interface) | Public façade for a running process. | `src/CliInvoke.Core/Processes/IExternalProcess.cs` |
| `ExternalProcess` | Implements `IExternalProcess`. Holds the configuration, the file path resolver, and the internal wrapper. | `src/CliInvoke/Processes/ExternalProcess.cs` |
| `ProcessWrapper` (internal) | Subclass of `System.Diagnostics.Process`. Owns the platform-specific process control, redirection, and timeout/cancellation logic. | `src/CliInvoke/Processes/Internal/ProcessWrapper.cs` |
| `BaseProcessControlAdapter` (internal) | Platform-specific suspend / resume / affinity / interrupt implementations. Selected by `ProcessControlAdapterFactory`. | `src/CliInvoke/Processes/Internal/ControlAdapters/` |
| `CancellationHelper` (internal) | Computes expected exit time and maps cancellation reasons to exceptions according to `ProcessExitConfiguration.ExceptionBehaviour`. | `src/CliInvoke/Processes/Internal/Cancellation/` |
| `IFilePathResolver` / `FilePathResolver` | Resolves a relative or bare executable name to an absolute file path. | `src/CliInvoke.Core/IFilePathResolver.cs`, `src/CliInvoke/FilePathResolver.cs` |

`ExternalProcess.StartAsync` performs three things in order:

1. Resolves the target file path through the injected
   `IFilePathResolver` (defaulting to a fresh `FilePathResolver` instance).
2. Constructs a new `ProcessWrapper` and calls `Start()` on it.
3. If a `StandardInput` stream was supplied, copies it into the
   process's stdin asynchronously.

`ProcessWrapper` is the only class that touches the OS process
directly. It translates `ProcessExitConfiguration` into a concrete
exit-handling strategy:

- `ProcessExitBehaviour.WaitForExit` → `WaitForExitAsync`.
- `ProcessExitBehaviour.GracefulExit` → race a `Task.Delay` against
  `WaitForExitAsync`; on timeout, send an interrupt signal via
  `BaseProcessControlAdapter.SendInterruptSignalAsync`; after a
  `CalculatePostInterruptGracePeriodSeconds(timeoutSeconds)` grace period
  (10s + 5% of the timeout, capped at 20s), fall back to `Kill()`.
- `ProcessExitBehaviour.ForcefulExit` → `Kill()` after the timeout.

The race is serialised by a `SemaphoreSlim`
(`ProcessWrapper._cancellationSemaphore`) so two concurrent
cancellation callers cannot both issue `Kill()`.

### Stage 4 — Result

| Type | File |
|------|------|
| `ProcessResult` | `src/CliInvoke.Core/Primitives/Results/ProcessResult.cs` |
| `BufferedProcessResult` | `src/CliInvoke.Core/Primitives/Results/BufferedProcessResult.cs` |

The result is built inside the `CaptureXxxResultAsync` method on
`ExternalProcess`, immediately after `WaitForExitOrTimeoutAsync`
returns:

```text
   ProcessWrapper.StartInfo.FileName    ──┐
   ProcessWrapper.ExitCode              ──┤
   ProcessWrapper.Id                    ──┼─►  ProcessResult
   ProcessWrapper.StartTime             ──┤
   ProcessWrapper.ExitTime              ──┘
                                          + (Buffered) StandardOutput, StandardError strings
                                          + (Piped)    StandardOutput, StandardError streams
```

Ownership of the result is transferred to the caller.

## End-to-End Sequence

The diagram below traces one invocation of
`IProcessInvoker.ExecuteBufferedAsync` from the caller's perspective
through every layer of the implementation.

```text
   Caller
     │
     │  ProcessConfiguration, ProcessExitConfiguration, CancellationToken
     ▼
   ┌──────────────────────────────────────────────────────────────┐
   │ ProcessInvoker.ExecuteBufferedAsync                          │   src/CliInvoke/ProcessInvoker.cs
   │   (resolve exit config, then:)                               │
   │     1. factory.CreateExternalProcess(config, exit)           │
   │     2. await externalProcess.StartAsync(token)               │
   │     3. await externalProcess.CaptureBufferedResult..         │
   │     4. externalProcess.Dispose()  (finally)                  │
   └──────────────────────────────────────────────────────────────┘
     │
     │   IExternalProcess
     ▼
   ┌──────────────────────────────────────────────────────────────┐
   │ ExternalProcessFactory.CreateExternalProcess                 │   src/CliInvoke/Factories/ExternalProcessFactory.cs
   │   returns new ExternalProcess(resolver, config, exit)        │
   └──────────────────────────────────────────────────────────────┘
     │
     ▼
   ┌──────────────────────────────────────────────────────────────┐
   │ ExternalProcess                                              │   src/CliInvoke/Processes/ExternalProcess.cs
   │   StartAsync                                                 │
   │     • resolver.ResolveFilePath(targetFilePath)               │   src/CliInvoke/FilePathResolver.cs
   │     • new ProcessWrapper(config, resourcePolicy)             │
   │     • wrapper.Start()                                        │
   │     • await wrapper.PipeStandardInputAsync(...)              │
   │                                                              │
   │   CaptureBufferedResultAsync                                 │
   │     • Task.WhenAll(                                          │
   │         WaitForExitOrTimeoutAsync(exitConfig, token),        │
   │         ReadAllTextAsync(token))                             │
   │     • return new BufferedProcessResult(...)                  │
   └──────────────────────────────────────────────────────────────┘
     │
     ▼
   ┌──────────────────────────────────────────────────────────────┐
   │ ProcessWrapper : System.Diagnostics.Process                  │   src/CliInvoke/Processes/Internal/ProcessWrapper.cs
   │   • Start() — OS CreateProcess / fork+exec                   │
   │   • WaitForExitOrTimeoutAsync / Graceful / Forceful          │
   │   • SendInterruptSignalAsync (platform-specific)             │   src/CliInvoke/Processes/Internal/ControlAdapters/
   │   • CancellationHelper — reason → exception mapping          │   src/CliInvoke/Processes/Internal/Cancellation/
   └──────────────────────────────────────────────────────────────┘
     │
     │  ExitCode, StartTime, ExitTime, Id, FileName, stdout, stderr
     ▼
   ┌──────────────────────────────────────────────────────────────┐
   │ BufferedProcessResult                                        │   src/CliInvoke.Core/Primitives/Results/BufferedProcessResult.cs
   └──────────────────────────────────────────────────────────────┘
     │
     ▼
   Caller
```

## Where to Make a Change

Use the table below to decide which stage a change belongs to.

| Change | Belongs in |
|--------|-----------|
| New property on the process-start knobs (e.g. a new encoding) | Configuration (Stage 1) |
| New consumer convenience API (e.g. a `RunXxxAsync` overload) | Invoke (Stage 2) — add to `CliRun` and `IProcessInvoker` |
| New exit strategy (e.g. a new `ProcessExitBehaviour` value) | OS Process (Stage 3) — `ProcessWrapper` + `CancellationHelper` + `ProcessControlAdapter` |
| New platform-specific process control (suspend / resume / interrupt) | OS Process (Stage 3) — add a new `BaseProcessControlAdapter` |
| New field on the result | Result (Stage 4) |

## Cross-References

- [GLOSSARY.md](../GLOSSARY.md) — domain glossary.
- [PATTERNS.md](../PATTERNS.md) — three invocation patterns and when
  to use them.
- [site/docs/guides/configuration.md](../site/docs/guides/configuration.md) —
  configuration model reference.
- [site/docs/guides/resource-disposal.md](../site/docs/guides/resource-disposal.md) —
  ownership rules for configuration and result types.
