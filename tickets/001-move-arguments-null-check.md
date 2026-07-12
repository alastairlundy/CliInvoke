---
title: Move arguments null check to 15-param ctor and delegate 3-param ctor
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CliInvoke-process-configuration-shape.md
---

## Goal

Collapse the two `ProcessConfiguration` ctors into a single canonical shape so that the 15-param ctor is the single source of truth for argument validation, while the public 3-param ctor becomes a thin delegator. This honors the frozen-type contract from the Decision Ledger and eliminates the line 49 dead-code bug in the 3-param ctor body.

## What to build

In `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`, refactor the ctor pair so that:

1. The 15-param ctor (lines 59–101) gains an `ArgumentNullException.ThrowIfNull(arguments);` check placed immediately after the existing `ArgumentException.ThrowIfNullOrEmpty(targetFilePath);` check on line 76.
2. The 3-param ctor (lines 29–57) is reduced to a single `: this(...)` delegation line; its body becomes empty.
3. The line 49 dead-code assignment `RedirectStandardInput = StandardInput != StreamWriter.Null;` is removed (it is always `false` because `StandardInput` is set to `StreamWriter.Null` on line 47 and is moot once the ctor body delegates).

The public 3-param ctor's signature stays at three params (`targetFilePath`, `arguments = ""`, `outputRedirection = true`) with the same defaults. The delegation passes `redirectStandardInput: false` and the caller's `outputRedirection` through to the 15-param ctor.

The exception type and `paramName` for the `arguments` null case are preserved — callers that pass `null` to the 3-param ctor still get `ArgumentNullException` with `paramName = "arguments"` at construction.

## Recommended Workflow

### Step 1 — Add the null check to the 15-param ctor

Where: `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`

- Open the 15-param ctor (lines 59–101).
- Insert `ArgumentNullException.ThrowIfNull(arguments);` directly after line 76's `ArgumentException.ThrowIfNullOrEmpty(targetFilePath);` so the `targetFilePath` check stays first (it is the primary key per `DECISIONS-CliInvoke-process-configuration-shape.md#T001`).

Verify: Reading the file, the 15-param ctor now has two consecutive argument-validation lines on `targetFilePath` and `arguments` before any field assignment.

### Step 2 — Replace the 3-param ctor body with a delegation expression

Where: `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`

- Replace lines 32–57 (the 3-param ctor body) with a single `: this(targetFilePath, arguments, redirectStandardInput: false, outputRedirection: outputRedirection)` constructor initializer per `DECISIONS-CliInvoke-process-configuration-shape.md#D011`.
- Remove the now-redundant `ArgumentNullException.ThrowIfNull(arguments);` from the 3-param ctor (the check is reached via delegation per `DECISIONS-CliInvoke-process-configuration-shape.md#T001`).
- Remove the line 49 dead-code assignment `RedirectStandardInput = StandardInput != StreamWriter.Null;` — it is always `false` and is moot once delegation takes over.

Verify: The 3-param ctor body is empty; the 15-param ctor body is the only place where field assignments and validation live.

### Step 3 — Add a test for `arguments` null behavior

Where: `tests/CliInvoke.Tests/`

- Add a test asserting `new ProcessConfiguration("foo.exe", null)` throws `ArgumentNullException` with `paramName = "arguments"`.
- Add a sibling test confirming `new ProcessConfiguration("foo.exe", "arg1")` does not throw.
- Add a sibling test confirming `new ProcessConfiguration("foo.exe")` (default `arguments = ""`) does not throw.
- Add a sibling test confirming `new ProcessConfiguration(null, "arg1")` throws `ArgumentException` from `ThrowIfNullOrEmpty` on `targetFilePath`.

Verify: `dotnet test` from `tests/CliInvoke.Tests/` (per the AGENTS.md working-directory convention) shows the new test passing and no regressions in existing tests.

### Step 4 — Run the full test suite

Where: N/A

- Run `dotnet test` from `tests/CliInvoke.Tests/` to confirm no regressions across the three target frameworks (net8.0, net9.0, net10.0).

