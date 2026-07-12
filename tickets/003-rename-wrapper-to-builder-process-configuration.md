---
title: Rename ProcessConfigurationWrapper to BuilderProcessConfiguration
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CliInvoke-process-configuration-shape.md
---

## Goal

Rename the internal `ProcessConfigurationWrapper` subclass to `BuilderProcessConfiguration` for a self-documenting type name that reflects its only consumer (`ProcessConfigurationBuilder`). The class stays `internal`, the file path is unchanged, and the `outputRedirection = false` ctor default is preserved as a deliberate builder-centric divergence from the 15-param ctor's `true` default.

## What to build

In `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`:

1. Line 426: rename `internal class ProcessConfigurationWrapper : ProcessConfiguration` to `internal class BuilderProcessConfiguration : ProcessConfiguration`.
2. Line 428: rename the ctor's name from `ProcessConfigurationWrapper` to `BuilderProcessConfiguration`. The `outputRedirection = false` default on line 430 is preserved (it is a deliberate builder-centric choice per `DECISIONS-CliInvoke-process-configuration-shape.md#T002`, not a bug to fix).
3. Line 408: update the call site to `BuilderProcessConfiguration configuration = new(_targetFilePath, arguments, ...);` so it matches the renamed type.
4. The `TargetFilePath` setter visibility is untouched — it remains `public` per `DECISIONS-CliInvoke-process-configuration-shape.md#D013` (the rename does not affect the resolution-slot contract).

The file remains `ProcessConfigurationBuilder.cs`; only the type name, ctor name, and call site change.

## Recommended Workflow

### Step 1 — Verify no external references to the old name

Where: N/A (repo-wide grep)

- Run a grep for `ProcessConfigurationWrapper` across `src/`, `tests/`, and `benchmarks/`.
- Confirm the only matches are at `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs:408` (call site) and lines 426, 428 (declaration and ctor).

Verify: Grep returns three matches, all inside `ProcessConfigurationBuilder.cs`.

### Step 2 — Rename the class declaration, ctor, and call site

Where: `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`

- Rename line 426's `ProcessConfigurationWrapper` to `BuilderProcessConfiguration`.
- Rename line 428's ctor name from `ProcessConfigurationWrapper` to `BuilderProcessConfiguration`.
- Update line 408's call site to `BuilderProcessConfiguration configuration = new(_targetFilePath, arguments, _redirectStandardInput, _outputRedirection, _workingDirectoryPath, _requiresAdministratorPrivileges, environmentVariables, credential, _standardInput, _standardInputEncoding, _standardOutputEncoding, _standardErrorEncoding, resourcePolicy, _enableWindowCreation, _useShellExecution);`.
- Leave the `outputRedirection = false` default on line 430 untouched — the divergence from the 15-param ctor's `true` default is intentional per `DECISIONS-CliInvoke-process-configuration-shape.md#T002`.

Verify: A repo-wide grep for `ProcessConfigurationWrapper` returns zero matches.

### Step 3 — Build and run tests

Where: N/A

- Run `dotnet test` from `tests/CliInvoke.Tests/` per the AGENTS.md working-directory convention.
- Confirm the build succeeds and no existing tests regress.

Verify: All existing tests pass on net8.0, net9.0, and net10.0.

## Context pointers

**Files**
- `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs` — the file being modified; the class declaration is on line 426, the ctor on line 428, the call site on line 408, and the CA1416 pragma on lines 18 and 445 (unaffected by this change).
- `tests/CliInvoke.Tests/Builders/ProcessConfigurationBuilderTests.cs` — adjacent tests; no direct reference to `ProcessConfigurationWrapper` is expected.

**ADRs** — None directly relevant.

**Domain terms**
- Wrapper — in this codebase, an internal subclass of `ProcessConfiguration` whose only purpose is to call the `protected` 15-param ctor from a different assembly (`CliInvoke` vs `CliInvoke.Core`). The wrapper is the legitimate cross-assembly access path; a future direction may eliminate it (`DECISIONS-CliInvoke-process-configuration-shape.md#D006`, deferred).

**Ledger records**
- `DECISIONS-CliInvoke-process-configuration-shape.md#D004` — re-opened form of D001; confirms the wrapper stays in renamed form and is the only consumer-facing access path from `CliInvoke` to the protected 15-param ctor.
- `DECISIONS-CliInvoke-process-configuration-shape.md#D005` — 15-param ctor stays `protected`; the rename does not introduce `InternalsVisibleTo` and the access path remains the wrapper.
- `DECISIONS-CliInvoke-process-configuration-shape.md#D007` — the rename itself: `ProcessConfigurationWrapper` becomes `BuilderProcessConfiguration`, the class stays `internal`, the file path is unchanged, and only the declaration (line 426), ctor name (line 428), and call site (line 408) are touched.
- `DECISIONS-CliInvoke-process-configuration-shape.md#D013` — `TargetFilePath` setter visibility remains `public`; the rename does not affect the resolution-slot contract.
- `DECISIONS-CliInvoke-process-configuration-shape.md#T002` — the wrapper's `outputRedirection = false` default is deliberate and must not be "fixed" to match the 15-param ctor's `true` default. The wrapper's other 11 defaults match the 15-param ctor's defaults; `outputRedirection` is the only intentional divergence.

## Acceptance criteria

- [ ] A repo-wide grep for `ProcessConfigurationWrapper` returns zero matches after the change.
- [ ] Line 426 reads `internal class BuilderProcessConfiguration : ProcessConfiguration`.
- [ ] Line 428's ctor is named `BuilderProcessConfiguration` and its `outputRedirection = false` default on line 430 is preserved, per `DECISIONS-CliInvoke-process-configuration-shape.md#T002`.
- [ ] Line 408's call site uses `BuilderProcessConfiguration` as the type of the local variable.
- [ ] The class remains `internal` and continues to subclass `ProcessConfiguration`, per `DECISIONS-CliInvoke-process-configuration-shape.md#D005` and `#D007`.
- [ ] The file path is unchanged — the class still lives in `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`.
- [ ] `TargetFilePath` setter visibility remains `public`, per `DECISIONS-CliInvoke-process-configuration-shape.md#D013`.
- [ ] `dotnet test` from `tests/CliInvoke.Tests/` passes with no regressions on net8.0, net9.0, and net10.0.

## Dependencies

**Blocked by** — None; this ticket is independent of the ctor-delegation work (`001-move-arguments-null-check`) and the setter-removal work (`002-remove-dead-setters`). It can run in parallel with either. The follow-on remarks-block work (`004-add-remarks-block-to-wrapper`) is blocked by this rename.
