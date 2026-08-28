---
title: Extensions — OutputTruncationMiddleware and options
classification: Independent
blocked_by: [002-cliinvoke-truncation-capture]
parent: IMPLEMENTATION-CliInvoke-middleware.md
---

## Goal

Add the truncation middleware that writes the configured per-stream cap into `MiddlewareItems` before `next`, plus the `TruncationOptions` POCO and the `UseOutputTruncation()` registration extension. This is the opt-in middleware that activates the capture-time truncation implemented in TK002.

## What to build

1. `TruncationOptions` (sealed) with `long MaxSize` and `static TruncationOptions Default { get; }` returning `MaxSize = 1_048_576` (1 MB), following `PowerShellMiddlewareOptions` pattern.
2. `OutputTruncationMiddleware : IProcessMiddleware` (internal sealed). In `InvokeAsync`, before calling `next`, write `context.Middleware.Items.Set<long>(TruncationDefaults.MaxBytesPerStreamKey, _options.MaxSize)` (referencing the CliInvoke constant from TK002); otherwise pass through.
3. `OutputTruncationMiddlewareExtensions` with `extension(IProcessMiddlewareBuilder builder)` providing `UseOutputTruncation()` and an overload accepting `TruncationOptions`, mirroring `UseLogging()`.

## Size

- **Files**: 3 (all new)
- **Large Files to be created**: omit
- **Large Edits required**: omit

## Recommended Workflow

### Step 1 - Add TruncationOptions

Where: src/CliInvoke.Extensions/Middleware/Truncation/TruncationOptions.cs (new)

- sealed class, `long MaxSize`, `static Default` = 1 MB.

Verify: Matches PowerShellMiddlewareOptions shape.

### Step 2 - Add OutputTruncationMiddleware

Where: src/CliInvoke.Extensions/Middleware/Truncation/OutputTruncationMiddleware.cs (new)

- internal sealed : IProcessMiddleware; ctor takes TruncationOptions (from DI); InvokeAsync sets the cap key then awaits next.

Verify: References CliInvoke.TruncationDefaults.MaxBytesPerStreamKey (project reference exists).

### Step 3 - Add UseOutputTruncation extension

Where: src/CliInvoke.Extensions/Middleware/Truncation/OutputTruncationMiddlewareExtensions.cs (new)

- C# 14 `extension(IProcessMiddlewareBuilder builder)` block; `UseOutputTruncation()` + options overload; mirrors UseLogging().

Verify: Builds; method appears on IProcessMiddlewareBuilder.

## Context pointers

##### Files

- src/CliInvoke.Extensions/Middleware/Logging/LoggingMiddlewareExtensions.cs — UseLogging() pattern to mirror.
- src/CliInvoke.Specializations/Middleware/PowerShellMiddlewareOptions.cs — options POCO pattern.
- src/CliInvoke.Core/Middleware/IProcessMiddlewareBuilder.cs — builder interface.
- src/CliInvoke.Core/Middleware/MiddlewareItems.cs — Set<T> used by middleware.
- src/CliInvoke/TruncationDefaults.cs — the key constant (TK002); referenced here.

##### ADRs

- D004 — truncation middleware ordered upstream of LoggingMiddleware; this middleware only writes the cap, LoggingMiddleware observes capped buffers.

##### Domain terms

- IProcessMiddleware — the middleware contract; truncation is one link in the pipeline.
- MiddlewareItems — the cap is written here for the pipeline to read.

##### Ledger records

- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T001 — OutputTruncationMiddleware + UseOutputTruncation() in Extensions.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T002 — middleware writes cap key before next.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T003 — TruncationOptions POCO, Default 1 MB.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D002 — middleware placement.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D003 — lossy cap + flag.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D004 — upstream of LoggingMiddleware.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D011 — default cap 1 MB.

## Acceptance criteria

- [ ] `TruncationOptions` has `long MaxSize` and `static Default` with `MaxSize = 1_048_576`.
- [ ] `OutputTruncationMiddleware` sets `TruncationDefaults.MaxBytesPerStreamKey` in `MiddlewareItems` to `_options.MaxSize` before `next`.
- [ ] `UseOutputTruncation()` (and options overload) exists on `IProcessMiddlewareBuilder` using C# 14 extension syntax.
- [ ] Middleware is opt-in (not registered by default in AddCliInvoke).

## Dependencies

Blocked by: 002-cliinvoke-truncation-capture
