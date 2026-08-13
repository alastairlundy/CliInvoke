---
title: Choosing your Invocation Pattern
layout: simple
---

# Choosing your Invocation Pattern

CliInvoke offers three distinct patterns for invoking external processes.
Each pattern targets a different audience and trade-off space. This guide
helps you pick the right pattern for your scenario and understand how to
migrate between them as your needs grow.

If you need the full API-level detail for each pattern, see
[`PATTERNS.md`](https://github.com/alastairlundy/CliInvoke/blob/main/PATTERNS.md).
For the internal data-flow, see
[Architecture](architecture.md).

## The three patterns at a glance

| Pattern | Best for | Boilerplate | DI required | Lifecycle control |
|---------|----------|-------------|-------------|-------------------|
| [`CliRun`](#cirun--quickstart-scripting) | Scripts, CI/CD, quick prototypes | Minimal | No | No |
| [`IProcessInvoker`](#iprocessinvoker--di-centric-applications) | DI-centric apps, testable code | Moderate | Yes | No |
| [`IExternalProcess`](#iexternalprocess--power-user-lifecycle-control) | Granular process control, long-running processes | Significant | Optional | Yes |

## Decision tree

Use the flowchart below to find your starting pattern. Each question
narrows the choice until one pattern remains.

```mermaid
flowchart TD
    START([Need to run an external process]) --> Q1{Do you need to\ninteract with the\nprocess while it runs?\n\(e.g. send input,\nmonitor progress\)}
    Q1 -->|Yes| EXTERNAL[IExternalProcess\n& IExternalProcessFactory]
    Q1 -->|No| Q2{Do you need to\nunit-test the\ncalling code or\nswap invoker\nimplementations?}
    Q2 -->|Yes| Q3{Do you already\nuse dependency\ninjection?}
    Q2 -->|No| CLIRUN[CliRun\n— zero boilerplate]
    Q3 -->|Yes| INVOKER[IProcessInvoker\n— DI-friendly]
    Q3 -->|No| Q4{Are you comfortable\nsetting up a\nDI container?}
    Q4 -->|Yes| INVOKER
    Q4 -->|No| CLIRUN
```

**Quick rules of thumb:**

- **Just need to run a command and get output?** → `CliRun`
- **Building a testable application with DI?** → `IProcessInvoker`
- **Need to pipe input, monitor a long-running process, or control start/stop?** → `IExternalProcess`

---

## `CliRun` — Quickstart & scripting

### When to choose it

- You need to run a command and get the result with minimal code.
- You're writing a script, a CI/CD step, or a quick prototype.
- You don't need to mock the invoker in unit tests.
- Default timeout, exit handling, and output buffering are acceptable.

### When NOT to choose it

- You need to inject a test double for the invoker.
- You need custom resource policies, interrupt strategies, or start-logic
  customisations per call.
- You need to interact with the process while it's running.

### Example

```csharp
// Run a simple command and wait for completion.
using CliInvoke;

BufferedProcessResult result = await CliRun.RunBufferedAsync(
    "dotnet", "--version");

Console.WriteLine(result.StandardOutput);
```

`CliRun` is a static façade. It builds a `ProcessConfiguration` internally,
applies sensible defaults, and delegates to the default `IProcessInvoker`.
You get results with a single line of code — no DI container, no factories.

### What you give up

- **Testability** — the static entry point can't be replaced with a mock.
- **Flexibility** — you can't customise resource policies, interrupt
  strategies, or start-logic per call.

---

## `IProcessInvoker` — DI-centric applications

### When to choose it

- You're building an application that uses dependency injection.
- You want to unit-test code that invokes processes by swapping in a
  mock `IProcessInvoker`.
- You need custom `ProcessConfiguration` or `ProcessExitConfiguration`
  per invocation.
- You want integration with standard DI containers (Microsoft.Extensions,
  Autofac, etc.).

### When NOT to choose it

- You need to interact with the process while it's running (pipe input
  after start, monitor progress, send signals).
- You want minimal boilerplate and don't care about testability.

### Example

```csharp
// Register in DI
using CliInvoke.Core;
using CliInvoke;

services.AddCliInvoke();
// IProcessInvoker is registered automatically.

// Later in your code
IProcessInvoker invoker = provider.GetRequiredService<IProcessInvoker>();

using ProcessConfiguration config = new(
    "dotnet", "--info", OutputRedirectionMode.Buffer);

BufferedProcessResult result = await invoker.ExecuteBufferedAsync(
    config, ProcessExitConfiguration.CreateGraceful());

Console.WriteLine(result.StandardOutput);
```

`IProcessInvoker` is an interface that consumes a `ProcessConfiguration`
and returns a typed result. The default implementation (`ProcessInvoker`)
wires together configuration, exit behaviour, cancellation, and piping.

### What you give up

- **Lifecycle control** — you can't interact with the process between
  start and exit. The invoker handles the full start → capture → dispose
  cycle internally.
- **Boilerplate** — you need a DI container and service registration.

---

## `IExternalProcess` — Power-user lifecycle control

### When to choose it

- You need to interact with the process while it's running — pipe input,
  read partial output, send signals, or monitor progress.
- You want granular control over the start/stop sequence.
- You're building a library that wraps process interaction.
- You need an API similar to `System.Diagnostics.Process` but with a
  richer, safer surface.

### When NOT to choose it

- You just need to run a command and get the result — `CliRun` or
  `IProcessInvoker` are simpler.
- You want minimal boilerplate.

### Example — without DI

```csharp
using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Core.Factories;
using CliInvoke.Factories;

IExternalProcessFactory factory = new ExternalProcessFactory();

using ProcessConfiguration config = new(
    "dotnet", "--runtime", OutputRedirectionMode.Buffer);
using IExternalProcess process = factory.CreateExternalProcess(config);

await process.StartAsync();

// You can interact with the process here — pipe input,
// check status, etc.

BufferedProcessResult result = await process.CaptureBufferedResultAsync(
    CancellationToken.None);
Console.WriteLine(result.StandardOutput);
```

### Example — with DI

```csharp
services.AddCliInvoke();
// IExternalProcessFactory is registered automatically.

IExternalProcessFactory factory =
    provider.GetRequiredService<IExternalProcessFactory>();

using ProcessConfiguration config = new(
    "dotnet", "--runtime", OutputRedirectionMode.Buffer);
using IExternalProcess process = factory.CreateExternalProcess(config);

await process.StartAsync();
BufferedProcessResult result = await process.CaptureBufferedResultAsync(
    CancellationToken.None);
```

`IExternalProcess` encapsulates a process instance and exposes asynchronous
start, capture, and kill methods. `IExternalProcessFactory` creates
configured instances, optionally with a custom `IFilePathResolver`.

### What you give up

- **Simplicity** — each scenario demands manual disposal, cancellation
  tokens, and lifecycle management.
- **Convention** — you're responsible for the full start → interact →
  capture → dispose sequence.

---

## Migrating between patterns

As your application grows, you may need to move from a simpler pattern
to a more capable one. The patterns are designed to be composable — you
can upgrade without rewriting your configuration.

### `CliRun` → `IProcessInvoker`

The `ProcessConfiguration` you built implicitly in `CliRun` can be
constructed explicitly and passed to `IProcessInvoker`:

```csharp
// Before: CliRun
BufferedProcessResult result = await CliRun.RunBufferedAsync(
    "dotnet", "--version");

// After: IProcessInvoker
IProcessInvoker invoker = provider.GetRequiredService<IProcessInvoker>();

using ProcessConfiguration config = new(
    "dotnet", "--version", OutputRedirectionMode.Buffer);

BufferedProcessResult result = await invoker.ExecuteBufferedAsync(config);
```

**What changes:**
- Add DI registration (`services.AddCliInvoke()`).
- Construct `ProcessConfiguration` explicitly.
- Resolve `IProcessInvoker` from the container.

### `IProcessInvoker` → `IExternalProcess`

The `ProcessConfiguration` stays the same. Instead of passing it to the
invoker, you pass it to the factory and manage the lifecycle yourself:

```csharp
// Before: IProcessInvoker
BufferedProcessResult result = await invoker.ExecuteBufferedAsync(config);

// After: IExternalProcess
IExternalProcessFactory factory =
    provider.GetRequiredService<IExternalProcessFactory>();

using IExternalProcess process = factory.CreateExternalProcess(config);
await process.StartAsync();

// Now you can interact with the process before capturing results.

BufferedProcessResult result = await process.CaptureBufferedResultAsync(
    CancellationToken.None);
```

**What changes:**
- Resolve `IExternalProcessFactory` instead of `IProcessInvoker`.
- Call `StartAsync()` and `CaptureBufferedResultAsync()` separately.
- Add explicit disposal of the `IExternalProcess`.

---

## Summary of trade-offs

| Pattern | Beginner friendly | Handles resource disposal | Testable | Lifecycle control | Boilerplate |
|---------|:-:|:-:|:-:|:-:|:-:|
| `CliRun` | ✔ | ✔ | ✖ | ✖ | Minimal |
| `IProcessInvoker` | ✖ | Requires `using` | ✔ | ✖ | Moderate |
| `IExternalProcess` / `IExternalProcessFactory` | ✖ | Requires `using` | ✔ | ✔ | Significant |

**Choose `CliRun`** for scripting or basic command execution where you
don't need DI or advanced configuration.

**Choose `IProcessInvoker`** for DI-centric applications where testability
and per-invocation configuration matter.

**Choose `IExternalProcess`** when you need process-level APIs similar to
`System.Diagnostics.Process` — interacting with the process while it runs,
controlling start/stop sequences, or building process-aware libraries.

## Further reading

- [`PATTERNS.md`](https://github.com/alastairlundy/CliInvoke/blob/main/PATTERNS.md) — full API reference for each pattern.
- [Architecture](architecture.md) — internal data-flow and implementation mapping.
- [Configuration](configuration.md) — configuration model reference and builders.
- [Resource Disposal](resource-disposal.md) — ownership and disposal rules.
