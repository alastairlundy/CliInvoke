---
title: Core — Retry classification hook
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-CliInvoke-middleware.md
---

## Goal

Add a retry-oriented classification member to `IProcessResultValidator<TProcessResult>` so the retry middleware can decide retryability by reusing the existing rule engine, without a new classifier type.

## What to build

Add `bool ShouldRetry(TProcessResult result)` to `IProcessResultValidator<TProcessResult>` with a default interface implementation `=> Validate(result)`, so existing implementers are unaffected. `ProcessResultValidator<T>` requires no edit (inherits the default).

## Size

- **Files**: 1 (edit)
- **Large Files to be created**: omit
- **Large Edits required**: omit

## Recommended Workflow

### Step 1 - Add ShouldRetry to the interface

Where: src/CliInvoke.Core/Validation/IProcessResultValidator.cs

- Add `bool ShouldRetry(TProcessResult result) => Validate(result);` as a default interface method.

Verify: Interface compiles; existing implementers need no change; ProcessResultValidator<T> still valid.

## Context pointers

##### Files

- src/CliInvoke.Core/Validation/IProcessResultValidator.cs — add the member.
- src/CliInvoke/Validation/ProcessResultValidator.cs — concrete impl; no edit needed (default applies).

##### ADRs

- None new.

##### Domain terms

- IProcessResultValidator<TProcessResult> — the existing rule engine; ShouldRetry reuses it for retry classification (D006).

##### Ledger records

- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T004 — ShouldRetry default => Validate; no new classifier type.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D005 — retry by default for classified failures.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D006 — single validator reuse, no IProcessResultClassifier.

## Acceptance criteria

- [ ] `IProcessResultValidator<TProcessResult>` has `bool ShouldRetry(TProcessResult result)` with default `=> Validate(result)`.
- [ ] Existing implementers compile unchanged; `ProcessResultValidator<T>` needs no edit.
- [ ] `CliInvoke.Core` builds with no new dependencies.

## Dependencies

Blocked by: None - can start immediately
