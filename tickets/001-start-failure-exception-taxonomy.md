---
title: Map known start-failure error codes in ProcessWrapper.Start
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-handoff-process-flaws-doc.md
---

# TK001 - Map known start-failure error codes in ProcessWrapper.Start

## Goal

Make start failures report their real cause instead of labeling every non-file-not-found `Win32Exception` as an authorization problem. Known native error codes map to their natural .NET exception types, and unknown codes fall back to a descriptive exception that is actually diagnosable.

## What to build

In `src/CliInvoke/Processes/Internal/ProcessWrapper.cs`, the `Start()` method (lines 159-174) catches `Win32Exception` and maps codes 2 and 3 to `FileNotFoundException`, but maps every other code to `UnauthorizedAccessException`. That mislabels failures such as `ERROR_BAD_EXE_FORMAT` (code 193), which is not a permissions problem.

Change the catch block so that:

- Codes 2 and 3 keep mapping to `FileNotFoundException` with the file path, unchanged.
- Code 5 (`ERROR_ACCESS_DENIED`) explicitly maps to `UnauthorizedAccessException`, preserving today's correct behavior for the common permissions case.
- Code 193 (`ERROR_BAD_EXE_FORMAT`) maps to `BadImageFormatException` with the file path in the message.
- All remaining unknown codes fall back to a descriptive exception that includes the target file path and the `NativeErrorCode`, so previously unlabeled failures are diagnosable from the message.

No new public exception type is introduced. For testability, the codebase context supports extracting the code-to-exception mapping into an internal static helper so it can be unit tested directly - the `CliInvoke` package already grants `InternalsVisibleTo` to `CliInvoke.Tests` (see `src/CliInvoke/CliInvoke.csproj`), and `tests/CliInvoke.Tests/Internal/Helpers/ProcessTestHelper.cs` exercises `ProcessWrapper` internals today.

This change touches invocation code, so load the `cliinvoke-pattern-validator` skill before editing.

## Size

- Files - 2 (1 edit - `ProcessWrapper.cs`, 1 create - test file under `tests/CliInvoke.Tests/`)

## Recommended Workflow

### Step 1 - Review the current catch block and decisions

Where: `src/CliInvoke/Processes/Internal/ProcessWrapper.cs`, `docs/decisions/DECISIONS-CliInvoke-process-flaw-mitigations.md`

- Read the `Start()` catch block and confirm the 2/3 mapping is the only correct behavior to preserve.
- Read the T001 record for the resolved mapping table and constraints.

Verify: You can state which codes map to which exception types and what the fallback must contain.

### Step 2 - Extract the mapping into a testable seam and implement it

Where: `src/CliInvoke/Processes/Internal/ProcessWrapper.cs`

- Extract the code-to-exception mapping into an internal static helper (or keep it inline if a test seam already exists).
- Implement the mappings for codes 5 and 193 and the descriptive fallback carrying the file path and `NativeErrorCode`.

Verify: The 2/3 mapping is byte-for-byte unchanged in behavior; no new public type appears in the package's public API.

### Step 3 - Write unit tests for the mapping

Where: `tests/CliInvoke.Tests/` (new test file, e.g. under `Processes/`)

- Test that code 193 yields `BadImageFormatException` with the file path in the message.
- Test that an unknown code (e.g. 206) yields the fallback exception whose message contains the `NativeErrorCode` and the file path.
- Test that codes 2 and 3 still yield `FileNotFoundException`.
- Construct `Win32Exception` instances directly against the extracted helper rather than spawning real executables.

Verify: `dotnet test` in `tests/CliInvoke.Tests/` passes the new tests.

### Step 4 - Run the inner-loop gates

Where: repo root

- Run `bash scripts/guard-configureawait.sh src` (the ConfigureAwait guard is a hard gate, though this change is synchronous).
- Run `dotnet build src/CliInvoke.sln`.

Verify: Guard and build pass with no new warnings.

## Context pointers

### Files

- `src/CliInvoke/Processes/Internal/ProcessWrapper.cs` - the `Start()` catch block to modify (lines 159-174)
- `tests/CliInvoke.Tests/Internal/Helpers/ProcessTestHelper.cs` - shows the existing pattern for exercising `ProcessWrapper` internals from tests
- `src/CliInvoke/CliInvoke.csproj` - confirms the `InternalsVisibleTo` grant to `CliInvoke.Tests`

### ADRs

- `docs/adr/0001-ivt-minimization.md` - no new grant is needed here (one exists); do not add grants as a side effect

### Domain terms

- Resource-Owning Type (GLOSSARY.md) - `ProcessWrapper` extends `Process` and manages OS resources; keep the change inside the existing lifecycle conventions

### Ledger records

- `DECISIONS-CliInvoke-process-flaw-mitigations.md#D001` - session scope covering the five mitigation items
- `DECISIONS-CliInvoke-process-flaw-mitigations.md#T001` - the resolved mapping table and the no-new-public-exception-type constraint
- `DECISIONS-CliInvoke-process-flaw-mitigations.md#T004` - its length-check ticket treats this mapping as the backstop for over-limit errors that slip through

## Acceptance criteria

- [ ] Code 5 maps to `UnauthorizedAccessException`; code 193 maps to `BadImageFormatException` with the file path in the message
- [ ] Unknown codes produce a descriptive exception whose message includes the file path and the `NativeErrorCode`
- [ ] Codes 2 and 3 still produce `FileNotFoundException` with unchanged behavior
- [ ] No new public exception type is added (T001 constraint)
- [ ] Unit tests cover the 193 mapping and the fallback message content, and pass
- [ ] `guard-configureawait.sh` and `dotnet build src/CliInvoke.sln` pass

## Dependencies

**Blocked by** - None - can start immediately
