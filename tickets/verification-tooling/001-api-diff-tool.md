---
title: Scripted public-API diff tool
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-v3-stable-readiness.md
---

# Ticket — Scripted public-API diff tool

## Goal

Build `tools/api-diff.ps1`, a one-off script that extracts the public API surface of the v2 and v3 assemblies by reflection, diffs them, and emits a checklist mapping every delta to at least one migration-guide entry. This provides the mechanical evidence for readiness gate criterion 1.

## What to build

Create `tools/api-diff.ps1` at the repo root's `tools/` directory. The script:

1. Takes a path to a v2.x assembly and a path to a v3 assembly.
2. Extracts the public surface of each (types, members, and their signatures) — reflection-based extraction is the chosen method; a throwaway console program using `System.Reflection.Metadata` is acceptable as the extraction engine if pure PowerShell reflection is impractical.
3. Diffs the two surfaces and emits each delta (removed types, removed members, changed signatures, added `required` members).
4. Emits a checklist template with one row per delta, intended to be mapped against the guide's TL;DR table in `site/docs/migration-guides/3.0.0.md`.

The script is one-off tooling — no new NuGet package dependencies are added to the repository, and no solution or project changes are made. The delta list must come from the assemblies themselves, not from prose, so the audit's coverage claim is mechanical.

## Size

- **Files** - 1 file to create (`tools/api-diff.ps1`, plus a throwaway extraction program outside the repo tree if needed)

## Recommended Workflow

### Step 1 - Obtain the v2 and v3 assemblies

Where: local NuGet cache and a local build of the v3 projects

- Fetch the latest CliInvoke 2.x package from NuGet (or pin a specific 2.x version) into a scratch location
- Build the v3 assemblies locally from `src/`

Verify: both assembly paths exist and load

### Step 2 - Write the public-surface extraction

Where: `tools/` (scratch program in `C:\Users\alast\AppData\Local\Temp\opencode` if using System.Reflection.Metadata)

- Extract public types and members with signatures from each assembly
- Normalize the output so the two dumps are comparable (sort, ignore compiler-generated members)

Verify: spot-check the dump against a known delta (for example, `ProcessConfigurationFactory` appears in v2 and not in v3)

### Step 3 - Implement the diff and checklist emitter

Where: `tools/api-diff.ps1`

- Diff the two normalized dumps; classify each delta (removed type, removed member, changed member, added required member)
- Emit a checklist template with one row per delta and an empty guide-entry column for manual mapping

Verify: running the script produces a delta list that includes every known breaking change from the guide's alpha-line rows

### Step 4 - Validate the delta list against the guide draft

Where: `tools/api-diff.ps1`, `site/docs/migration-guides/3.0.0.md`

- Run the script and manually map each delta to an existing or planned guide row
- Record any delta with no guide row - these feed ticket 003's catalog work

Verify: the script's output is repeatable and its rows correspond to identifiable guide entries

## Context pointers

##### Files

- `src/CliInvoke.Core/`, `src/CliInvoke/`, `src/CliInvoke.Specializations/` - source of the v3 assemblies to reflect over
- `site/docs/migration-guides/3.0.0.md` - the guide whose catalog rows the delta list is mapped against
- `IMPLEMENTATION-v3-stable-readiness.md` (Part 5, criterion 1 of Part 0) - the blueprint section defining this tooling

##### ADRs

- None directly relevant

##### Domain terms

- None required beyond the blueprint

##### Ledger records

- `DECISIONS-CliInvoke-v3-stable-readiness.md#T003` - the scripted public-API diff method this ticket implements
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D008` - this tool is stream 1 (diff audit) of the two evidence streams
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D004` - the guide's completeness is audited against the frozen public API surface
- `DECISIONS-CliInvoke-v3-stable-readiness.md#T002` - the output feeds gate criterion 1
- `DECISIONS-CliInvoke-v3-stable-readiness.md#T001` - foundation constraint - no new packages, three shipping projects

## Acceptance criteria

- [ ] `tools/api-diff.ps1` exists and runs against a v2 and a v3 assembly, producing a complete public-API delta list sourced from the binaries, not from prose
- [ ] The emitted checklist has one row per delta with a guide-entry mapping column
- [ ] Every known breaking change from the guide's alpha-line rows appears in the delta list
- [ ] No new NuGet package dependencies were added to the repository
- [ ] The script is repeatable so its output can serve as gate criterion 1 evidence

## Dependencies

**Blocked by** - None - can start immediately
