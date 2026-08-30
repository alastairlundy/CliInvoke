---
title: Why CliInvoke did not copy CliWrap
status: Accepted
date: 2026-08-30
---

# Why CliInvoke did not copy CliWrap

## Status

Accepted (2026-08-30)

## Context

[CliWrap](https://github.com/Tyrrrz/CliWrap) is a popular .NET library for wrapping command-line
processes. When designing CliInvoke's invocation API it would have been tempting to copy CliWrap's
fluent, chained `Command` builder and its pipe-based result model. CliInvoke instead ships three
distinct invocation patterns plus two composition paths (DI + Middleware and the platform
Specializations). This ADR records why we deliberately did **not** mirror CliWrap.

## Decision

CliInvoke keeps the following design choices, which differ from CliWrap:

1. **Immutable configuration objects, not a single fluent chain.** `ProcessConfiguration` is a plain
   immutable value object — it does not even implement `IDisposable`, so the caller owns disposal of any
   `StandardInput` or `UserCredential` it supplies. This makes configurations thread-safe, easy to share
   across invocations, and straightforward to assert on in tests, rather than a mutable chain you must
   rebuild on every call.
2. **Three invocation patterns for three audiences.** `CliRun` (zero-boilerplate facade),
   `IProcessInvoker` (DI-friendly, testable orchestration), and `IExternalProcess`
   (`System.Diagnostics.Process`-style lifetime control). CliWrap collapses this into one fluent
   `Command` type; we split it so each audience gets an API shaped for its needs.
3. **A first-class middleware pipeline.** Cross-cutting concerns (logging, retry, validation) are
   expressed as middleware around `IProcessInvoker`, not bolted onto a builder. This mirrors
   `HttpContext`-style request pipelines and composes cleanly with DI.
4. **Platform Specializations as a layer, not the core.** PowerShell and Cmd helpers (`UsePowerShell`,
   `UseCmd`) are opt-in Specializations built on the invoker, keeping the core portable and CLI-agnostic.

## Consequences

- Beginners get `CliRun` as the default, lowest-friction entry point.
- Applications that need DI, testability, or middleware have a clean seam (`IProcessInvoker` + the
  middleware pipeline) instead of a monolithic fluent API.
- Power users retain `System.Diagnostics.Process`-style control via `IExternalProcess`.
- Callers explicitly own disposal of `StandardInput` and `UserCredential`, consistent with CliWrap's
  precedent that the command/config object itself is not disposable.

## References

- [PATTERNS.md](../../PATTERNS.md) — the pattern decision tree.
- [Resource Disposal Guide](../../site/docs/guides/resource-disposal.md) — caller-owned disposal rules.
