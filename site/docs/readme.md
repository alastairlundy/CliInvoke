---
title: Documentation
---

# CliInvoke Documentation

The CliInvoke library offers three patterns for invoking external
processes from .NET, each tuned to a different audience and trade-off
space. This page is the central hub of the Developer Portal. It
routes you to the right specialized documentation based on **who you
are** and **what you're trying to do**.

## I am a …

Pick the description that fits you best. Each path is designed to
land you on a working example or the right reference in 1-3 clicks.

### Beginner: "I just need to run a command"

You want to launch a process, capture its output, and move on. No
DI container, no builders, no ceremony.

**Start here.** Copy, paste, run:

```csharp
using CliInvoke;
using CliInvoke.Core;

// Execute a command and wait for it to finish.
ProcessResult result = await CliRun.RunAsync("dotnet", "--version");
Console.WriteLine($"Exit Code: {result.ExitCode}");
```

That's a complete working program using the beginner-friendly
[`CliRun`](guides/choosing-invocation-pattern.md#cirun--quickstart-scripting)
façade. One click from this page takes you to a runnable example.

**When you're ready for more:**

- [Choosing your Invocation Pattern](guides/choosing-invocation-pattern.md).
  When to stay on `CliRun` and when to graduate.
- [Quickstart](getting-started-quickstart.md). Install, register,
  and run.
- [Getting Started](getting-started.md). Full installation and
  setup walkthrough.

### Professional Developer: "I'm building a testable app with DI"

You work in a codebase that uses dependency injection, you need to
unit-test code that calls out to processes, and you want clean
separation between configuration and execution.

**Start here:**

1. [Choosing your Invocation Pattern](guides/choosing-invocation-pattern.md).
   Follow the decision tree to `IProcessInvoker`.
2. [Getting Started → Setting up CliInvoke](getting-started.md#setting-up-cliinvoke).
   Register `IProcessInvoker` with your DI container.
3. [Configuration](guides/configuration.md). The `ProcessConfiguration`
   model, the builder lifecycle, and the default-value reference
   appendix.

### Power User: "I need full lifecycle control"

You need to interact with the process while it runs (pipe input
after `Start`, monitor progress, send signals), or replace
components for advanced scenarios.

**Start here:**

1. [Choosing your Invocation Pattern → `IExternalProcess`](guides/choosing-invocation-pattern.md).
   When and how to use the factory and lifecycle API.
2. [Architecture](guides/architecture.md). The internal data-flow
   so you can reason about where a customisation belongs.
3. [Configuration](guides/configuration.md). Every knob on
   `ProcessConfiguration` and `ProcessExitConfiguration`.

## I want to …

If you know what you need to accomplish but not which page it's on,
jump straight to the relevant guide.

| I want to … | Go to |
|---|---|
| Debug a leak, hang, or wrong exit code | [Troubleshooting](guides/troubleshooting.md) |
| Tune how a process starts or exits | [Configuration](guides/configuration.md) |
| Understand the internal data-flow | [Architecture](guides/architecture.md) |
| Pick between `CliRun`, `IProcessInvoker`, and `IExternalProcess` | [Choosing your Invocation Pattern](guides/choosing-invocation-pattern.md) |
| Dispose the Resource-Owning Types correctly | [Resource Disposal](guides/resource-disposal.md) |
| Install and set up the library | [Getting Started](getting-started.md) · [Quickstart](getting-started-quickstart.md) |
| Migrate from v1 to v2, or v2 to v3 | [Migration Guides](migration-guides/) |
| Look up a specific API | [API Reference](../api/) |

## All documentation

A flat index of every page in this Developer Portal.

### Getting started

- [Quickstart](getting-started-quickstart.md). Install and run
  your first `CliRun` command in under five minutes.
- [Getting Started](getting-started.md). Full installation, DI
  setup, and worked examples.

### Guides

- [Choosing your Invocation Pattern](guides/choosing-invocation-pattern.md).
  Decision tree and trade-offs for `CliRun`, `IProcessInvoker`,
  and `IExternalProcess`.
- [Architecture](guides/architecture.md). The four-stage data-flow
  from configuration to result.
- [Configuration](guides/configuration.md). `ProcessConfiguration`,
  builders, defaults, and the reference appendix.
- [Resource Disposal](guides/resource-disposal.md). The three
  Resource-Owning Types and the disposal patterns they require.
- [Troubleshooting](guides/troubleshooting.md). Category-based
  failure diagnosis with detection methods.

### Other

- [Building CliInvoke](building-cliinvoke.md). Build the library
  from source.
- [Migration Guides](migration-guides/). v1 to v2, v2 to v3, and other
  migrations.
- [API Reference](../api/). Auto-generated API reference.

## Beyond the portal

- [GitHub repository](https://github.com/alastairlundy/CliInvoke).
  Source, issues, and discussions.
- [NuGet packages](https://www.nuget.org/packages/CliInvoke).
  `CliInvoke`, `CliInvoke.Core`, and
  `CliInvoke.Specializations`.
