---
title: PowershellProcessConfiguration TargetFilePath delegation
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-bug-audit-fixes.md
---

## Goal

Make the public `TargetFilePath` property on the PowerShell specialization truthful by delegating to the base configuration's per-OS resolution.

## What to build

- Replace the never-assigned auto-property (reported at `src/CliInvoke.Specializations/Configurations/PowershellProcessConfiguration.cs:88`) with `public new string TargetFilePath => base.TargetFilePath;` — the base constructor already resolves pwsh per OS (operating system) (DECISIONS-CliInvoke-bug-audit-fixes.md#T014).
- Must not preclude the v4 init conversion of both specializations (v4 ledger record T005).
- The Cmd constructor re-assign quirk (reported at `:71`) is untouched — separate cleanup if ever wanted.

## Size

- **Files** - 1

## Recommended Workflow

### Step 1 - Confirm the property and base resolution

Where: src/CliInvoke.Specializations/Configurations/PowershellProcessConfiguration.cs

- Read the file; confirm the never-assigned auto-property and that the base constructor resolves the target per OS.

Verify: the current property never returns the resolved path (confirming the defect).

### Step 2 - Replace with the delegating property

Where: src/CliInvoke.Specializations/Configurations/PowershellProcessConfiguration.cs

- Replace the auto-property with the expression-bodied `new` property delegating to `base.TargetFilePath`.

Verify: the property returns the base-resolved path for the current OS.

### Step 3 - Build and test

Where: N/A

- Build the solution and run the Specializations test project.

Verify: dotnet test passes for CliInvoke.Specializations.Tests.

## Context pointers

##### Files

- `src/CliInvoke.Specializations/Configurations/PowershellProcessConfiguration.cs` - the only file this ticket edits

##### Domain terms

- Entrypoint package - Specializations is a distinct consumer entrypoint; the fix keeps its public surface minimal (a delegating property, no new state) so the planned v4 init conversion stays unobstructed.

##### Ledger records

- `DECISIONS-CliInvoke-bug-audit-fixes.md#T014` - expression-bodied new property delegating to base; Cmd quirk untouched; must not preclude the v4 init conversion

## Acceptance criteria

- [ ] `TargetFilePath` is an expression-bodied `new` property delegating to `base.TargetFilePath` (T014)
- [ ] The v4 init conversion is not precluded; the Cmd constructor quirk is untouched
- [ ] Build and Specializations tests pass

## Dependencies

**Blocked by** - None - can start immediately
