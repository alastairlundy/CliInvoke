---
title: Guides
layout: simple
---

# Guides

Welcome to the CliInvoke Guides section. This collection of conceptual guides
explains how the library works, how to configure it well, and how to keep it
running safely in production. Each guide is self-contained — you can read them
in order, or jump straight to the one that matches your need.

## Who these guides are for

These guides are for .NET developers who have already
[installed CliInvoke](../getting-started.md) and want to understand how to
use it well. If you are brand new to the library, start with the
[Quickstart](../getting-started-quickstart.md) instead — it gets you to a
running command in under five minutes — and come back here once you have a
working invocation.

The guides assume you can read C#, are familiar with `async`/`await`, and
have at least passing exposure to dependency injection. They do **not**
require you to know anything about `System.Diagnostics.Process` internals.

## Suggested reading order

Read the guides in this order on your first pass. Each one builds on the
concepts introduced in the previous guide, so the sequence is designed to
minimise backtracking.

1. **[Choosing your Invocation Pattern](choosing-invocation-pattern.md)** —
   pick the right pattern (`CliRun`, `IProcessInvoker`, or
   `IExternalProcess`) for your scenario before you write any code.
2. **[Configuration](configuration.md)** — once you have a pattern, learn
   what `ProcessConfiguration` owns, how its builder works, and what the
   defaults are.
3. **[Resource Disposal](resource-disposal.md)** — every unmanaged handle
   and `SecureString` buffer in the library is owned by exactly one of the
   five Resource-Owning Types; this guide documents the disposal contract
   for each.
4. **[Architecture](architecture.md)** — once you have written a few
   invocations, read this to understand the four-stage data-flow, the
   Process Invocation Pipeline, and where cross-cutting concerns plug in.
5. **[Troubleshooting](troubleshooting.md)** — keep this as a reference for
   when something goes wrong; it is symptom-organised, not narrative.

After the first pass, you will probably treat this section as a reference
and return to specific guides as needed. That is the intended use.

## Guide summaries

Each guide is summarised below with one or two sentences on what it covers
and when to read it.

### [Choosing your Invocation Pattern](choosing-invocation-pattern.md)

Decision tree and trade-off guide for picking between `CliRun`,
`IProcessInvoker`, and `IExternalProcess`. **Read this first** — it
establishes which API surface the rest of the guides apply to and explains
when (and how) to migrate from one pattern to another as your needs grow.

### [Architecture](architecture.md)

Explains the four-stage data-flow (Builder → Configuration Model → Invoker
→ Result), shows how the three invocation patterns map onto that flow, and
documents the **Process Invocation Pipeline** — the layered pattern that
keeps cross-cutting concerns (path resolution, runner wrapping, result
validation) out of the core execution path. Read this once you are
comfortable invoking processes and want to understand what is happening
under the hood, or before you write a custom pipeline interceptor.

### [Configuration](configuration.md)

The canonical reference for `ProcessConfiguration` and the related
configuration models. Documents the builder lifecycle, the immutability
invariants, every property on every model, and the default-value
appendix. Read this when you are assembling a non-trivial configuration
and need to know what a property does, what its default is, and which
builder method to use.

### [Resource Disposal](resource-disposal.md)

Documents every public type in the library that implements `IDisposable`
(and, where applicable, `IAsyncDisposable`) — there are exactly five — the
unmanaged resources they own, and the disposal patterns callers must
follow. Read this before shipping code that creates processes in a loop,
exposes processes to a long-running service, or handles `SecureString`
credentials.

### [Troubleshooting](troubleshooting.md)

Symptom-organised diagnosis guide for the most common CliInvoke failure
modes: process hangs, `ObjectDisposedException`, exit-code mismatches,
file-not-found errors, and resource leaks. Each section lists the likely
causes in priority order with a concrete detection method. Keep this as
a reference for when an invocation misbehaves.

## Where to go next

- If you have not installed the library yet, start with the
  [Quickstart](../getting-started-quickstart.md).
- If you are looking for API-level detail on a specific type, see the
  [API Reference](../../api/).
- For the full docs hub, see the
  [Documentation home](../readme.md).
