---
title: Control adapter platform fixes - Unix throws and dead OS terms
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-bug-audit-fixes.md
---

## Goal

Make Unix admin and credential requests fail loudly with `PlatformNotSupportedException` instead of silently doing nothing, and remove the dead platform terms from both control adapters so future readers get no false platform signals.

## What to build

- `UnixProcessControlAdapter.RequireRunningAsAdmin` throws `PlatformNotSupportedException`. It is reached only when `RequiresAdministrator` is set (`BaseProcessControlAdapter.cs:72-73`) (DECISIONS-CliInvoke-bug-audit-fixes.md#T003).
- `UnixProcessControlAdapter.SetUserCredential` throws `PlatformNotSupportedException` for a non-null credential. The throw must be guarded on a non-null credential because `ApplyConfiguration` calls it unconditionally (`BaseProcessControlAdapter.cs:76`) (DECISIONS-CliInvoke-bug-audit-fixes.md#T003).
- Remove the dead `|| OperatingSystem.IsWindows()` term at `UnixProcessControlAdapter.cs:71` (DECISIONS-CliInvoke-bug-audit-fixes.md#T018).
- Verify by direct read, then remove the dead `IsLinux()` term in `WindowsProcessControlAdapter.SetResourcePolicy` (audit-reported site) (DECISIONS-CliInvoke-bug-audit-fixes.md#T018).
- Windows behavior is unchanged. This is a breaking change on Unix — release notes are handled by ticket 015.

## Size

- **Files** - 3 (2 source edits, 1 test file created or edited)

## Recommended Workflow

### Step 1 - Verify the adapter sites and call paths

Where: src/CliInvoke/Processes/Internal/ControlAdapters/

- Read `UnixProcessControlAdapter.cs`, `WindowsProcessControlAdapter.cs`, and `BaseProcessControlAdapter.cs` to confirm the guard conditions at lines 72-73 and 76 and the audit-reported dead term in `SetResourcePolicy`.
- Confirm the dead `|| OperatingSystem.IsWindows()` term at UnixProcessControlAdapter.cs:71.

Verify: all four sites confirmed by direct read before any edit.

### Step 2 - Implement the Unix throws

Where: src/CliInvoke/Processes/Internal/ControlAdapters/UnixProcessControlAdapter.cs

- Make `RequireRunningAsAdmin` throw `PlatformNotSupportedException`.
- Make `SetUserCredential` throw `PlatformNotSupportedException` only when the credential is non-null.

Verify: a null-credential invocation path still completes; a non-null credential throws.

### Step 3 - Remove the dead OS terms

Where: src/CliInvoke/Processes/Internal/ControlAdapters/UnixProcessControlAdapter.cs, src/CliInvoke/Processes/Internal/ControlAdapters/WindowsProcessControlAdapter.cs

- Remove the dead `|| OperatingSystem.IsWindows()` term at UnixProcessControlAdapter.cs:71.
- Remove the dead `IsLinux()` term in `WindowsProcessControlAdapter.SetResourcePolicy` (verified in Step 1).

Verify: build passes; no behavior change on the supported platform for each adapter.

### Step 4 - Add Unix adapter throw tests

Where: tests/ (adapter test file in the CliInvoke test suite)

- Add tests asserting `PlatformNotSupportedException` for admin and non-null-credential requests on Unix (DECISIONS-CliInvoke-bug-audit-fixes.md#T027).
- Make the tests OS-conditional — Unix-path fixes verify on Ubuntu CI (continuous integration); Windows-local runs cannot cover them.

Verify: tests pass on Ubuntu CI; skipped or trivially passing on Windows locally.

## Context pointers

##### Files

- `src/CliInvoke/Processes/Internal/ControlAdapters/UnixProcessControlAdapter.cs` - throws and dead term
- `src/CliInvoke/Processes/Internal/ControlAdapters/WindowsProcessControlAdapter.cs` - dead term only
- `src/CliInvoke/Processes/Internal/ControlAdapters/BaseProcessControlAdapter.cs` - read-only reference for the call-site guards (lines 72-73, 76)

##### Domain terms

- Resource-Owning Type - the adapters sit in the process-control layer that manages OS (operating system) resources; the throws make unsupported resource requests fail loudly rather than silently no-op.

##### Ledger records

- `DECISIONS-CliInvoke-bug-audit-fixes.md#T003` - throw on Unix for admin and non-null credential; guarded SetUserCredential; Windows unchanged; breaking change documented
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T018` - remove all three dead OS terms; Windows-adapter site verified by direct read before deleting
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T001` - existing files only
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T027` - OS-conditional tests, Ubuntu CI verification

## Acceptance criteria

- [ ] `RequireRunningAsAdmin` throws `PlatformNotSupportedException` on Unix (T003)
- [ ] `SetUserCredential` throws `PlatformNotSupportedException` for a non-null credential on Unix; the null-credential path is unchanged (T003)
- [ ] Dead OS terms removed from both adapters; the Windows-adapter site was verified by direct read before deletion (T018)
- [ ] Throw tests are OS-conditional and pass on Ubuntu CI (T027)

## Dependencies

**Blocked by** - None - can start immediately
