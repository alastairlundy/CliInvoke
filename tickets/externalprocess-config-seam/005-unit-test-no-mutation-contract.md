---
title: Unit test asserting no-mutation contract on ExternalProcess
classification: Independent
blocked_by: ["004-resolve-filepath-externalprocess-start"]
parent: IMPLEMENTATION-externalprocess-config-seam.md
---

## Goal

Add a CI-runnable unit test that proves the no-mutation contract from D002 — `Configuration.TargetFilePath` is unchanged after `Start()` and `StartAsync()`, and the result's `ExecutedFilePath` reflects the resolved path.

## What to build

New file `tests/CliInvoke.Tests/Processes/ExternalProcessNoMutationTests.cs` (or similar location consistent with the existing test layout — check `tests/CliInvoke.Tests/` for the right folder; existing folders include `Primitives/`, `Invokers/`, `Resolvers/`).

The test:

1. Constructs `var config = new ProcessConfiguration("dotnet"); var process = new ExternalProcess(config);`.
2. Captures `config.TargetFilePath` as a string before calling `Start()` / `StartAsync()`.
3. Calls `Start()` (and separately `StartAsync(default)`); then calls `WaitForExitOrTimeoutAsync(default)` (or `CaptureBufferedResultAsync(default)`) to obtain a `ProcessResult`.
4. Asserts `config.TargetFilePath` is still `"dotnet"` (no mutation).
5. Asserts `result.ExecutedFilePath` equals the resolved path from `IFilePathResolver` (resolve `"dotnet"` via a fresh `FilePathResolver` and compare `FullName` strings).

The fast-exiting-process race is handled by the existing `ProcessWrapper` guard (see `ProcessWrapper.cs:90` comment). Use TUnit per `AGENTS.md §Testing`.

## Size

- Files: 1 (new)

## Recommended Workflow

### Step 1 — Pick the test location

Where: `tests/CliInvoke.Tests/`

- Inspect the existing test folder layout (e.g., `Primitives/`, `Invokers/`, `Resolvers/`) and place the new file under a `Processes/` or `ExternalProcess/` folder consistent with that layout.
- Verify: Folder chosen follows existing convention.

### Step 2 — Write the no-mutation test

Where: the new test file

- Construct `var config = new ProcessConfiguration("dotnet"); var process = new ExternalProcess(config);`.
- Capture `config.TargetFilePath` as a string before calling `Start()` / `StartAsync()`.
- Call `Start()` (and separately `StartAsync(default)`); call `WaitForExitOrTimeoutAsync(default)` (or `CaptureBufferedResultAsync(default)`) to obtain a `ProcessResult`.
- Assert `config.TargetFilePath` is still `"dotnet"`.
- Assert `result.ExecutedFilePath` equals the resolved path from `IFilePathResolver` (resolve `"dotnet"` via a fresh `FilePathResolver` and compare `FullName` strings).
- Verify: Test fails on the pre-TK004 implementation (the write-back on `ExternalProcess.cs:127`); passes on the TK004 implementation.

### Step 3 — Run the test

Where: `tests/CliInvoke.Tests/`

- Run `dotnet test tests/CliInvoke.Tests/` (or filter to the new test class).
- Verify: Test passes; no existing tests regress.

## Context pointers

- Files:
  - `tests/CliInvoke.Tests/CliRunTests.cs` and `tests/CliInvoke.Tests/Invokers/ProcessInvokerTests.cs` — existing patterns for spawning `dotnet` as a test executable
  - `tests/CliInvoke.Tests/TestData/TestFixture.cs` — shared test fixtures to reuse if applicable
  - `src/CliInvoke/FilePathResolver.cs` — the resolver used to obtain the expected `ExecutedFilePath`
- Domain terms: "Resource-Owning Type" (GLOSSARY.md — `ProcessConfiguration` is a resource-owning type; the test should not leak it)
- Ledger records:
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#T005` — unit test on `ExternalProcess`
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#T011` — `dotnet --info` test stub
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#D002` — no-mutation contract under test

## Acceptance criteria

- [ ] A new TUnit test exists in `tests/CliInvoke.Tests/` (path chosen per existing convention).
- [ ] The test asserts `Configuration.TargetFilePath` is unchanged after `Start()` and `StartAsync()`.
- [ ] The test asserts `result.ExecutedFilePath` equals the resolved path from `IFilePathResolver`.
- [ ] The test fails when run against the pre-TK004 `ExternalProcess.cs` (write-back present).
- [ ] The test passes when run against the post-TK004 `ExternalProcess.cs`.
- [ ] No existing tests regress.

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** — 004-resolve-filepath-externalprocess-start (test exercises the rewritten Start/StartAsync; without TK004, the assertion `TargetFilePath == "dotnet"` after Start is impossible because the pre-TK004 code mutates it).
