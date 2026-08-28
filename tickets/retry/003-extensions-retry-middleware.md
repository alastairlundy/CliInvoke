---
title: Extensions — RetryMiddleware and UseRetryPolicy
classification: Independent
blocked_by: [001-core-retry-classification-hook, 002-extensions-retry-options]
parent: IMPLEMENTATION-CliInvoke-middleware.md
---

## Goal

Add the retry middleware that re-invokes the pipeline while the result is retryable and attempts remain, plus the `UseRetryPolicy()` registration extension(s).

## What to build

1. `RetryMiddleware : IProcessMiddleware` (internal sealed). Ctor takes `IProcessResultValidator<ProcessResult> retryableConditions` and `RetryOptions` from DI. In `InvokeAsync`, call `await next(context)`; while `retryableConditions.ShouldRetry(ctx.Result)` is true and attempts remain, re-invoke `next` with the configured backoff (Fixed or Exponential per `RetryOptions.Strategy`, using `RetryOptions.BaseDelay`).
2. `RetryMiddlewareExtensions` with `extension(IProcessMiddlewareBuilder builder)` providing `UseRetryPolicy()` (default options + default retryable-conditions validator) and overloads accepting a custom `IProcessResultValidator<ProcessResult>` and/or `RetryOptions`, mirroring `UseLogging()`/`UsePostExitValidation()`.

## Size

- **Files**: 2 (both new)
- **Large Files to be created**: omit
- **Large Edits required**: omit

## Recommended Workflow

### Step 1 - Add RetryMiddleware

Where: src/CliInvoke.Extensions/Middleware/Retry/RetryMiddleware.cs (new)

- internal sealed : IProcessMiddleware; ctor(validator, options); InvokeAsync loops on ShouldRetry with backoff.

Verify: Re-invokes next; honors MaxAttempts; uses Strategy/BaseDelay.

### Step 2 - Add UseRetryPolicy extension

Where: src/CliInvoke.Extensions/Middleware/Retry/RetryMiddlewareExtensions.cs (new)

- C# 14 extension block; UseRetryPolicy() + overloads; mirrors UseLogging().

Verify: Builds; method on IProcessMiddlewareBuilder.

## Context pointers

##### Files

- src/CliInvoke.Core/Validation/IProcessResultValidator.cs — ShouldRetry member (TK004).
- src/CliInvoke.Extensions/Middleware/Retry/RetryOptions.cs — options (TK005).
- src/CliInvoke.Extensions/Middleware/Validation/PostExitValidationExtensions.cs — registration pattern to mirror.
- src/CliInvoke.Core/Middleware/IProcessMiddlewareBuilder.cs — builder interface.

##### ADRs

- D005 — retry by default for classified failures; callers avoid retry for non-idempotent invocations.

##### Domain terms

- IProcessResultValidator<ProcessResult> — supplies retryable-conditions rules; ShouldRetry decides.
- Backoff — Fixed or Exponential per RetryOptions.Strategy.

##### Ledger records

- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T004 — retry uses ShouldRetry on the validator.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T006 — RetryMiddleware + UseRetryPolicy() (not UseRetry()).
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D005 — retry by default for retryable failures.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D006 — single validator reuse.

## Acceptance criteria

- [ ] `RetryMiddleware` re-invokes `next` while `ShouldRetry(ctx.Result)` is true and attempts remain, applying Fixed/Exponential backoff from `RetryOptions`.
- [ ] `UseRetryPolicy()` (and overloads for custom validator and/or options) exists on `IProcessMiddlewareBuilder`.
- [ ] Registration name is `UseRetryPolicy()`, not `UseRetry()`.

## Dependencies

Blocked by: 001-core-retry-classification-hook, 002-extensions-retry-options
