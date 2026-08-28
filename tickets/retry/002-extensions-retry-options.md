---
title: Extensions — RetryOptions and RetryBackoffStrategy
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-CliInvoke-middleware.md
---

## Goal

Add the retry configuration types: the `RetryBackoffStrategy` enum and the `RetryOptions` POCO with ledger-mandated defaults, following the `PowerShellMiddlewareOptions` pattern.

## What to build

1. `RetryBackoffStrategy` enum: `Fixed, Exponential`.
2. `RetryOptions` (sealed) with `int MaxAttempts` (default 3), `TimeSpan BaseDelay` (default 100 ms, convention), `RetryBackoffStrategy Strategy` (default Exponential), and `static RetryOptions Default { get; }`, following `PowerShellMiddlewareOptions` pattern.

## Size

- **Files**: 2 (both new)
- **Large Files to be created**: omit
- **Large Edits required**: omit

## Recommended Workflow

### Step 1 - Add RetryBackoffStrategy enum

Where: src/CliInvoke.Extensions/Middleware/Retry/RetryBackoffStrategy.cs (new)

- `public enum RetryBackoffStrategy { Fixed, Exponential }`

Verify: Enum compiles.

### Step 2 - Add RetryOptions

Where: src/CliInvoke.Extensions/Middleware/Retry/RetryOptions.cs (new)

- sealed class; MaxAttempts=3, BaseDelay=100ms, Strategy=Exponential; static Default.

Verify: Matches PowerShellMiddlewareOptions shape; defaults match D012/D014.

## Context pointers

##### Files

- src/CliInvoke.Specializations/Middleware/PowerShellMiddlewareOptions.cs — options POCO pattern to follow.

##### ADRs

- None new.

##### Domain terms

- RetryBackoffStrategy — selects Fixed or Exponential backoff (D007).

##### Ledger records

- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T005 — RetryOptions POCO + RetryBackoffStrategy enum.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D007 — enum named RetryBackoffStrategy in RetryOptions.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D012 — default MaxAttempts = 3.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D014 — default Strategy = Exponential.

## Acceptance criteria

- [ ] `RetryBackoffStrategy` enum exists with `Fixed` and `Exponential`.
- [ ] `RetryOptions` has `MaxAttempts` (default 3), `BaseDelay` (default 100 ms), `Strategy` (default Exponential), and `static Default`.
- [ ] Shapes follow `PowerShellMiddlewareOptions`.

## Dependencies

Blocked by: None - can start immediately
