---
title: Windows-gated command-line length pre-start check
classification: Independent
blocked_by: [001-start-failure-exception-taxonomy.md]
parent: IMPLEMENTATION-handoff-process-flaws-doc.md
---

# TK002 - Windows-gated command-line length pre-start check

## Goal

Give Windows users an actionable error with the measured command-line length before process start, instead of a cryptic `Win32Exception` (e.g. error 206) when `CreateProcess` rejects an oversized command line.

## What to build

When `OperatingSystem.IsWindows()` is true, the total command-line length - executable path plus the quoted/joined arguments as `CreateProcess` will see it - is checked before process start. Once it exceeds the ~32,767-character `CreateProcess` limit, an `ArgumentException` with the measured length is thrown. The message states that the limit is approximate.

Preferred location: `src/CliInvoke/Processes/Internal/ControlAdapters/BaseProcessControlAdapter.cs` in `ApplyConfiguration`, so the check runs once where the command line is assembled (both the `Arguments` string path and the `ArgumentList` path must be covered). Fallback location: a pre-start guard in `ProcessWrapper.Start()`. Because TK001 modifies `ProcessWrapper.cs`, the adapter is strongly preferred to avoid same-file churn.

Non-Windows platforms get no check - Unix command-line limits are far higher.

Note that per-argument escaping stays in the builder (`ProcessConfigurationBuilder`); this check must not re-escape or mutate arguments, only measure. T001's native-code mapping in `ProcessWrapper.Start()` remains the backstop for over-limit cases that still slip through (e.g. error 206).

This change touches invocation code, so load the `cliinvoke-pattern-validator` skill before editing.

## Size

- Files - 2 to 3 (1-2 edits - `BaseProcessControlAdapter.cs`, possibly `ProcessWrapper.cs` if the guard lands there, 1 create - test file under `tests/CliInvoke.Tests/`)

## Recommended Workflow

### Step 1 - Locate the command-line assembly point

Where: `src/CliInvoke/Processes/Internal/ControlAdapters/BaseProcessControlAdapter.cs`

- Read `ApplyConfiguration` and identify where `Arguments` is set and where `ArgumentList` entries are added (both paths).
- Decide the single choke point where the fully assembled command line can be measured.

Verify: You can name where the check will run for both the `Arguments` and `ArgumentList` configurations.

### Step 2 - Implement the Windows-gated check

Where: `src/CliInvoke/Processes/Internal/ControlAdapters/BaseProcessControlAdapter.cs`

- Gate on `OperatingSystem.IsWindows()`.
- Compute the approximate total length - `FileName` plus quoted/joined arguments including per-argument quoting overhead.
- Throw `ArgumentException` past 32,767 characters, with the measured length in the message and a note that the limit is approximate.

Verify: The check is a no-op on non-Windows; arguments are measured, never mutated.

### Step 3 - Write the Windows-only test

Where: `tests/CliInvoke.Tests/` (new test file)

- Assert that a configuration whose assembled command line exceeds the limit throws `ArgumentException` on Windows.
- Skip the test on non-Windows, matching the repo's OS-dependent test conventions (see existing tests that skip when a dependency is not on PATH).

Verify: `dotnet test` in `tests/CliInvoke.Tests/` passes on Windows with the new test active.

### Step 4 - Run the inner-loop gates

Where: repo root

- Run `bash scripts/guard-configureawait.sh src` and `dotnet build src/CliInvoke.sln`.

Verify: Guard and build pass.

## Context pointers

### Files

- `src/CliInvoke/Processes/Internal/ControlAdapters/BaseProcessControlAdapter.cs` - `ApplyConfiguration` is the preferred location
- `src/CliInvoke/Processes/Internal/ProcessWrapper.cs` - fallback guard location; also modified by ticket 001, which is why this ticket is blocked by it
- `tests/CliInvoke.Tests/` - existing OS-dependent test conventions to mimic

### ADRs

- None directly constraining - no new `InternalsVisibleTo` grants (see `docs/adr/0001-ivt-minimization.md`)

### Domain terms

- Invocation Capability (GLOSSARY.md) - the length limit is a contract the caller can rely on, not an optional middleware concern

### Ledger records

- `DECISIONS-CliInvoke-process-flaw-mitigations.md#D001` - session scope covering the five mitigation items
- `DECISIONS-CliInvoke-process-flaw-mitigations.md#T004` - the resolved Windows-gated pre-start check, approximate-limit constraint, and Windows-only code path
- `DECISIONS-CliInvoke-process-flaw-mitigations.md#T001` - the native-code mapping that remains the backstop

## Acceptance criteria

- [ ] On Windows, a command line exceeding 32,767 characters throws `ArgumentException` before process start, with the measured length in the message
- [ ] The message states the limit is approximate
- [ ] Non-Windows behavior is unchanged
- [ ] Both the `Arguments` and `ArgumentList` configuration paths are measured
- [ ] A Windows-only test asserts the failure and skips on non-Windows, and passes
- [ ] `guard-configureawait.sh` and `dotnet build src/CliInvoke.sln` pass

## Dependencies

**Blocked by** - [001-start-failure-exception-taxonomy.md](001-start-failure-exception-taxonomy.md) - the blueprint places the check in `BaseProcessControlAdapter.cs` but names `ProcessWrapper.Start()` as the fallback location, and TK001 edits `ProcessWrapper.cs`; sequencing avoids same-file churn, and T004 treats T001's mapping as its backstop.
