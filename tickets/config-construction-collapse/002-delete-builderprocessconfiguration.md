---
title: Delete BuilderProcessConfiguration bridge and call internal ctor directly from ProcessConfigurationBuilder
classification: Independent
blocked_by: ["001-internal-15param-ctor"]
parent: IMPLEMENTATION-config-construction.md
---

## Goal

Eliminate the `BuilderProcessConfiguration` bridge subclass by calling the (now-internal) 15-parameter `ProcessConfiguration` constructor directly from `ProcessConfigurationBuilder.Build()`. The cross-constraint check at `Build():424–426` is preserved unchanged.

## What to build

In `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`:

1. In `Build()` at lines 435–441, replace the `new BuilderProcessConfiguration(...)` call with `new ProcessConfiguration(...)`. The argument list is identical (`_targetFilePath, arguments, _redirectStandardInput, _outputRedirection, _workingDirectoryPath, _requiresAdministratorPrivileges, environmentVariables, credential, _standardInput, _standardInputEncoding, _standardOutputEncoding, _standardErrorEncoding, resourcePolicy, _enableWindowCreation, _useShellExecution`); the local variable type also changes from `BuilderProcessConfiguration` to `ProcessConfiguration`.
2. Delete the `BuilderProcessConfiguration` class at lines 468–485.
3. Delete the accompanying `<remarks>` doc block for `BuilderProcessConfiguration` at lines 453–467 (the four-paragraph comment that explains the cross-assembly hack, warns against careless deletion, and notes that a long-term solution is being developed).
4. Leave the cross-constraint check at lines 424–426 (`_useShellExecution && (_redirectStandardInput || _standardInput != StreamWriter.Null)` throwing `ArgumentException`) unchanged — that check stays in `Build()` per T009.

After this change, the builder relies on the existing `CliInvoke.Core` → `CliInvoke` `InternalsVisibleTo` grant (`src/CliInvoke.Core/CliInvoke.Core.csproj:57`) to call the internal ctor directly. No new `InternalsVisibleTo` is added.

## Size

- Files: 1

## Recommended Workflow

### Step 1 — Verify bridge subclass is referenced only in this file

Where: `src/`

- Run a grep for `BuilderProcessConfiguration` across `src/`.
- Verify: Matches are limited to lines 435 (the ctor call in `Build()`) and 468–485 (the class definition + its doc block at 453–467) in this file. No external references; safe to delete both the call site and the class.

### Step 2 — Replace `new BuilderProcessConfiguration(...)` with `new ProcessConfiguration(...)` in `Build()`

Where: `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`

- Edit line 435: change the local variable declaration `BuilderProcessConfiguration configuration = new(...)` to `ProcessConfiguration configuration = new(...)`. The argument list (lines 435–439) is unchanged.
- Verify: Line 435 now reads `ProcessConfiguration configuration = new(_targetFilePath, arguments, ...)`; the build of `CliInvoke` succeeds against the now-internal ctor from TK001.

### Step 3 — Delete the bridge class and its doc block

Where: `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`

- Delete lines 453–485 (the `<summary>` doc block + the `internal class BuilderProcessConfiguration : ProcessConfiguration` class declaration + its 15-param ctor).
- Verify: A grep for `BuilderProcessConfiguration` across `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs` returns no matches; the file no longer references the bridge subclass.

### Step 4 — Build the full solution and confirm no regressions

Where: `src/CliInvoke.sln`

- Run `dotnet build src/CliInvoke.sln`.
- Verify: Build clean. `ProcessConfigurationBuilder.Build()` still produces a valid `ProcessConfiguration` via the internal ctor; the cross-constraint check at `Build():424–426` still throws `ArgumentException` when the prohibited combination is configured.

## Context pointers

- Files:
  - `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs` — target of the change (`Build()` at lines 422–442, class to delete at lines 468–485, doc block to delete at lines 453–467)
  - `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs:54–69` — the internal 15-param ctor from TK001 that this ticket now calls directly
  - `src/CliInvoke.Core/CliInvoke.Core.csproj:57` — the existing `InternalsVisibleTo("CliInvoke")` grant that authorises this cross-assembly call
- Domain terms:
  - "Process Invocation Pipeline" (from `GLOSSARY.md`) — `ProcessConfigurationBuilder` is the canonical builder for assembling a `ProcessConfiguration` that flows through the pipeline; this change preserves the builder's external surface; do not reproduce the glossary entry
- Ledger records:
  - `DECISIONS-CliInvoke-config-construction.md#T004` — bridge elimination via internal ctor; `BuilderProcessConfiguration` deleted; the builder calls the internal 15-param ctor directly
  - `DECISIONS-CliInvoke-config-construction.md#I001` — clarification: the bridge was a workaround for cross-assembly access to the protected ctor; the internal ctor + existing `InternalsVisibleTo` replaces it
  - `DECISIONS-CliInvoke-config-construction.md#T009` — the cross-constraint check (`shell execution + standard input redirection`) stays in `Build()`; it is not relocated to the factory
  - `DECISIONS-CliInvoke-config-construction.md#T011` — no new `InternalsVisibleTo` is added; the existing Core → CliInvoke grant authorises the cross-assembly call

## Acceptance criteria

- [ ] `Build()` at lines 435–441 constructs `ProcessConfiguration` directly via `new ProcessConfiguration(...)`, not `new BuilderProcessConfiguration(...)` (per `DECISIONS-CliInvoke-config-construction.md#T004`).
- [ ] The `BuilderProcessConfiguration` class at lines 468–485 and its `<remarks>` doc block at lines 453–467 are deleted (per `DECISIONS-CliInvoke-config-construction.md#T004`).
- [ ] A grep for `BuilderProcessConfiguration` across `src/` returns no matches.
- [ ] The cross-constraint check at `Build():424–426` (`_useShellExecution && (_redirectStandardInput || _standardInput != StreamWriter.Null)` throwing `ArgumentException`) is preserved unchanged (per `DECISIONS-CliInvoke-config-construction.md#T009`).
- [ ] No new `InternalsVisibleTo` declarations are added in `src/CliInvoke.Core/CliInvoke.Core.csproj`; the existing Core → `CliInvoke` grant authorises the cross-assembly call (per `DECISIONS-CliInvoke-config-construction.md#T011`).
- [ ] `dotnet build src/CliInvoke.sln` succeeds.

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** — `001-internal-15param-ctor` (semantic coupling: this ticket deletes the bridge subclass and replaces its ctor call with a direct call to the internal 15-param ctor; TK001 must land first or the build will not find the ctor from `CliInvoke`).
