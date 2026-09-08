# CliInvoke Design Patterns

CliInvoke separates its design patterns into two concerns:

- **Invocation patterns** — how a process is executed. CliInvoke offers three: `CliRun`, `IProcessInvoker`, and `IExternalProcess`. Each targets a different audience and trade-off space.
- **Configuration patterns** — how an invocation is described. Every invocation pattern consumes a `ProcessConfiguration`, which can be built with the simple public constructors or the builder.

The two concerns are independent: choose an invocation pattern and a configuration approach separately; neither constrains the other.

## Which invocation pattern should I use?

Start from your situation — this is a short decision tree; follow the first branch that matches:

- **"I just need to run a command and get the result."** → Use **`CliRun`** — the recommended **default** for beginners, scripts, and CI/CD. Zero boilerplate, no DI required.
- **"I need dependency injection, testability, or cross-cutting behaviour (logging, retry, validation)."** → Use **`IProcessInvoker`** registered in DI, and add the behaviour through the **middleware pipeline** — that combination is the **DI + Middleware** path.
- **"I need raw, `System.Diagnostics.Process`-style control over start/stop and lifetime."** → Use **`IExternalProcess`** / `IExternalProcessFactory`.
- **"I am scripting PowerShell or Cmd specifically."** → Use the **Specializations** (`UsePowerShell`, `UseCmd`) middleware extensions on the `IProcessInvoker` path. Specializations are **add-ons to an invocation pattern, not a fourth pattern**.

> **Not sure where to start? Use `CliRun`.** It covers the majority of cases. Move to `IProcessInvoker` only when you need DI or middleware, and to `IExternalProcess` only when you need process-level control. See [Why CliInvoke did not copy CliWrap](docs/adr/0002-why-not-cliwrap.md) for the design rationale.

## Table of Contents

