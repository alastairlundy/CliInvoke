---
title: ShellDetector cancellation checkpoints and narrowed fallback catch
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-bug-audit-fixes.md
---

## Goal

Remove the throwing cancellation registration from `ShellDetector`'s Unix flow and narrow the bare catch in the pwsh-to-cmd fallback so cancellation propagates and unexpected failures surface instead of being swallowed.

## What to build

- Delete the throwing `cancellationToken.Register` at `src/CliInvoke/ShellDetector.cs:67`. The callback throws when the token is already canceled at registration time, which turns a routine cancellation into an unexpected exception from a registration call (DECISIONS-CliInvoke-bug-audit-fixes.md#T002).
- Add `ThrowIfCancellationRequested` checkpoints before and after the awaits in `ResolveDefaultShellOnUnixAsync` so cancellation surfaces promptly (DECISIONS-CliInvoke-bug-audit-fixes.md#T002). Sync-stage cancellation still waits for the next checkpoint — accepted. No registration remains in the flow.
- Narrow the bare catch at `src/CliInvoke/ShellDetector.cs:137` to the expected resolve/start failures with a justification comment; `OperationCanceledException` must propagate instead of falling through to the cmd fallback (DECISIONS-CliInvoke-bug-audit-fixes.md#T017).
- The Windows flow is untouched.

## Size

- **Files** - 3 (1 source edit, 2 test edits)

## Recommended Workflow

### Step 1 - Confirm the fix sites against current source

Where: src/CliInvoke/ShellDetector.cs

- Read the file and locate the `Register` call (reported at line 67) and the bare catch (reported at line 137); line numbers are audit-reported, so verify by direct read.
- Note the await sites in `ResolveDefaultShellOnUnixAsync` that need checkpoints.

Verify: both sites located and match the audit description.

### Step 2 - Remove the registration and add cancellation checkpoints

Where: src/CliInvoke/ShellDetector.cs

- Delete the `cancellationToken.Register` call and its callback.
- Add `ThrowIfCancellationRequested` checkpoints before and after each await in `ResolveDefaultShellOnUnixAsync`.

Verify: no `Register` remains in the flow; the project builds.

### Step 3 - Narrow the fallback catch

Where: src/CliInvoke/ShellDetector.cs

- Replace the bare catch with catches for the documented expected resolve/start failures, each with a justification comment.
- Ensure `OperationCanceledException` propagates rather than falling through to the cmd fallback.

Verify: code review against the narrowing rule — only expected exception types caught, cancellation rethrown.

### Step 4 - Add regression tests

Where: tests/CliInvoke.Tests/Invokers/Cancellation/GracefulCancellationTests.cs, tests/CliInvoke.Tests/Helpers/Processes/ProcessCancellationTests.cs

- Add tests covering prompt cancellation surfacing on the Unix flow and fallback behavior when an expected failure occurs (DECISIONS-CliInvoke-bug-audit-fixes.md#T027).
- Run the test suite from tests/CliInvoke.Tests/ per repo convention.

Verify: dotnet test passes for the touched suites.

## Context pointers

##### Files

- `src/CliInvoke/ShellDetector.cs` - the only source file this ticket edits
- `tests/CliInvoke.Tests/Invokers/Cancellation/GracefulCancellationTests.cs` - cancellation regression tests
- `tests/CliInvoke.Tests/Helpers/Processes/ProcessCancellationTests.cs` - cancellation regression tests

##### Domain terms

- Canceled - the result-model state for library-terminated processes; the checkpoints exist so cancellation is classified deterministically rather than surfacing as an unexpected exception.

##### Ledger records

- `DECISIONS-CliInvoke-bug-audit-fixes.md#T002` - remove Register, add checkpoints; sync-stage delay accepted; Windows flow untouched
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T017` - narrow each bare catch to documented expected exceptions with justification; `OperationCanceledException` propagates
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T001` - existing files only, no new projects or packages
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T027` - regression tests per behavior-changing fix

## Acceptance criteria

- [ ] No `cancellationToken.Register` remains in ShellDetector's Unix flow, so no throw can originate from a registration callback (T002)
- [ ] `ThrowIfCancellationRequested` checkpoints guard before and after each await in `ResolveDefaultShellOnUnixAsync` (T002)
- [ ] The pwsh-to-cmd fallback catch catches only documented expected exceptions with a justification comment; `OperationCanceledException` propagates (T017)
- [ ] Windows flow unchanged; build and touched test suites pass

## Dependencies

**Blocked by** - None - can start immediately
