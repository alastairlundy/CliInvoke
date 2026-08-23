---
title: Configuration
layout: simple
---

# Configuration

This page is the canonical reference for how CliInvoke is configured. It
documents the configuration **models**, the relationship between the
**builders** and the **models** they produce, and the role of the
**invoker** that consumes them.

If you only read one section, read
[The Lifecycle](#the-lifecycle-builder--model--invoker) and the
[Reference Appendix](#reference-appendix).

## Goals

This page exists to answer three questions precisely:

1. What does each configuration model represent, and what does it own?
2. How do the builders relate to the models — and are they required?
3. What is the default value of every property on every configuration
   model?

## Terminology

A **Configuration Model** is an immutable value-bearing object that
describes one aspect of how a process should be run. Models in this
library are POCOs (plain old CLR objects): they hold data, expose
read-only properties, and implement value equality.

A **Builder** is a fluent, mutable object used to assemble a
configuration model. Each builder is a short-lived staging area whose
only job is to produce exactly one model via `Build()`. The produced
model is independent of the builder — they do not share lifetime.

An **Invoker** is the abstraction that turns a configuration model
into a running process. The invoker owns nothing about the
configuration; it reads the model, spawns a `System.Diagnostics.Process`,
runs it, captures the result, and disposes the OS resources it
allocated.

## The Lifecycle: Builder → Model → Invoker

Every CliInvoke invocation moves through three stages.

```text
   ┌────────────┐    Build()    ┌──────────────────┐   ExecuteAsync    ┌──────────┐
   │  Builder   │ ────────────► │  Configuration   │ ────────────────► │ Invoker  │
   │ (mutable)  │               │     Model        │                   │ (executes)│
   └────────────┘               │  (immutable)     │                   └──────────┘
                                └──────────────────┘
```

1. **Builder** — the caller assembles intent. Builders expose
   `Set*`/`Configure*` methods that return `this` for chaining and
   terminate with a `Build()` method.
2. **Model** — `Build()` returns an immutable, value-equal
   `ProcessConfiguration` (and any associated models it references,
   such as `ProcessExitConfiguration` or `UserCredential`).
3. **Consumer** — the caller hands the model to one of three
   consumption paths: `IProcessInvoker.ExecuteAsync`,
   `IExternalProcess.StartAsync` (followed by a separate capture
   call), or the static `CliRun.RunAsync` family. Each consumer
   reads the model and runs the process; the result is then obtained
   from the returned task or, for `IExternalProcess`, by calling
   `WaitForExitOrTimeoutAsync` / `CaptureBufferedResultAsync` /
   `CapturePipedResultAsync` after the process has started.

The same model can be reused across multiple invocations. The same
builder can be used to produce multiple models, but only by calling
`Build()` repeatedly; the builder itself is single-use-per-stage.

## Builders Are Optional

**You do not have to use a builder.** Every configuration model in this
library has a public constructor that accepts its required parameters
positionally. The builder is a convenience for cases where:

- The configuration has many properties and you want to set only a
  few of them.
- The configuration needs to be assembled conditionally, in stages, or
  from multiple sources.
- The construction logic benefits from being expressed as a fluent
  pipeline.

If neither of these applies, construct the model directly:

```csharp
// Direct construction — no builder.
using ProcessConfiguration config = new ProcessConfiguration(
    targetFilePath: "git",
    arguments: "status",
    outputRedirection: true);

ProcessResult result = await invoker.ExecuteAsync(config);
```

vs. the equivalent builder-based form:

```csharp
// Builder-based construction.
using ProcessConfiguration config = new ProcessConfigurationBuilder()
    .SetTargetFilePath("git")
    .SetArguments("status")
    .SetOutputRedirection(true)
    .Build();

ProcessResult result = await invoker.ExecuteAsync(config);
```

The two examples above produce identical models, but the builder and
the direct constructor are **not** equivalent in general. Most defaults
match, but there are several known differences in default values,
validation, and argument handling that callers must be aware of. The
builders are an ergonomic layer, not a thin wrapper.

This is a deliberate design choice. The configuration models are
designed to be serializable, comparable, and stable across versions. The
builders are designed for human callers; they are mutable, they
allocate, and they can change shape between versions without breaking
serialized models.

### Differences from Direct Construction

The builder and the direct constructor **do not always produce the
same model** for the same input. Concretely:

- **`OutputRedirection` default differs.** The model's public
  constructor defaults `outputRedirection` to `true`; the builder
  defaults it to `false`. `new ProcessConfiguration("git")` and
  `new ProcessConfigurationBuilder("git").Build()` produce
  configurations with different `OutputRedirection` values.
  Callers relying on redirected output from a builder-built
  configuration must call `SetOutputRedirection(true)` explicitly.
- **Working-directory existence is validated by the builder, not the
  model.** `SetWorkingDirectory` throws `DirectoryNotFoundException`
  if the directory does not exist. The model's constructor does not
  perform this check; it stores the path verbatim.
- **Argument validation differs.** `SetArguments(IEnumerable<string>)`
  silently drops entries that the configured validation logic rejects
  and throws `ArgumentException` if every entry is rejected. The model
  takes the joined string as-is. When the default validation is in
  use, this means the builder rejects all-null argument lists where
  the model accepts them.
- **Argument escaping is applied by the builder.** `Add` and
  `AddRange` on `ArgumentsSpec` apply character escaping (quotes,
  backslashes, control characters) before joining. The model's
  `Arguments` string is stored verbatim. For most real inputs the
  escaping is a no-op, but the two paths can differ for arguments
  containing `\\`, `"`, or control characters.
- **`ProcessorAffinity` lower bound.** Both the model's
  `ProcessResourcePolicy` and `ProcessResourcePolicySpec.SetProcessorAffinity`
  require `processorAffinity >= 1` (a value of `0` selects no processor
  and is rejected). There is no upper bound because a processor affinity
  mask is a bitmask over processors, so any positive value is accepted.
- **Working-set pairing in `ProcessResourcePolicySpec`.** Calling
  `SetMinWorkingSet` without a prior `SetMaxWorkingSet` fabricates
  `Max = Min + 1` so the resulting policy is internally consistent.
  The model allows `Min` and `Max` to be set independently and will
  accept a configuration with `Min = 100, Max = null`.
- **`UserCredential.LoadUserProfile` default differs.** The model's
  `new UserCredential()` defaults `LoadUserProfile` to `false`; the
  spec's `new UserCredentialSpec().Build()` defaults it to
  `null`. This difference is invisible through `ProcessConfiguration`,
  whose default `Credential` is the all-null `UserCredential.Null`
  singleton, and which the builder also produces by default. It only
  surfaces when constructing a `UserCredential` directly.

## The Configuration Models

The library has four top-level configuration models. Three of them are
optional; only `ProcessConfiguration` is required.

| # | Model | Required? | Purpose |
|---|-------|-----------|---------|
| 1 | [`ProcessConfiguration`](#1-processconfiguration) | Yes | Describes *what* to run and *how* to start it. |
| 2 | [`ProcessExitConfiguration`](#2-processexitconfiguration) | No | Describes timeout, exception, and cancellation behaviour. |
| 3 | [`UserCredential`](#3-usercredential) | No | Windows-domain credentials for the spawned process. |
| 4 | [`ProcessResourcePolicy`](#4-processresourcepolicy) | No | Processor affinity, priority class, and working-set sizes. |

`ProcessExitConfiguration` is the only one passed as a separate
parameter to the invoker; the others are referenced from
`ProcessConfiguration`.

### 1. `ProcessConfiguration`

Defined in `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`.

```csharp
public class ProcessConfiguration : IEquatable<ProcessConfiguration>, IDisposable
```

The **only required model**. It describes the executable to run, the
arguments to pass, and the OS-level knobs that affect how the process
is spawned (working directory, environment, redirection, credentials,
resource policy, encodings).

**Required constructor argument**: `TargetFilePath`. The constructor
throws `ArgumentException` if it is null or empty.

The model is mostly immutable: most properties have only a getter.
`TargetFilePath`, `Arguments`, and `OutputRedirection` are mutable on
the public surface for back-compat reasons — see the
[Reference Appendix](#reference-appendix) for the exact mutability of
each property.

**Relationship to other models**: A `ProcessConfiguration` may hold
references to a `UserCredential` (via `Credential`) and a
`ProcessResourcePolicy` (via `ResourcePolicy`). It does **not** hold a
reference to a `ProcessExitConfiguration`; the exit configuration is
passed alongside it to the invoker. This separation is intentional:
many invocations share the same `ProcessConfiguration` but differ in
their `ProcessExitConfiguration` (e.g., one has a timeout, another
does not).

**Disposal**: `ProcessConfiguration` implements `IDisposable`; see the
[Resource Disposal](./resource-disposal.md) guide for ownership rules.

### 2. `ProcessExitConfiguration`

Defined in `src/CliInvoke.Core/Primitives/ProcessExitConfiguration.cs`.

```csharp
public class ProcessExitConfiguration : IEquatable<ProcessExitConfiguration>
```

Describes **how the invoker should behave while the process is
running and after it exits**. It is **not** stored on the
`ProcessConfiguration`; it is passed as a separate parameter to
`IProcessInvoker.ExecuteAsync`, `ExecuteBufferedAsync`, and
`ExecutePipedAsync`. If the caller does not pass one, the invoker uses
its own internal default.

**Owns**: a `ProcessTimeoutPolicy` (via `TimeoutPolicy`), a
`ProcessExitBehaviour` (via `RequestedCancellationExitBehaviour`), a
`ProcessExceptionBehaviour` (via `ExceptionBehaviour`), and a `bool`
(via `CancellationThrowsException`).

**Relationship to the lifecycle**: The exit configuration is
constructed in the same Builder → Model stage as the rest, but it is
the **only** model that can vary between two invocations of the same
`ProcessConfiguration` without changing the spawned process itself.

### 3. `UserCredential`

Defined in `src/CliInvoke.Core/Primitives/UserCredential.cs`.

```csharp
public class UserCredential : IEquatable<UserCredential>, IDisposable
```

Represents the Windows-domain credentials under which the child
process should run. On non-Windows platforms the credential is
constructed but not applied; the property is `[SupportedOSPlatform("windows")]`
on `Domain`, `Password`, and `LoadUserProfile`.

`UserCredential.Null` is a static singleton representing "no
credential". This is the default assigned by
`ProcessConfiguration`'s constructor.

**Disposal**: `UserCredential` owns its `SecureString` password and
implements `IDisposable`. If the credential is assigned to a
`ProcessConfiguration.Credential`, the configuration takes ownership
and the caller must **not** double-dispose — see the
[Resource Disposal](./resource-disposal.md) guide.

### 4. `ProcessResourcePolicy`

Defined in `src/CliInvoke.Core/Primitives/Policies/ProcessResourcePolicy.cs`.

```csharp
public class ProcessResourcePolicy : IEquatable<ProcessResourcePolicy>
```

Describes OS-level resource constraints applied to the spawned
process: processor affinity, priority class, priority boost, and
working-set sizes. The default value (`ProcessResourcePolicy.Default`)
assigns affinity to all available logical processors; everything else
is left at the OS default.

**Platform notes**:

- `ProcessorAffinity` is supported on Windows and Linux only.
- `MinWorkingSet` and `MaxWorkingSet` are **not** supported on Linux
  or Android.
- All other properties are platform-agnostic.

The model is value-equal and immutable.

## The Builders

There is one builder per model that has a non-trivial set of optional
properties.

| Configuration type | Produces | Defined in |
|---------|----------|------------|
| `IProcessConfigurationBuilder` | `ProcessConfiguration` | `src/CliInvoke.Core/Builders/IProcessConfigurationBuilder.cs` |
| `ArgumentsSpec` | `string` (joined arguments) | `src/CliInvoke.Core/Configuration/ArgumentsSpec.cs` |
| `EnvironmentVariablesSpec` | `IReadOnlyDictionary<string, string>` | `src/CliInvoke.Core/Configuration/EnvironmentVariablesSpec.cs` |
| `ProcessResourcePolicySpec` | `ProcessResourcePolicy` | `src/CliInvoke.Core/Configuration/ProcessResourcePolicySpec.cs` |
| `UserCredentialSpec` | `UserCredential` | `src/CliInvoke.Core/Configuration/UserCredentialSpec.cs` |

All builders are **optional**. Every model they produce has a public
constructor that bypasses the builder entirely. See
[Builders Are Optional](#builders-are-optional) above.

**The `Configure*` pattern**: Several `Configure*` methods accept an
`Action<TSpec>` so the caller can configure a nested spec
inline. For example,
`IProcessConfigurationBuilder.ConfigureArguments(Action<ArgumentsSpec>)`
runs the action against the shared `ArgumentsSpec` instance held by the
builder and folds the result into the configuration being built. Repeated
calls update the same staged arguments rather than starting from empty.

## The Consumers

The third stage of the lifecycle has **three** consumption paths.
They all consume the same `ProcessConfiguration` (and, optionally,
the same `ProcessExitConfiguration`) but differ in lifetime,
ergonomics, and level of control.

| Consumer | Defined in | Lifetime | Use when |
|----------|------------|----------|----------|
| `IProcessInvoker` | `src/CliInvoke.Core/IProcessInvoker.cs` | Fire-and-forget; runs to completion. | You want to run a process and get a result. |
| `IExternalProcess` | `src/CliInvoke.Core/Processes/IExternalProcess.cs` | Long-lived handle to a running process. | You need to observe `Started`/`Exited` events, stream output, or interact with the process while it runs. |
| `CliRun` (static) | `src/CliInvoke/Extensions/CliRun.cs` | Fire-and-forget; builds the configuration for you. | You want the shortest possible call and don't need to reuse the configuration. |

### `IProcessInvoker`

```csharp
public interface IProcessInvoker
{
    Task<ProcessResult> ExecuteAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default);

    Task<BufferedProcessResult> ExecuteBufferedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default);

    Task<PipedProcessResult> ExecutePipedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default);
}
```

The invoker is the **only** stage of the lifecycle that performs
side-effects. It is also the **only** stage that does not retain
references to the configuration after the call returns — see the
[Resource Disposal](./resource-disposal.md) guide.

The invoker does not validate the configuration. The configuration
models validate themselves in their constructors (e.g.,
`ProcessConfiguration` rejects a null `TargetFilePath`). The invoker
trusts the model it was given.

The invoker is also not the place to configure process behaviour —
that is the job of the configuration model. If you find yourself
wanting to pass a flag to `ExecuteAsync` that is not on
`ProcessConfiguration` or `ProcessExitConfiguration`, the right answer
is to add it to the appropriate model, not to overload the invoker.

### `IExternalProcess`

```csharp
public interface IExternalProcess : IDisposable
{
    ProcessConfiguration Configuration { get; init; }
    ProcessExitConfiguration ExitConfiguration { get; }

    bool HasExited { get; }
    bool HasStarted { get; }

    event EventHandler Started;
    event EventHandler Exited;

    Task StartAsync(CancellationToken cancellationToken);
    Task StartAsync(ProcessConfiguration configuration, CancellationToken cancellationToken);

    Task<ProcessResult> WaitForExitOrTimeoutAsync(CancellationToken cancellationToken);
    Task<BufferedProcessResult> CaptureBufferedResultAsync(CancellationToken cancellationToken);
    Task<PipedProcessResult> CapturePipedResultAsync(CancellationToken cancellationToken);

    int FireAndForget(CancellationToken cancellationToken);
    Task Kill();
}
```

`IExternalProcess` is a long-lived wrapper around the running
`System.Diagnostics.Process`. Unlike `IProcessInvoker`, which spawns
and joins in a single call, `IExternalProcess` exposes the process's
lifecycle as a sequence of steps you orchestrate yourself:

1. **Start** — call `StartAsync(...)`. This returns once the OS
   process has been launched and the redirected pipes are attached.
   `StartAsync` returns a plain `Task`; it does **not** return the
   process result. The result is obtained separately, by calling
   one of the capture methods below.
2. **Observe** — subscribe to `Started` and `Exited` events, or
   poll `HasStarted` / `HasExited`.
3. **Capture** — call `WaitForExitOrTimeoutAsync` for a plain
   `ProcessResult`, `CaptureBufferedResultAsync` to read the buffered
   stdout/stderr into memory, or `CapturePipedResultAsync` to obtain
   a `PipedProcessResult` whose streams you can stream from. These
   methods can be called at any point during execution, not only at
   exit.
4. **Terminate** — call `Kill()` to forcibly stop a runaway process,
   or `FireAndForget` if you only need the OS process id and do not
   care about the result.

`IExternalProcess` is constructed by `IExternalProcessFactory`
(typically obtained via dependency injection in the `CliInvoke.Extensions`
package) or by the `CliRun` static API below. The caller owns the
returned `IExternalProcess` and is responsible for disposing it —
see the [Resource Disposal](./resource-disposal.md) guide.

`IProcessInvoker` and `IExternalProcess` are **not** competing APIs.
`IProcessInvoker` is the right choice when you want a one-shot run;
`IExternalProcess` is the right choice when you need ongoing control.
Internally, the invoker factory constructs an `IExternalProcess` to
do its work.

### `CliRun`

```csharp
public static class CliRun
{
    public static Task<ProcessResult> RunAsync(
        string targetFilePath,
        string arguments = "",
        string? workingDirectory = null,
        TimeSpan? timeoutTimeSpan = null,
        CancellationToken cancellationToken = default);

    public static Task<ProcessResult> RunAsync(
        ProcessConfiguration configuration,
        ProcessExitConfiguration? exitConfiguration = null,
        CancellationToken cancellationToken = default);

    // RunBufferedAsync / RunPipedAsync follow the same shape.
}
```

`CliRun` is a static façade that hides the configuration model
entirely. It is the right choice when you have a single command to
run, do not need to reuse the configuration, and do not want to
import an `IProcessInvoker` from DI.

Internally, `CliRun` constructs an `IExternalProcess` via a
default `IExternalProcessFactory`, calls `StartAsync` on it, then
calls one of the capture methods (`WaitForExitOrTimeoutAsync` for
`RunAsync`, `CaptureBufferedResultAsync` for `RunBufferedAsync`,
`CapturePipedResultAsync` for `RunPipedAsync`) to obtain the result,
and disposes the `IExternalProcess`. The configuration is built for
you from the positional parameters; the timeout defaults to
`ProcessTimeoutPolicy.Default.TimeoutThreshold` (3 minutes); and
the exit configuration defaults to a graceful one. The factory and
file-path resolver are fixed defaults — `CliRun` keeps no process-wide
mutable state — so callers that need a custom factory or resolver
should construct an `IProcessInvoker` (or resolve one from the DI
container) instead.

`CliRun` is the most concise entry point and the most opinionated.
It trades the explicitness of the configuration model for
readability. Callers that need to configure anything beyond
`targetFilePath`, `arguments`, `workingDirectory`, and
`timeoutTimeSpan` should drop down to a constructed
`ProcessConfiguration` and use one of the other consumers.

## When to Use What

| Scenario | Recommended construction |
|----------|--------------------------|
| One-off command with a small fixed set of arguments | `CliRun.RunAsync(...)` (no model), or direct constructor on `ProcessConfiguration` |
| Process with many optional properties set conditionally | `IProcessConfigurationBuilder` |
| Need a per-invocation timeout but a shared process configuration | Direct constructor on `ProcessExitConfiguration` passed alongside |
| Running as a different Windows user | Configure a `UserCredentialSpec` through the builder or call `Build()` to produce a `UserCredential`, then assign the model to `ProcessConfiguration.Credential` |
| Constraining CPU or memory | Configure a `ProcessResourcePolicySpec` through the builder or call `Build()` to produce a `ProcessResourcePolicy`, then assign the model to `ProcessConfiguration.ResourcePolicy` |
| Need to observe `Started`/`Exited` events or stream output while the process runs | `IExternalProcess` (via `IExternalProcessFactory`) |
| Run from a static context without DI | `CliRun` |

## Reference Appendix

This appendix lists every property on every configuration model in
CliInvoke, with its type, its default value, and where it lives in
the source.

### `ProcessConfiguration`

Defined in `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`.

| Property | Type | Default | Mutability | Source line |
|----------|------|---------|------------|-------------|
| `TargetFilePath` | `string` | *(required, no default)* | Mutable | 111 |
| `Arguments` | `string` | `""` | `protected set` | 121 |
| `RequiresAdministrator` | `bool` | `false` | Read-only | 106 |
| `WorkingDirectoryPath` | `string` | `Directory.GetCurrentDirectory()` | Read-only | 116 |
| `WindowCreation` | `bool` | `false` | Read-only | 126 |
| `UseShellExecution` | `bool` | `false` | Read-only | 147 |
| `EnvironmentVariables` | `IReadOnlyDictionary<string, string>` | `new Dictionary<string, string>()` | Read-only | 131 |
| `Credential` | `UserCredential` | `UserCredential.Null` | Read-only | 136 |
| `OutputRedirection` | `bool` | `true` | `protected set` | 168 |
| `RedirectStandardInput` | `bool` | `false` | Read-only | 163 |
| `StandardInput` | `StreamWriter?` | `StreamWriter.Null` | Read-only | 158 |
| `ResourcePolicy` | `ProcessResourcePolicy` | `ProcessResourcePolicy.Default` | Read-only | 181 |
| `StandardInputEncoding` | `Encoding` | `Encoding.Default` | Read-only | 186 |
| `StandardOutputEncoding` | `Encoding` | `Encoding.Default` | Read-only | 191 |
| `StandardErrorEncoding` | `Encoding` | `Encoding.Default` | Read-only | 196 |

> **Note on `OutputRedirection`**: This is the master switch for
> stdout/stderr redirection. When `false`, neither stream is captured
> and the invoker's buffered/piped result types cannot be used.

### `ProcessExitConfiguration`

Defined in `src/CliInvoke.Core/Primitives/ProcessExitConfiguration.cs`.

| Property | Type | Default | Source line |
|----------|------|---------|-------------|
| `TimeoutPolicy` | `ProcessTimeoutPolicy` | `ProcessTimeoutPolicy.Default` | 64 |
| `RequestedCancellationExitBehaviour` | `ProcessExitBehaviour` | `ProcessExitBehaviour.GracefulExit` | 74 |
| `ExceptionBehaviour` | `ProcessExceptionBehaviour` | `ProcessExceptionBehaviour.AllowExceptionsIfUnexpected` | 82 |
| `CancellationThrowsException` | `bool` | `false` | 88 |

### `ProcessTimeoutPolicy`

Defined in `src/CliInvoke.Core/Primitives/Policies/ProcessTimeoutPolicy.cs`.

The parameterless constructor sets `TimeoutThreshold` to **2 minutes**;
the static `Default` instance used by `ProcessExitConfiguration` sets
it to **3 minutes**. Code that constructs its own
`ProcessTimeoutPolicy()` gets the 2-minute value; code that relies on
`ProcessExitConfiguration()`'s default gets the 3-minute value via
`Default`.

| Property | Type | `new ProcessTimeoutPolicy()` | `ProcessTimeoutPolicy.Default` | `ProcessTimeoutPolicy.None` | Source line |
|----------|------|------------------------------|-------------------------------|----------------------------|-------------|
| `Enabled` | `bool` | `true` | `true` | `false` | 74 |
| `TimeoutThreshold` | `TimeSpan` | `TimeSpan.FromMinutes(2)` | `TimeSpan.FromMinutes(3)` | `TimeSpan.FromSeconds(0)` | 69 |
| `TimeoutExitBehaviour` | `ProcessExitBehaviour` | `GracefulExit` | `GracefulExit` | `WaitForExit` | 64 |

### `ProcessResourcePolicy`

Defined in `src/CliInvoke.Core/Primitives/Policies/ProcessResourcePolicy.cs`.

| Property | Type | Default | Platform | Source line |
|----------|------|---------|----------|-------------|
| `ProcessorAffinity` | `IntPtr?` | `2 * Environment.ProcessorCount - 1` *(all logical processors)* | Windows, Linux | 87 |
| `PriorityClass` | `ProcessPriorityClass` | `ProcessPriorityClass.Normal` | All | 92 |
| `EnablePriorityBoost` | `bool` | `false` | All | 97 |
| `MinWorkingSet` | `nint?` | `null` | Windows, macOS | 105 |
| `MaxWorkingSet` | `nint?` | `null` | Windows, macOS | 113 |

`ProcessResourcePolicy.Default` is a static instance that
initializes `ProcessorAffinity` to all logical processors and leaves
the other properties at their constructor defaults.

### `UserCredential`

Defined in `src/CliInvoke.Core/Primitives/UserCredential.cs`.

| Property | Type | `new UserCredential()` | `UserCredential.Null` | Platform | Source line |
|----------|------|------------------------|-----------------------|----------|-------------|
| `Domain` | `string?` | `null` | `null` | Windows | 65 |
| `UserName` | `string?` | `null` | `null` | All | 70 |
| `Password` | `SecureString?` | `null` | `null` | Windows | 76 |
| `LoadUserProfile` | `bool?` | `false` | `null` | Windows | 82 |

`UserCredential.Null` is a static singleton with all four fields
`null`. It is the value `ProcessConfiguration` assigns to
`Credential` by default.

### Enumerations

#### `ProcessExitBehaviour`

Defined in `src/CliInvoke.Core/Primitives/ProcessExitBehaviour.cs`.

| Value | Numeric | Meaning |
|-------|---------|---------|
| `WaitForExit` | `0` | Run until the process exits on its own. |
| `GracefulExit` | `1` | *(default)* Cancel via SIGTERM/SIGINT, fall back to a `CancellationTokenSource`. |
| `ForcefulExit` | `2` | Forcefully terminate the process and all child processes. |

#### `ProcessExceptionBehaviour`

Defined in `src/CliInvoke.Core/Primitives/ProcessExceptionBehaviour.cs`.

| Value | Numeric | Meaning |
|-------|---------|---------|
| `SuppressExceptions` | `0` | Suppress all exceptions thrown during execution. |
| `AllowExceptions` | `1` | Allow .NET to throw the exception if expected. |
| `AllowExceptionsIfUnexpected` | `2` | *(default)* Allow the exception only if it was unexpected. |

## Cross-References

- [Architecture](./architecture.md) — how the three invocation patterns
  consume the configuration.
- [Resource Disposal](./resource-disposal.md) — ownership rules for
  `ProcessConfiguration` and `UserCredential`.
- [Troubleshooting](./troubleshooting.md) — common configuration
  mistakes.
- API Reference — full type documentation.
- Source files:
  - `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`
  - `src/CliInvoke.Core/Primitives/ProcessExitConfiguration.cs`
  - `src/CliInvoke.Core/Primitives/ProcessExitBehaviour.cs`
  - `src/CliInvoke.Core/Primitives/ProcessExceptionBehaviour.cs`
  - `src/CliInvoke.Core/Primitives/UserCredential.cs`
  - `src/CliInvoke.Core/Primitives/Policies/ProcessTimeoutPolicy.cs`
  - `src/CliInvoke.Core/Primitives/Policies/ProcessResourcePolicy.cs`
  - `src/CliInvoke.Core/Builders/IProcessConfigurationBuilder.cs`
  - `src/CliInvoke.Core/IProcessInvoker.cs`