- [Which invocation pattern should I use?](#which-invocation-pattern-should-i-use)

## Invocation Patterns

- [Beginner-Friendly Pattern – `CliRun`](#beginner-friendly-pattern--clirun)
- [End-to-End / DI-Friendly Pattern – `IProcessInvoker`](#end-to-end--di-friendly-pattern--iprocessinvoker)
- [Flexible / Process-User Familiar Pattern – `IExternalProcess` & `IExternalProcessFactory`](#flexible--process-user-familiar-pattern--iexternalprocess--iexternalprocessfactory)
- [Summary of Invocation Trade-offs](#summary-of-invocation-trade-offs)

## Configuration Patterns

- [Which configuration approach should I use?](#which-configuration-approach-should-i-use)
- [Simple constructors (default)](#simple-constructors-default)
- [Advanced – Builder path](#advanced--builder-path)
- [Summary of Configuration Trade-offs](#summary-of-configuration-trade-offs)

---

## Invocation Patterns

### Beginner-Friendly Pattern – `CliRun`

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
* Limited flexibility – cannot change resource policies, interrupt strategies, or start-logic customisations.
* Harder to replace the underlying invoker for unit testing or alternative back-ends.

### Example
```csharp
// Run a simple command and wait for completion.
BufferedProcessResult result = await CliRun.RunAsync("dotnet", "--version");

Console.WriteLine(result.StandardOutput);
```

### End-to-End / DI-Friendly Pattern – `IProcessInvoker`

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
// Startup in an app
builder.Services.AddSingleton<IProcessInvoker, ProcessInvoker>();

// Later in code
IProcessInvoker invoker = provider.GetRequiredService<IProcessInvoker>();

ProcessConfiguration config = new("dotnet", "--info", true);

BufferedProcessResult result = await invoker.ExecuteBufferedAsync(config, ProcessExitConfiguration.CreateGraceful());
```

### Flexible / Process-User Familiar Pattern – `IExternalProcess` & `IExternalProcessFactory`

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
* Requires significant boilerplate: each scenario demands manual disposal, cancellation tokens, and life-cycle management.

### Examples

#### Non-DI Example
```csharp
// 1) Without dependency injection – create factory manually
IExternalProcessFactory factory = new ExternalProcessFactory();

ProcessConfiguration config = new("dotnet", "--runtime", true);
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

ProcessConfiguration config = new("dotnet", "--runtime", true);

using IExternalProcess process = externalFactory.CreateExternalProcess(config);
await process.StartAsync();

BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);
Console.WriteLine(result.StandardOutput);
```

### Summary of Invocation Trade-offs

| Pattern | Beginner Friendly | Resource Disposal | Testable | Lifecycle Control | Boilerplate |
|---------|------------------|------------------|---------|-----------|------------|
| `CliRun` | ✔ | Result returned to caller | ✖ | ✖ | Minimal |
| `IProcessInvoker` | ✖ | Result returned to caller | ✔ | ✖ | Moderate |
| `IExternalProcess`/`IExternalProcessFactory` | ✖ | Requires `using` | ✔ | ✔ | Significant |

Choose `CliRun` for scripting or basic command execution, `IProcessInvoker` for DI-centric applications, and `IExternalProcess` when you need process-level APIs similar to `System.Diagnostics.Process`.

---

## Configuration Patterns

Every invocation pattern consumes a `ProcessConfiguration`. The invocation pattern you chose does not constrain how you build it — pick a configuration approach on its own merits.

### Which configuration approach should I use?

Start from your situation — follow the first branch that matches:

- **"I have a target file path, plain arguments, and nothing exotic."** → Use the **simple public constructors** — the recommended **default**. No builder, no factory, no DI required.
- **"I need argument escaping, Windows user credentials, or resource policy settings."** → Use the **builder path** (`ProcessConfigurationBuilder`), which supports features the simple constructors cannot express.

### Simple constructors (default)

`ProcessConfiguration` exposes two public constructors that cover most scenarios.

**Convenience constructor** — for the common case of target file path plus arguments:

```csharp
ProcessConfiguration config = new("dotnet", "--version");
```

This sets `TargetFilePath`, `Arguments`, and `OutputRedirection` (which defaults to `true`); all other properties take their documented defaults.

**Parameterless constructor with an object initializer** — when you need to set additional init-only properties:

```csharp
ProcessConfiguration config = new()
{
    TargetFilePath = "dotnet",
    Arguments = "--version",
    OutputRedirection = true
};
```

`TargetFilePath` is required and validated at construction time. `WorkingDirectoryPath` defaults to the current directory and throws `DirectoryNotFoundException` if set to a non-existent path.

### Advanced – Builder path

Use `ProcessConfigurationBuilder` when you need features that the simple constructors cannot express:

- **Argument escaping** — `ConfigureArguments(Action<ArgumentsSpec>)` wraps and validates individual arguments.
- **User credential configuration** — `ConfigureUserCredential(Action<UserCredentialSpec>)` for Windows-only credential injection with `SecureString` password staging.
- **Resource policy configuration** — `ConfigureProcessResourcePolicy(Action<ProcessResourcePolicySpec>)` for processor affinity and resource settings.

```csharp
using CliInvoke.Builders;

IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("dotnet")
    .SetArguments(["--info"])
    .SetOutputRedirection(true);

ProcessConfiguration config = builder.Build();
```

The builder delegates to the same init-only properties under the hood; it adds escaping, credential spec, and resource policy callbacks on top.

### Summary of Configuration Trade-offs

| Approach | Best for | Boilerplate | Argument escaping | User credentials | Resource policy |
|----------|----------|-------------|-------------------|------------------|-----------------|
| Simple constructors | Most scenarios | None | ✖ | ✖ | ✖ |
| Builder | Escaping, credentials, or policy needs | Low | ✔ | ✔ | ✔ |

Use the simple constructors by default, and reach for the builder only when you need escaping, credential, or resource policy features.

## See also

- [Why CliInvoke did not copy CliWrap](docs/adr/0002-why-not-cliwrap.md) — the design rationale behind the patterns above.
