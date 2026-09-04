---
title: CliRun defaults facade (no static state)
status: Accepted
date: 2026-09-04
deciders: CliInvoke maintainers
---

# CliRun defaults facade (no static state)

## Context

`CliRun` was previously a static facade backed by process-wide mutable state configured through `CliRun.UseExternalProcessFactory` / `CliRun.UseFilePathResolver`. Those `Use*` methods and their backing static fields/helpers were removed: every `Run*`/`FireAndForget` call now allocates a fresh `ProcessInvocationPipeline` (and a fresh `ExternalProcessFactory` with a default `FilePathResolver`) per call. There is therefore no shared lock or lazy-initialisation asymmetry to preserve — the historical `lock(_syncRoot)` double-check on the resolver no longer exists. Callers needing a custom factory or resolver must use `IProcessInvoker` (or DI) instead of `CliRun`.

## Decision

Remove all static state from `CliRun`. The public API now operates entirely on per-call allocation:

- `CliRun.RunAsync()`, `CliRun.FireAndForget()`, and all other entrypoints create a fresh `ProcessInvocationPipeline` per invocation.
- A fresh `ExternalProcessFactory` with a default `FilePathResolver` is allocated per call.
- No shared locks, no lazy-initialisation, no process-wide mutable state.
- `UseExternalProcessFactory` and `UseFilePathResolver` methods (and their static backing) were removed from the public surface.
- Callers requiring custom factories or resolvers must register `IProcessInvoker` via DI or use `IProcessInvoker` directly.

## Rationale

- **Thread safety:** Per-call allocation eliminates all lock/contention scenarios.
- **Testability:** Each test gets an isolated pipeline instance without needing to reset static state.
- **Simplicity:** No need to track configuration lifetime or synchronize access.
- **Explicit dependencies:** Custom resolvers/factories flow through DI or `IProcessInvoker` rather than hidden static state.

## Consequences

- **Benefit:** Eliminated lock/contention bugs; simpler per-invocation configuration; easier testing.
- **Cost:** Callers previously using `CliRun.UseExternalProcessFactory()` / `CliRun.UseFilePathResolver()` must migrate to registering `IProcessInvoker` in DI or using the invoker directly.
- **Breaking change:** None for the common case (the default behavior is identical). Only affects edge cases that relied on the static facade.

## Related

- `docs/adr/0002-why-not-cliwrap.md` — design rationale for the three invocation patterns
- `CONTRIBUTING.md` — IVT minimization and contribution guidelines
- `PATTERNS.md` — pattern decision tree for choosing an invocation pattern