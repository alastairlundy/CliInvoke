---
title: RetryMiddleware overflow-checked delay and honest docs
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-bug-audit-fixes.md
---

## Goal

Make the documented `MaxTaskDelay` clamp hold for any attempt count and align the XML documentation with what the retry middleware actually does.

## What to build

- `ComputeDelay` (reported at `src/CliInvoke/Extensions/Middleware/Retry/RetryMiddleware.cs:94-108`) computes exponential and linear delays with overflow-checked arithmetic clamping to `MaxTaskDelay` on would-be overflow; any (BaseDelay, MaxAttempts) pair yields a valid non-negative delay (DECISIONS-CliInvoke-bug-audit-fixes.md#T006). Construction-time bound rejection was considered and not adopted.
- XML documentation states that retries apply to classifier-approved results and that exceptions from the pipeline propagate without retry; no exception-classification code ships (DECISIONS-CliInvoke-bug-audit-fixes.md#T007). The cancellation half of the audit item needs no change — `Task.Delay` already observes the token.
- Pattern guard: this middleware applies to the `ProcessInvoker` pattern only; `IExternalProcess` bypasses middleware entirely (repo AGENTS.md middleware asymmetry).

## Size

- **Files** - 2 (1 source edit, 1 test file created or edited)

## Recommended Workflow

### Step 1 - Confirm ComputeDelay and the docs gap

Where: src/CliInvoke/Extensions/Middleware/Retry/RetryMiddleware.cs

- Read the file; confirm the delay computation and where the XML documentation overpromises.

Verify: overflow-prone arithmetic and the docs mismatch are both located.

### Step 2 - Implement overflow-checked arithmetic

Where: src/CliInvoke/Extensions/Middleware/Retry/RetryMiddleware.cs

- Compute exponential and linear delays with overflow-checked arithmetic; clamp to `MaxTaskDelay` on would-be overflow.

Verify: extreme inputs (huge BaseDelay, huge MaxAttempts) produce a valid non-negative delay.

### Step 3 - Correct the XML documentation

Where: src/CliInvoke/Extensions/Middleware/Retry/RetryMiddleware.cs

- State that retries apply to classifier-approved results and that exceptions from the pipeline propagate without retry.
- Add no classification code.

Verify: docs match actual behavior; no behavior code changed for T007.

### Step 4 - Add overflow-boundary tests

Where: tests/CliInvoke.Extensions.Tests/ (retry middleware test file)

- Add boundary tests: large attempt counts, large base delays, and combinations that would overflow unchecked arithmetic (DECISIONS-CliInvoke-bug-audit-fixes.md#T027).

Verify: dotnet test passes for the Extensions test project.

## Context pointers

##### Files

- `src/CliInvoke/Extensions/Middleware/Retry/RetryMiddleware.cs` - the only source file this ticket edits
- `tests/CliInvoke.Extensions.Tests/` - overflow-boundary tests

##### Domain terms

- Process Invocation Pipeline - retry is a middleware concern composed around the invocation, not an Invocation Capability; it applies only to the `ProcessInvoker` pattern.

##### Ledger records

- `DECISIONS-CliInvoke-bug-audit-fixes.md#T006` - overflow-checked arithmetic clamping to MaxTaskDelay; any (BaseDelay, MaxAttempts) pair valid; no construction-time rejection
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T007` - honest docs only; no classification code; cancellation needs no change
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T001`, `DECISIONS-CliInvoke-bug-audit-fixes.md#T027` - existing files only; overflow-boundary tests required

## Acceptance criteria

- [ ] Any (BaseDelay, MaxAttempts) pair yields a valid non-negative delay clamped to `MaxTaskDelay` (T006)
- [ ] XML documentation states the classifier-approved-results promise and exception propagation; no classification code added (T007)
- [ ] Overflow-boundary tests pass (T027)

## Dependencies

**Blocked by** - None - can start immediately
