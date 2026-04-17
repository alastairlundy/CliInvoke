# CliInvoke Design Patterns

CliInvoke offers three distinct design patterns for invoking external processes. Each pattern targets a different audience and trade‑off space


## Table of Contents

- [Beginner‑Friendly Pattern – `CliRun`](#beginner-friendly-pattern-­cliRun)
- [End‑to‑End / DI‑Friendly Pattern – `IProcessInvoker`](#end-to-end--di-friendly-pattern‑iprocessinvoquer)
- [Flexible / Process‑User Familiar Pattern – `ExternalProcess` & `ExternalProcessFactory`](#flexible--process‑user-familiar-pattern‑externalprocess--externalprocessfactory)
- [Summary of Trade‑offs](#summary-of-trade-offs)

---

## Beginner‑Friendly Pattern – `CliRun`

### Purpose
Straightforward API for running a process and retrieving its result.

### Target user
* Basic scripting, CI/CD tasks, or quick prototyping.

### Core idea
`CliRun` exposes `Run*Async` methods that internally create a `ProcessConfiguration`, apply a default `ProcessExitConfiguration`, and delegate execution to the configured `IProcessInvoker`.

### Advantage
* Zero boilerplate – no DI container, no factories required.
* Most arguments are optional; defaults provide sensible behavior for most common use cases.

### Disadvantage
* Limited flexibility – cannot change resource policies, interrupt strategies, or start‑logic customisations.
* Harder to replace the underlying invoker for unit testing or alternative back‑ends.

### Example
```csharp
// Run a simple command and wait for completion.
ProcessResult result = await CliRun.RunAsync("dotnet", "--version");

Console.WriteLine(result.StandardOutput);
```

## End‑to‑End / DI‑Friendly Pattern – `IProcessInvoker`

### Purpose
Full control over the process lifecycle while keeping the orchestrator abstract.

### Target user
* Framework developers or applications that need testability, logging, or custom timeout logic.

### Core idea
`IProcessInvoker` is an interface that consumes a `ProcessConfiguration` and returns a typed `ProcessResult`. An implementation (`ProcessInvoker`) wires together all plumbing – configuration, exit behaviour, cancellation, and piping.

### Advantage
* Explicit dependency injection – easily register a test double.
* Customisable `ProcessConfiguration` and `ProcessExitConfiguration` per call.
* Integrates with standard DI containers (`Microsoft.Extensions.DependencyInjection`, Autofac, etc.).

### Disadvantage
* Requires moderate boilerplate: register services and resolve the invoker.
* Slightly more verbose than `CliRun`.

### Example
```csharp
// Startup in an aoo
builder.Services.AddSingleton<IProcessInvoker, ProcessInvoker>();

// Later in code
IProcessInvoker invoker = provider.GetRequiredService<IProcessInvoker>();

using ProcessConfiguration config = new("dotnet", "--info", OutputRedirectionMode.Buffer);

BufferedProcessResult result = await invoker.ExecuteBufferedAsync(config, ProcessExitConfiguration.CreateGraceful());
```

## Flexible / Process‑User Familiar Pattern – `IExternalProcess` & `IExternalProcessFactory`

### Purpose
Mirror the classic `System.Diagnostics.Process` workflow while providing a safe and rich API surface.

### Target user
* Power users who want granular control over the start/stop sequence and process lifetime.

### Core idea
`IExternalProcess` encapsulates a process instance and exposes asynchronous start, capture, and kill methods. `IExternalProcessFactory` creates configured instances, optionally with a custom `IFilePathResolver`.

### Advantage
* Full lifecycle control: start + optional pipe input, capture output, and terminate the process.
* Extends beyond the invoker abstraction – useful in libraries that need to interact with the process while it runs.

### Disadvantage
* Requires significant boilerplate: each scenario demands manual disposal, cancellation tokens, and life‑cycle management.

### Examples

#### Non-DI Example
```csharp
// 1) Without dependency injection – create factory manually
IExternalProcessFactory factory = new ExternalProcessFactory();

using ProcessConfiguration config = new("dotnet", "--runtime", OutputRedirectionMode.Buffer);
using IExternalProcess process = factory.CreateExternalProcess(config);
await process.StartAsync();

BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);
Console.WriteLine(result.StandardOutput);
```
#### DI Example

```csharp
// 2) With dependency injection – register factory in DI container
builder.Services.AddSingleton<IExternalProcessFactory, ExternalProcessFactory>();
IExternalProcessFactory externalFactory = provider.GetRequiredService<IExternalProcessFactory>();

using ProcessConfiguration config = new("dotnet", "--runtime", OutputRedirectionMode.Buffer);

using IExternalProcess process = externalFactory.CreateExternalProcess(config);
await process.StartAsync();

BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);
Console.WriteLine(result.StandardOutput);
```

## Summary of Trade‑offs

| Pattern | Beginner Friendly | Handles Resource Disposal | Testable | Lifecycle Control | Boilerplate |
|---------|------------------|----------|---------|-----------|------------|
| `CliRun` | ✔ |  ✔ | ✖ | ✖ | Minimal |
| `IProcessInvoker` | ✖ | Requires `using` |  ✔ |  ✖ | Moderate |
| `IExternalProcess`/`ExternalProcess` | ✖ |  Requires `using` | ✔ |  ✔ | Significant |

Choose `CliRun` for scripting or basic command execution, `IProcessInvoker` for DI‑centric applications, and `ExternalProcess` when you need process‑level APIs similar to `System.Diagnostics.Process`.