Verify: All existing tests pass; the new test from Step 3 passes.

## Context pointers

**Files**
- `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs` — the file being modified; the 3-param ctor at lines 29–57 and the 15-param ctor at lines 59–101 are the targets. The `Arguments` and `OutputRedirection` setters are addressed by a separate ticket (`002-remove-dead-setters`) and are not touched here.
- `tests/CliInvoke.Tests/Builders/ProcessConfigurationBuilderTests.cs` — adjacent tests, useful as a reference for the test style; the new test goes in this project (path determined by the test framework's existing convention for `ProcessConfiguration` constructor tests).

**ADRs** — None directly relevant. The frozen-type contract is recorded in the Decision Ledger, not an ADR.

**Domain terms**
- Frozen type — a type whose properties are set once at construction and never mutated afterwards, except for properties explicitly designated as resolution slots. In this codebase, `TargetFilePath` is the only resolution slot per `DECISIONS-CliInvoke-process-configuration-shape.md#D001` / `#D004`.
- Canonical ctor — the single ctor that owns field assignment and validation; other ctors delegate to it.

**Ledger records**
- `DECISIONS-CliInvoke-process-configuration-shape.md#D001` — frozen-at-construction contract that this ticket implements by collapsing validation into one place.
- `DECISIONS-CliInvoke-process-configuration-shape.md#D004` — re-opened form of D001; supersedes D001 and confirms the wrapper stays (relevant because the wrapper at `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs:426` continues to call the 15-param ctor directly).
- `DECISIONS-CliInvoke-process-configuration-shape.md#D005` — 15-param ctor stays `protected`; the public 3-param ctor remains the only public ctor.
- `DECISIONS-CliInvoke-process-configuration-shape.md#D011` — the 3-param ctor's delegation shape: `: this(targetFilePath, arguments, redirectStandardInput: false, outputRedirection: outputRedirection)` with an empty body.
- `DECISIONS-CliInvoke-process-configuration-shape.md#T001` — placement rule for the moved null check: immediately after the `targetFilePath` check on line 76.

## Acceptance criteria

- [ ] The 15-param ctor at `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs:59–101` contains `ArgumentNullException.ThrowIfNull(arguments);` placed directly after the existing `ArgumentException.ThrowIfNullOrEmpty(targetFilePath);` on line 76, per `DECISIONS-CliInvoke-process-configuration-shape.md#T001`.
- [ ] The 3-param ctor at lines 29–57 has an empty body and delegates to the 15-param ctor via `: this(targetFilePath, arguments, redirectStandardInput: false, outputRedirection: outputRedirection)`, per `DECISIONS-CliInvoke-process-configuration-shape.md#D011`.
- [ ] The line 49 dead-code assignment `RedirectStandardInput = StandardInput != StreamWriter.Null;` is removed (per `DECISIONS-CliInvoke-process-configuration-shape.md#D011`).
- [ ] The 3-param ctor's null-check is removed from its body (the check is reached via delegation per `DECISIONS-CliInvoke-process-configuration-shape.md#T001`).
- [ ] `new ProcessConfiguration("foo.exe", null)` throws `ArgumentNullException` with `paramName = "arguments"` (preserved public API behavior per `DECISIONS-CliInvoke-process-configuration-shape.md#T001`).
- [ ] `new ProcessConfiguration("foo.exe")` (default `arguments = ""`) does not throw.
- [ ] `new ProcessConfiguration("foo.exe", "arg1")` does not throw.
- [ ] `new ProcessConfiguration(null, "arg1")` throws `ArgumentException` from `ThrowIfNullOrEmpty` on `targetFilePath`.
- [ ] The 15-param ctor remains `protected` and is the only ctor that performs field assignment, per `DECISIONS-CliInvoke-process-configuration-shape.md#D005`.
- [ ] `dotnet test` from `tests/CliInvoke.Tests/` passes with no regressions on net8.0, net9.0, and net10.0.

## Dependencies

**Blocked by** — None; this is a foundation ticket and can start immediately.
