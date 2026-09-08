---
title: ProcessWrapper lifecycle fixes - cancellation reason - ForcefulExit - Started event - dead code - catches
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-bug-audit-fixes.md
---

## Goal

Make `ProcessWrapper`'s cancellation classification deterministic, its exit behavior honest, and its event raising and catch discipline standard — the subtle concurrency cluster of the audit.

## What to build

- Delete both `Register` callbacks (`src/CliInvoke/Processes/Internal/ProcessWrapper.cs:642-647` and `:704-709`); compute the cancellation reason via `CancellationHelper.GetCancellationReason(expectedExitTime, cancellationToken)` inside each catch, where the token is known-canceled. Applies to `WaitForExitOrForcefulTimeoutAsync` and `CancelWithInterrupt` (DECISIONS-CliInvoke-bug-audit-fixes.md#T015). Reason computed at catch rather than cancel-fire — same information, accepted. Semaphore discipline and `ForcefulExit` placement are out of scope.
- `ForcefulExit` (`:428-438`) checks `HasExited` and no-ops on an exited process; the unguarded finally call at `:675` becomes safe on normal exits; the early-return path's deferral to the active cancellation holder stays (DECISIONS-CliInvoke-bug-audit-fixes.md#T020). Existing call-site guards become redundant (harmless); benign `HasExited` snapshot races accepted.
- Narrow the bare catches at `:119-123` and `:143-147` (expected: `InvalidOperationException` from suspend/resume on an exited process) and the bare catch inside `ForcefulExit` (`:434-436`); with T020's self-guard the fallback may reduce to a single guarded kill — implementer discretion within the narrowing rule (DECISIONS-CliInvoke-bug-audit-fixes.md#T017).
- Remove the unreachable `!HasStarted` re-check at `:177` (DECISIONS-CliInvoke-bug-audit-fixes.md#T018).
- Raise the Started event via `Started?.Invoke(...)` at `:213` (DECISIONS-CliInvoke-bug-audit-fixes.md#T019).

## Size

- **Files** - 3 (1 source edit, 2 test edits)

## Recommended Workflow

### Step 1 - Confirm all six sites

Where: src/CliInvoke/Processes/Internal/ProcessWrapper.cs

- Read the file and confirm the two `Register` callbacks, the `ForcefulExit` body and its finally call site, the three bare catches, the dead re-check, and the Started event raise.

Verify: all sites located and matching the audit description.

### Step 2 - Move cancellation-reason computation to the catch point

Where: src/CliInvoke/Processes/Internal/ProcessWrapper.cs

- Delete both `Register` callbacks in `WaitForExitOrForcefulTimeoutAsync` and `CancelWithInterrupt`.
- Compute the reason via `CancellationHelper.GetCancellationReason(expectedExitTime, cancellationToken)` inside each catch.

Verify: no registration remains; the reason is computed only where the token is known-canceled.

### Step 3 - Self-guard ForcefulExit

Where: src/CliInvoke/Processes/Internal/ProcessWrapper.cs

- Add the `HasExited` check so `ForcefulExit` no-ops on an exited process.
- Leave the finally call at `:675` unguarded (now safe) and leave the early-return path's deferral to the active cancellation holder.

Verify: a normally-exiting process no longer reaches a kill path from the finally.

### Step 4 - Narrow the bare catches

Where: src/CliInvoke/Processes/Internal/ProcessWrapper.cs

- Narrow the catches at `:119-123` and `:143-147` to `InvalidOperationException` (suspend/resume on an exited process) with justification comments.
- Narrow the `ForcefulExit` internal catch (`:434-436`); with the self-guard, the fallback may reduce to a single guarded kill — implementer discretion within the narrowing rule.

Verify: each catch documents its expected exception; unexpected exceptions propagate.

### Step 5 - Remove dead code and guard the event raise

Where: src/CliInvoke/Processes/Internal/ProcessWrapper.cs

- Remove the unreachable `!HasStarted` re-check at `:177`.
- Change the Started event raise at `:213` to `Started?.Invoke(...)`.

Verify: build passes; no null-reference path on the event raise.

### Step 6 - Add regression tests

Where: tests/CliInvoke.Tests/Invokers/Cancellation/GracefulCancellationTests.cs, tests/CliInvoke.Tests/Helpers/Processes/ProcessCancellationTests.cs

- Cover deterministic Canceled-state classification, safe normal-exit finally behavior, and event raising (DECISIONS-CliInvoke-bug-audit-fixes.md#T027).

Verify: dotnet test passes for the touched suites.

## Context pointers

##### Files

- `src/CliInvoke/Processes/Internal/ProcessWrapper.cs` - the only source file this ticket edits
- `tests/CliInvoke.Tests/Invokers/Cancellation/GracefulCancellationTests.cs` - cancellation regression tests
- `tests/CliInvoke.Tests/Helpers/Processes/ProcessCancellationTests.cs` - cancellation regression tests

##### Domain terms

- Resource-Owning Type - `ProcessWrapper` manages unmanaged OS (operating system) resources; the self-guard and catch narrowing protect the disposal and kill paths.
- Canceled - the result-model state for library-terminated processes; the catch-point reason computation makes this classification deterministic.

##### Ledger records

- `DECISIONS-CliInvoke-bug-audit-fixes.md#T015` - delete both Register callbacks; reason computed at catch; semaphore discipline and ForcefulExit placement out of scope
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T020` - self-guarding ForcefulExit; redundant call-site guards harmless; snapshot races accepted
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T017` - narrow the three bare catches with justification; implementer discretion on the kill fallback
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T018` - remove the unreachable re-check
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T019` - null-conditional event invoke
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T001`, `DECISIONS-CliInvoke-bug-audit-fixes.md#T027` - existing files only; regression tests

## Acceptance criteria

- [ ] No `Register` callbacks remain in `WaitForExitOrForcefulTimeoutAsync` or `CancelWithInterrupt`; the reason is computed at the catch point via `CancellationHelper.GetCancellationReason` (T015)
- [ ] `ForcefulExit` no-ops on an exited process; the finally call at `:675` is safe on normal exits; the early-return deferral is preserved (T020)
- [ ] All three bare catches are narrowed to documented expected exceptions with justification comments (T017)
- [ ] The unreachable `!HasStarted` re-check is removed (T018) and the Started event is raised via null-conditional invoke (T019)
- [ ] Touched test suites pass

## Dependencies

**Blocked by** - None - can start immediately
