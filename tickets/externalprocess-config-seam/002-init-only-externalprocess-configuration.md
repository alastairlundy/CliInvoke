---
title: Init-only ExternalProcess.Configuration and IExternalProcess.Configuration
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-externalprocess-config-seam.md
---

## Goal

Make the `Configuration` property init-only on `IExternalProcess`, `ExternalProcess`, and `ISuspendableExternalProcess` so post-construction reassignment of the configuration reference is impossible at compile time.

## What to build

In `src/CliInvoke.Core/Processes/IExternalProcess.cs`, change `ProcessConfiguration Configuration { get; set; }` (currently at line 21) to `ProcessConfiguration Configuration { get; init; }`. In `src/CliInvoke/Processes/ExternalProcess.cs`, change `public ProcessConfiguration Configuration { get; set; }` (currently at line 85) to `public ProcessConfiguration Configuration { get; init; }`. Verify `ISuspendableExternalProcess` (in `src/CliInvoke.Core/Processes/ISuspendableExternalProcess.cs`) inherits the change via its `IExternalProcess` base — no separate setter override should exist there. The `ExitConfiguration` property remains `{ get; set; }` per the blueprint (no scope creep).

This is the type-system enforcement of the broader no-mutation philosophy: callers cannot swap the `Configuration` instance out from under the `ExternalProcess` after construction.

## Size

- Files: 3

## Recommended Workflow

### Step 1 — Flip Configuration accessor on interface

Where: `src/CliInvoke.Core/Processes/IExternalProcess.cs`

- Change `ProcessConfiguration Configuration { get; set; }` to `{ get; init; }`.
- Verify: The interface declaration compiles.

### Step 2 — Flip Configuration accessor on implementation

Where: `src/CliInvoke/Processes/ExternalProcess.cs`

- Change `public ProcessConfiguration Configuration { get; set; }` to `{ get; init; }`.
- Verify: Build of `src/CliInvoke/CliInvoke.csproj` succeeds against the modified interface.

### Step 3 — Verify ISuspendableExternalProcess alignment

Where: `src/CliInvoke.Core/Processes/ISuspendableExternalProcess.cs`

- Confirm `ISuspendableExternalProcess` does not declare its own `Configuration` property or override the setter. If it does, mirror the change.
- Verify: `dotnet build src/CliInvoke.sln` succeeds with no setter override warnings.

### Step 4 — Build the full solution

Where: `src/CliInvoke.sln`

- Run `dotnet build src/CliInvoke.sln` to confirm the init-only change ripples across all `IExternalProcess` implementations (none currently exist outside `ExternalProcess` per the blueprint's `ISuspendableExternalProcess` alignment check, but verify).
- Verify: Build clean.

## Context pointers

- Files:
  - `src/CliInvoke.Core/Processes/IExternalProcess.cs` (interface target)
  - `src/CliInvoke/Processes/ExternalProcess.cs` (concrete target)
  - `src/CliInvoke.Core/Processes/ISuspendableExternalProcess.cs` (verification only)
- Domain terms: "Process Invocation Pipeline" (GLOSSARY.md — background only)
- Ledger records:
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#T010` — `Configuration` setter is init-only
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#D002` — no-mutation contract rationale

## Acceptance criteria

- [ ] `IExternalProcess.Configuration` is declared `{ get; init; }`.
- [ ] `ExternalProcess.Configuration` is declared `{ get; init; }`.
- [ ] `ISuspendableExternalProcess` has no separate `Configuration` setter override.
- [ ] Assigning `externalProcess.Configuration = newConfig;` does not compile.
- [ ] `dotnet build src/CliInvoke.sln` succeeds.

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** — None
