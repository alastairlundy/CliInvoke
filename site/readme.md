---
title: Home
layout: simple
og_type: website
---

# CliInvoke

CliInvoke is a .NET library for running and interacting with external
command-line processes. It is built for .NET developers who need to launch
an executable, redirect its standard streams, capture its output, and reason
about its lifetime — without hand-rolling `System.Diagnostics.Process` for
every project.

## What CliInvoke is

Every .NET codebase that shells out to another tool eventually needs the same
things: a way to describe the command, a way to start it, a way to capture
its output, a way to dispose of its handles, and a way to test the code that
does all of that. `System.Diagnostics.Process` provides the primitives; the
boilerplate — pipe setup, output buffering, exit-code handling, disposal,
thread-safety, cross-platform path lookup — is left to you. CliInvoke
provides the layer on top.

The library targets .NET 10, runs on Windows, macOS,
Linux, and BSD, and ships with first-class dependency-integration helpers
through `CliInvoke.Extensions`.

## What it offers

CliInvoke exposes **three invocation patterns** that cover the spectrum from
scripting to long-running process control:

- **[`CliRun`](docs/guides/choosing-invocation-pattern.md#cirun--quickstart-scripting)** —
  a static façade for one-line command execution. The right entry point when
  you just need to run a command and read its output.
- **[`IProcessInvoker`](docs/guides/choosing-invocation-pattern.md#iprocessinvoker--di-centric-applications)** —
  an interface for DI-centric applications. Takes a `ProcessConfiguration`
  and returns a typed result, with full support for test doubles and per-call
  configuration.
- **[`IExternalProcess`](docs/guides/choosing-invocation-pattern.md#iexternalprocess--power-user-lifecycle-control)** —
  a power-user API for granular lifecycle control. Splits start, capture,
  and kill into separate calls so you can interact with the process while
  it runs.

Underneath those patterns sit the capabilities that make the library
practical to use day-to-day:

- **Configuration builders and models** — fluent builders assemble
  immutable `ProcessConfiguration` and `ProcessExitConfiguration` models
  with sensible defaults. See the
  [Configuration guide](docs/guides/configuration.md) for the full
  reference.
- **Resource disposal** — five
  [Resource-Owning Types](docs/guides/resource-disposal.md) own every
  unmanaged handle and `SecureString` buffer; the
  [Resource Disposal guide](docs/guides/resource-disposal.md) documents
  the disposal contract for each one.
- **Cross-platform support** — Windows, macOS, Linux, and BSD with
  consistent behaviour; see
  [Supported Operating Systems](docs/Supported-OperatingSystems.md).
- **Dependency-injection extensions** — `AddCliInvoke()` registers the
  invoker, factory, and file-path resolver with the container of your
  choice. See
  [Getting Started](docs/getting-started.md#setting-up-cliinvoke).
- **Specializations** — first-class wrappers for running commands through
  PowerShell or CMD on Windows via
  [`CliInvoke.Specializations`](https://www.nuget.org/packages/CliInvoke.Specializations).

If you are unsure which pattern fits your scenario, the
[Choosing your Invocation Pattern](docs/guides/choosing-invocation-pattern.md)
guide walks through the decision tree and the trade-offs of each option.

## Where to go next

Different readers arrive here with different needs. Pick the path that
matches yours and follow it in order.

### If you are new to CliInvoke

Start with a runnable example, then read the pattern-decision guide.

1. [Quickstart](docs/getting-started-quickstart.md) — install the package
   and run your first `CliRun` command in under five minutes.
2. [Choosing your Invocation Pattern](docs/guides/choosing-invocation-pattern.md)
   — the decision tree for picking between `CliRun`, `IProcessInvoker`,
   and `IExternalProcess`.
3. [Configuration](docs/guides/configuration.md) — every knob on
   `ProcessConfiguration` and `ProcessExitConfiguration`.

### If you are building a testable app with DI

You work in a codebase that uses dependency injection and you need to
unit-test code that shells out to processes.

1. [Getting Started](docs/getting-started.md#setting-up-cliinvoke) —
   register `IProcessInvoker` with your DI container.
2. [Choosing your Invocation Pattern](docs/guides/choosing-invocation-pattern.md)
   — confirm `IProcessInvoker` is the right pattern for your scenario.
3. [Configuration](docs/guides/configuration.md) — the
   `ProcessConfiguration` model, the builder lifecycle, and the
   default-value reference appendix.

### If you need full lifecycle control

You need to interact with the process while it runs — pipe input, monitor
output, send signals, or replace components for advanced scenarios.

1. [Choosing your Invocation Pattern](docs/guides/choosing-invocation-pattern.md)
   — when and how to use the `IExternalProcess` factory and lifecycle
   API.
2. [Architecture](docs/guides/architecture.md) — the internal data-flow
   and the Process Invocation Pipeline so you can reason about where a
   customisation belongs.
3. [Resource Disposal](docs/guides/resource-disposal.md) — the disposal
   contract for the five Resource-Owning Types, including
   `IExternalProcess` and `PipedProcessResult`.

### If you have a specific task

If you know what you need to accomplish but not which page it lives on,
jump straight to the relevant guide.

| I want to … | Go to |
|---|---|
| Debug a leak, hang, or wrong exit code | [Troubleshooting](docs/guides/troubleshooting.md) |
| Tune how a process starts or exits | [Configuration](docs/guides/configuration.md) |
| Understand the internal data-flow | [Architecture](docs/guides/architecture.md) |
| Pick between `CliRun`, `IProcessInvoker`, and `IExternalProcess` | [Choosing your Invocation Pattern](docs/guides/choosing-invocation-pattern.md) |
| Dispose the Resource-Owning Types correctly | [Resource Disposal](docs/guides/resource-disposal.md) |
| Install and set up the library | [Getting Started](docs/getting-started.md) · [Quickstart](docs/getting-started-quickstart.md) |
| Look up a specific API | [API Reference](api/) |

For a flat index of every page, see the
[Documentation hub](docs/readme.md).
