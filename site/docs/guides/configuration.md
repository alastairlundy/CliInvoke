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
[Construction (the default)](#construction-the-default) and the
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

## Construction (the default)

In v3, **init construction is the default**. Every configuration model
has a public constructor that accepts its required parameters
positionally. The builder is reserved for advanced scenarios that the
simple constructors cannot express.

### `ProcessConfiguration` — direct construction

`ProcessConfiguration` is the only required model. `TargetFilePath` is
a `required` init property — the constructor throws `ArgumentException`
if it is null or empty. `WorkingDirectoryPath` validates the directory
exists at init time and throws `DirectoryNotFoundException` if it does
not.

**Convenience constructor** — for the common case of target file path
plus arguments:

```csharp
ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");
```

This sets `TargetFilePath`, `Arguments`, and `OutputRedirection` (which
defaults to `true`); all other properties take their documented
defaults.

**Parameterless constructor with an object initializer** — when you
need to set additional init-only properties:

```csharp
ProcessConfiguration config = new ProcessConfiguration
{
    TargetFilePath = "dotnet",
    Arguments = "--version",
    OutputRedirection = true,
    WorkingDirectoryPath = @"C:\my\project"
};
```

**ArgumentList** — replaces the v2 `ArgumentsList` property. When
non-empty, the control adapter emits entries via
`ProcessStartInfo.ArgumentList` instead of the single `Arguments`
string, so the OS command-line parser passes each entry unmodified:

```csharp
ProcessConfiguration config = new ProcessConfiguration("dotnet")
{
    ArgumentList = ["--list-sdks", "--verbosity", "minimal"]
};
```

The model is mostly immutable: most properties have only a getter.
`TargetFilePath`, `Arguments`, and `OutputRedirection` are mutable on
the public surface for back-compat reasons — see the
[Reference Appendix](#reference-appendix) for the exact mutability of
each property.

### `ProcessExitConfiguration` — direct construction

```csharp
ProcessExitConfiguration exitConfig = new ProcessExitConfiguration(
    ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromSeconds(30)));
```

Or use the static factory for the common graceful case:

```csharp
ProcessExitConfiguration exitConfig = ProcessExitConfiguration.CreateGraceful();
```

### `UserCredential` — direct construction

```csharp
UserCredential credential = new UserCredential
{
    Domain = "CONTOSO",
    UserName = "admin",
    Password = securePassword
};
```

`UserCredential` implements `IDisposable` — see the
[Resource Disposal](./resource-disposal.md) guide for ownership rules.

### `ProcessResourcePolicy` — direct construction

```csharp
ProcessResourcePolicy policy = ProcessResourcePolicy.Default;
```

Or construct a custom policy:

```csharp
ProcessResourcePolicy policy = new ProcessResourcePolicy
{
    PriorityClass = ProcessPriorityClass.High
};
```

## Builders — advanced

The builder is **not required** and is positioned as an advanced
construction path. Use the builder when you need features that the
simple constructors cannot express:

- **Argument escaping** — `ConfigureArguments(Action<ArgumentsSpec>)`
  wraps and validates individual arguments, applying character escaping
  (quotes, backslashes, control characters) before joining.
- **User credential configuration** —
  `ConfigureUserCredential(Action<UserCredentialSpec>)` for Windows-only
  credential injection with `SecureString` password staging.
- **Resource policy configuration** —
  `ConfigureProcessResourcePolicy(Action<ProcessResourcePolicySpec>)`
  for processor affinity and resource settings with internal pairing
  logic (e.g., `SetMinWorkingSet` without a prior `SetMaxWorkingSet`
  fabricates `Max = Min + 1`).

```csharp
using CliInvoke.Builders;

IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("dotnet")
    .SetArguments(["--info"])
    .SetOutputRedirection(true);

ProcessConfiguration config = builder.Build();
```

The builder delegates to the same init-only properties under the hood;
it adds escaping, credential spec, and resource policy callbacks on
top.

### Differences from direct construction

The builder and the direct constructor **do not always produce the
same model** for the same input. Concretely:

- **`OutputRedirection` default differs.** The model's public
  constructor defaults `outputRedirection` to `true`; the builder
  defaults it to `false`. `new ProcessConfiguration("git")` and
  `new ProcessConfigurationBuilder("git").Build()` produce
  configurations with different `OutputRedirection` values.
- **Working-directory existence is validated by the builder, not the
  model.** `SetWorkingDirectory` throws `DirectoryNotFoundException`
  if the directory does not exist. The model's constructor also
  validates via the `WorkingDirectoryPath` init setter.
- **Argument escaping is applied by the builder.** `Add` and
  `AddRange` on `ArgumentsSpec` apply character escaping before
  joining. The model's `Arguments` string is stored verbatim.
- **Working-set pairing in `ProcessResourcePolicySpec`.** Calling
  `SetMinWorkingSet` without a prior `SetMaxWorkingSet` fabricates
  `Max = Min + 1` so the resulting policy is internally consistent.
  The model allows `Min` and `Max` to be set independently.

### Builder-methods-to-init-properties table

| Builder method | Init property | Notes |
|----------------|---------------|-------|
| `SetTargetFilePath(string)` | `TargetFilePath` | Required in both paths |
| `SetArguments(string)` | `Arguments` | Verbatim string |
| `SetArguments(IEnumerable<string>)` | `ArgumentList` | Init-only |
| `ConfigureArguments(Action<ArgumentsSpec>)` | *(no direct init)* | Escaping, validation |
| `SetOutputRedirection(bool)` | `OutputRedirection` | Default differs: `true` (init) vs `false` (builder) |
| `SetWorkingDirectory(string)` | `WorkingDirectoryPath` | Builder validates existence |
| `SetWindowCreation(bool)` | `WindowCreation` | |
| `ConfigureShellExecution()` | `UseShellExecution` | |
| `ConfigureEnvironmentVariables(Action<EnvironmentVariablesSpec>)` | `EnvironmentVariables` | |
| `ConfigureUserCredential(Action<UserCredentialSpec>)` | `Credential` | Advanced: `SecureString` staging |
| `ConfigureProcessResourcePolicy(Action<ProcessResourcePolicySpec>)` | `ResourcePolicy` | Advanced: pairing logic |

### When to keep the builder

The builder is the right choice when:

- You need argument escaping (arguments containing `\`, `"`, or
  control characters).
- You need `UserCredentialSpec` for Windows-domain credential staging
  with `SecureString`.
- You need `ProcessResourcePolicySpec` for resource-policy callback
  flows with internal pairing logic.
- The configuration needs to be assembled conditionally, in stages,
  or from multiple sources.

## Dependency Injection

`AddCliInvoke` (namespace `CliInvoke.Extensions`, ships in the
`CliInvoke` package) registers all core services. Call it once at
startup:

```csharp
services.AddCliInvoke();
```

You can configure the middleware pipeline when registering:

```csharp
services.AddCliInvoke(builder => builder.UseMiddleware<LoggingMiddleware>());
```

> If you use the [CliInvoke.Specializations](https://www.nuget.org/packages/CliInvoke.Specializations)
> package's `UsePowerShell()`/`UseCmd()` middleware, also call
> `AddCliInvokeSpecializations()` (same namespace) with the same
> `ServiceLifetime` so those middleware types resolve from the container.

## The Lifecycle: Construction → Model → Invoker

Every CliInvoke invocation moves through three stages.

```text
   ┌──────────────┐    init / Build()   ┌──────────────────┐   ExecuteAsync    ┌──────────┐
   │ Construction  │ ──────────────────► │  Configuration   │ ────────────────► │ Invoker  │
   │ (init or build)│                    │     Model        │                   │ (executes)│
   └──────────────┘                      │  (immutable)     │                   └──────────┘
                                         └──────────────────┘
```

1. **Construction** — the caller creates the configuration, either
   directly via init properties (the default) or via a builder
   (advanced). Both paths produce an immutable `ProcessConfiguration`.
2. **Model** — the configuration holds the data the invoker reads.
   The model is independent of the construction path.
3. **Consumer** — the caller hands the model to one of three
   consumption paths: `IProcessInvoker.ExecuteAsync`,
   `IExternalProcess.StartAsync` (followed by a separate capture
   call), or the static `CliRun.RunAsync` family. Each consumer
   reads the model and runs the process; the result is then obtained
   from the returned task or, for `IExternalProcess`, by calling
    `WaitForExitOrTimeoutAsync` / `CaptureBufferedResultAsync`
    after the process has started.

The same model can be reused across multiple invocations.

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
public class ProcessConfiguration : IEquatable<ProcessConfiguration>
```

The **only required model**. It describes the executable to run, the
arguments to pass, and the OS-level knobs that affect how the process
is spawned (working directory, environment, redirection, credentials,
resource policy, encodings).

**Required init property**: `TargetFilePath`. The constructor
throws `ArgumentException` if it is null or empty.

**Relationship to other models**: A `ProcessConfiguration` may hold
references to a `UserCredential` (via `Credential`) and a
`ProcessResourcePolicy` (via `ResourcePolicy`). It does **not** hold a
reference to a `ProcessExitConfiguration`; the exit configuration is
passed alongside it to the invoker. This separation is intentional:
many invocations share the same `ProcessConfiguration` but differ in
their `ProcessExitConfiguration` (e.g., one has a timeout, another
does not).

**Disposal**: `ProcessConfiguration` does **not** implement `IDisposable`. It is a plain
immutable value object. The `StandardInput` (`StreamWriter`) and `UserCredential` you supply
remain **your** responsibility to dispose — see the
[Resource Disposal](./resource-disposal.md) guide for ownership rules.

### 2. `ProcessExitConfiguration`

Defined in `src/CliInvoke.Core/Primitives/ProcessExitConfiguration.cs`.

```csharp
public class ProcessExitConfiguration : IEquatable<ProcessExitConfiguration>
```

Describes **how the invoker should behave while the process is
running and after it exits**. It is **not** stored on the
`ProcessConfiguration`; it is passed as a separate parameter to
`IProcessInvoker.ExecuteAsync`, `ExecuteBufferedAsync`. If the caller
does not pass one, the invoker uses
its own internal default.

**Owns**: a `ProcessTimeoutPolicy` (via `TimeoutPolicy`), a
`ProcessExitBehaviour` (via `RequestedCancellationExitBehaviour`), a
`ProcessExceptionBehaviour` (via `ExceptionBehaviour`), and a `bool`
(via `CancellationThrowsException`).

**Relationship to the lifecycle**: The exit configuration is
constructed in the same Construction → Model stage as the rest, but it is
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
implements `IDisposable`. A `UserCredential` assigned to a
`ProcessConfiguration.Credential` is **not** disposed by the configuration —
the caller must dispose it. See the
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

## The Consumers

The third stage of the lifecycle has **three** consumption paths.
They all consume the same `ProcessConfiguration` (and, optionally,
the same `ProcessExitConfiguration`) but differ in lifetime,
ergonomics, and level of control.

| Consumer | Defined in | Lifetime | Use when |
|----------|------------|----------|----------|
| `IProcessInvoker` | `src/CliInvoke.Core/IProcessInvoker.cs` | Fire-and-forget; runs to completion. | You want to run a process and get a result. |
| `IExternalProcess` | `src/CliInvoke.Core/Processes/IExternalProcess.cs` | Long-lived handle to a running process. | You need to observe `Started`/`Exited` events, stream output, or interact with the process while it runs. |
| `CliRun` (static) | `src/CliInvoke/CliRun.cs` | Fire-and-forget; builds the configuration for you. | You want the shortest possible call and don't need to reuse the configuration. |

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
    `ProcessResult`, or `CaptureBufferedResultAsync` to read the buffered
    stdout/stderr into memory. These methods can be called at any point
    during execution, not only at exit.
4. **Terminate** — call `Kill()` to forcibly stop a runaway process.

> **Fire-and-forget launching** is *not* a member of `IExternalProcess`.
> If you only need the OS process id and do not care about the result,
> use the static `CliRun.FireAndForget(...)` API instead:
>
> ```csharp
> // Returns the OS process id; the process runs detached from the caller.
> static int CliRun.FireAndForget(ProcessConfiguration configuration);
> static int CliRun.FireAndForget(string targetFilePath, string arguments = "", string? workingDirectory = null);
> ```

`IExternalProcess` is constructed by `IExternalProcessFactory`
(typically obtained via `AddCliInvoke` dependency injection, namespace
`CliInvoke.Extensions`) or by the `CliRun` static API below. The caller owns the
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

    // RunBufferedAsync follows the same shape.
}
```

`CliRun` is a static façade that hides the configuration model
entirely. It is the right choice when you have a single command to
run, do not need to reuse the configuration, and do not want to
import an `IProcessInvoker` from DI.

Internally, `CliRun` constructs an `IExternalProcess` via a
default `IExternalProcessFactory`, calls `StartAsync` on it, then
calls one of the capture methods (`WaitForExitOrTimeoutAsync` for
`RunAsync` or `CaptureBufferedResultAsync` for `RunBufferedAsync`) to
obtain the result,
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
| Process with many optional properties set conditionally | `IProcessConfigurationBuilder` (advanced) |
| Need a per-invocation timeout but a shared process configuration | Direct constructor on `ProcessExitConfiguration` passed alongside |
| Running as a different Windows user | Configure a `UserCredentialSpec` through the builder or construct a `UserCredential` directly |
| Constraining CPU or memory | Construct a `ProcessResourcePolicy` directly or use `ProcessResourcePolicySpec` (advanced) |
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
| `TargetFilePath` | `string` | *(required, no default)* | Read-only | 111 |
| `Arguments` | `string` | `""` | Read-only | 121 |
| `RequiresAdministrator` | `bool` | `false` | Read-only | 106 |
| `WorkingDirectoryPath` | `string` | `Directory.GetCurrentDirectory()` | Read-only | 116 |
| `WindowCreation` | `bool` | `false` | Read-only | 126 |
| `UseShellExecution` | `bool` | `false` | Read-only | 147 |
| `EnvironmentVariables` | `IReadOnlyDictionary<string, string>` | `new Dictionary<string, string>()` | Read-only | 131 |
| `Credential` | `UserCredential` | `UserCredential.Null` | Read-only | 136 |
| `OutputRedirection` | `bool` | `true` | Read-only | 168 |
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
- [Choosing your Invocation Pattern](./choosing-invocation-pattern.md) —
  the three-pattern canon with decision tree.
- [Migrating to 3.0.0](../migration-guides/3.0.0.md) — breaking changes
  and upgrade guide.
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
