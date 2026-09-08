---
title: Core primitives equality contracts and property tests
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-bug-audit-fixes.md
---

## Goal

Establish one equality convention across the library's public result, configuration, exception-info, credential, and shell-information types, with property-based test coverage.

## What to build

All edits are in `src/CliInvoke.Core/Primitives` unless noted.

- `ProcessResult` `==`/`!=` (reported at `:195-212`) become null-safe — `null == null` true, `null == x` false, never throwing (DECISIONS-CliInvoke-bug-audit-fixes.md#T008).
- `BufferedProcessResult` null-safe operators (reported at `:168-178`) and `Equals`/`GetHashCode` include `ProcessId` (reported at `:99-151`), matching the base contract (DECISIONS-CliInvoke-bug-audit-fixes.md#T008, DECISIONS-CliInvoke-bug-audit-fixes.md#T009). Stricter equality accepted — the same command re-run with a different process id compares unequal.
- `ProcessConfiguration` `==`/`!=` (reported at `:341-361`) become null-safe (DECISIONS-CliInvoke-bug-audit-fixes.md#T010).
- `ProcessExceptionInfo<TProcessResult>` `==`/`!=` (reported at `:178-199`) become null-safe (DECISIONS-CliInvoke-bug-audit-fixes.md#T010).
- `UserCredential.GetHashCode` (reported at `:175-189`) hashes `Domain`, `UserName`, and `LoadUserProfile` only; `Password` participates in `Equals` content comparison but never in `GetHashCode`; XML documentation notes possible collisions for distinct passwords with the same user/domain (DECISIONS-CliInvoke-bug-audit-fixes.md#T011). No SecureString unwrapping in hash paths.
- `ShellInformation.Equals` includes `Version`, matching `GetHashCode` (DECISIONS-CliInvoke-bug-audit-fixes.md#T012).

Accepted behavior changes: `null == null` changes from a thrown exception to true for the result types and from false to true for the configuration and exception-info types; the `UserCredential` hash changes; `BufferedProcessResult` equality becomes stricter.

## Size

- **Files** - 11 (6 source edits, 4 test edits, 1 new property-test file)

## Recommended Workflow

### Step 1 - Confirm all operator and hash sites

Where: src/CliInvoke.Core/Primitives/

- Read `ProcessResult.cs`, `BufferedProcessResult.cs`, `ProcessConfiguration.cs`, `ProcessExceptionInfo.cs`, `UserCredential.cs`, `ShellInformation.cs`; confirm the reported line ranges still hold.

Verify: all six sites identified.

### Step 2 - Implement the null-safe operators

Where: src/CliInvoke.Core/Primitives/ProcessResult.cs, BufferedProcessResult.cs, ProcessConfiguration.cs, ProcessExceptionInfo.cs

- Apply the standard null-safe pattern to all four `==`/`!=` pairs: `null == null` true, `null == x` false, never throwing.

Verify: unit checks for both null operands against each type.

### Step 3 - Add ProcessId to BufferedProcessResult equality

Where: src/CliInvoke.Core/Primitives/BufferedProcessResult.cs

- Include `ProcessId` in `Equals` and `GetHashCode`, matching the base `ProcessResult` contract.

Verify: instances differing only by process id compare unequal and hash differently.

### Step 4 - Rework the UserCredential hash source

Where: src/CliInvoke.Core/Primitives/UserCredential.cs

- Hash `Domain`, `UserName`, and `LoadUserProfile` only; exclude `Password` from `GetHashCode` while keeping it in `Equals`.
- Add the XML collision note (distinct passwords with the same user/domain may share a hash).

Verify: no password material enters any hash path; `Equals` still distinguishes passwords.

### Step 5 - Include Version in ShellInformation equality

Where: src/CliInvoke.Core/Primitives/ShellInformation.cs

- Add `Version` to `Equals` so it matches `GetHashCode`.

Verify: instances differing only by version compare unequal.

### Step 6 - Extend equality suites and add property tests

Where: tests/CliInvoke.Tests/Primitives/ProcessResultEqualityTests.cs, tests/CliInvoke.Tests/BufferedProcessResultEqualityTests.cs, tests/CliInvoke.Tests/Primitives/UserCredentialTests.cs, tests/CliInvoke.Tests/Primitives/ProcessConfigurationTests.cs, plus a new FsCheck property-test file

- Extend the four existing suites with the new semantics (DECISIONS-CliInvoke-bug-audit-fixes.md#T027).
- Add FsCheck property tests for the equality contracts (reflexivity, symmetry, transitivity, hash consistency) — FsCheck is already referenced in tests/CliInvoke.Tests/CliInvoke.Tests.csproj, so no package addition is needed.

Verify: dotnet test passes including property tests.

## Context pointers

##### Files

- `src/CliInvoke.Core/Primitives/ProcessResult.cs`, `BufferedProcessResult.cs`, `ProcessConfiguration.cs`, `ProcessExceptionInfo.cs`, `UserCredential.cs`, `ShellInformation.cs` - the six source files
- The four equality test suites listed in Step 6 - existing regression coverage to extend

##### Domain terms

- Resource-Owning Type - `UserCredential` holds sensitive data in memory; the hash rework exists precisely so secret material is never unwrapped or hashed.

##### Ledger records

- `DECISIONS-CliInvoke-bug-audit-fixes.md#T008` - standard null-safe semantics for ProcessResult and BufferedProcessResult operators
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T009` - ProcessId included in BufferedProcessResult Equals/GetHashCode
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T010` - same null-safe pattern for ProcessConfiguration and ProcessExceptionInfo
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T011` - hash non-secret fields only; collision note in XML docs; no SecureString unwrapping
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T012` - Version in ShellInformation.Equals
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T001`, `DECISIONS-CliInvoke-bug-audit-fixes.md#T027` - existing files and packages only; regression plus FsCheck property tests

## Acceptance criteria

- [ ] All four operator pairs implement standard null-safe equality — `null == null` true, `null == x` false, never throwing (T008, T010)
- [ ] `BufferedProcessResult.Equals`/`GetHashCode` include `ProcessId`, matching the base contract (T009)
- [ ] `UserCredential.GetHashCode` hashes `Domain`, `UserName`, and `LoadUserProfile` only; `Password` never enters the hash; collision note present in XML docs (T011)
- [ ] `ShellInformation.Equals` considers `Version`, matching `GetHashCode` (T012)
- [ ] Regression suites extended and FsCheck property tests pass (T027)

## Dependencies

**Blocked by** - None - can start immediately
