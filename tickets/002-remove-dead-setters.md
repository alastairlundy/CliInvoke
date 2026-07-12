---
title: Remove dead setters on Arguments and OutputRedirection
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CliInvoke-process-configuration-shape.md
---

## Goal

Honor the frozen-type contract from the Decision Ledger by removing the protected setters on `Arguments` and `OutputRedirection`, leaving `TargetFilePath` as the only mutable property on `ProcessConfiguration`. This is a dead-surface cleanup with no behavior change.

## What to build

In `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`:

1. Change line 121 from `public string Arguments { get; protected set; }` to `public string Arguments { get; }`.
2. Change line 168 from `public bool OutputRedirection { get; protected set; }` to `public bool OutputRedirection { get; }`.

The ctors continue to set the backing fields via direct field assignment in their bodies, which is permitted for `{ get; }` auto-properties. No production, test, or benchmark code calls these setters (verified before the change per `DECISIONS-CliInvoke-process-configuration-shape.md#D012`).

## Recommended Workflow

### Step 1 — Verify no callers use the setters

Where: N/A (repo-wide grep)

- Run a grep for `.Arguments =` and `.OutputRedirection =` across `src/`, `tests/`, and `benchmarks/`.
- Confirm zero matches in production, test, or benchmark code before removing the setters, per `DECISIONS-CliInvoke-process-configuration-shape.md#D012`.

Verify: Grep returns no matches; the setters are confirmed dead surface area.

### Step 2 — Remove the protected setters

Where: `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`

- Change line 121 from `public string Arguments { get; protected set; }` to `public string Arguments { get; }`.
- Change line 168 from `public bool OutputRedirection { get; protected set; }` to `public bool OutputRedirection { get; }`.

Verify: The two properties now declare only `{ get; }`; reading the file confirms no other lines reference `protected set` for these properties.

### Step 3 — Confirm the ctors still compile and tests pass

Where: N/A

- Run `dotnet test` from `tests/CliInvoke.Tests/` per the AGENTS.md working-directory convention.
- The ctors set the implicit backing fields directly, which remains legal for `{ get; }` auto-properties.

Verify: The build succeeds and no existing tests regress on net8.0, net9.0, and net10.0.

## Context pointers

**Files**
- `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs` — the file being modified; lines 121 and 168 are the targets.

**ADRs** — None directly relevant.

**Domain terms**
- Frozen type — a type whose properties are set once at construction and never mutated afterwards, except for properties explicitly designated as resolution slots. `TargetFilePath` is the only resolution slot per `DECISIONS-CliInvoke-process-configuration-shape.md#D001` / `#D004`.

**Ledger records**
- `DECISIONS-CliInvoke-process-configuration-shape.md#D001` — frozen-at-construction contract; this ticket enforces it by removing the last two setters that are not on the resolution slot.
- `DECISIONS-CliInvoke-process-configuration-shape.md#D004` — re-opened form of D001; confirms the contract is in force and the wrapper (separately renamed) continues to call the 15-param ctor directly.
- `DECISIONS-CliInvoke-process-configuration-shape.md#D012` — explicit decision to remove the `Arguments` and `OutputRedirection` protected setters; the only in-scope change is the setter visibility, not the property type or any constructor.

## Acceptance criteria

- [ ] A repo-wide grep for `.Arguments =` and `.OutputRedirection =` returns zero matches in `src/`, `tests/`, and `benchmarks/` before the change, per `DECISIONS-CliInvoke-process-configuration-shape.md#D012`.
- [ ] Line 121 reads `public string Arguments { get; }` after the change.
- [ ] Line 168 reads `public bool OutputRedirection { get; }` after the change.
- [ ] The build succeeds with the new property declarations.
- [ ] The 15-param ctor at lines 59–101 continues to assign the backing fields via direct field assignment, which is permitted for `{ get; }` auto-properties.
- [ ] `TargetFilePath` remains the only property with a setter, per `DECISIONS-CliInvoke-process-configuration-shape.md#D001` / `#D004`.
- [ ] `dotnet test` from `tests/CliInvoke.Tests/` passes with no regressions on net8.0, net9.0, and net10.0.

## Dependencies

**Blocked by** — None; this ticket is independent of the ctor-delegation work (`001-move-arguments-null-check`) and the rename work (`003-rename-wrapper-to-builder-process-configuration`). It can run in parallel with either.
