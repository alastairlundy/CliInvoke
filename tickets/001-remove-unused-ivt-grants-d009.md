---
title: Remove unused InternalsVisibleTo grants (D009 independent set)
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-DECISIONS-CliInvoke-v3-internals-visibility.md
---

## Goal

Remove the five InternalsVisibleTo grants that are already unused, shrinking the cross-package coupling surface without touching any code behavior. This is the D009 pass that drops dead grants regardless of whether the granted assembly is a test project.

## What to build

Edit three project/assembly files to delete the following dead grants:

- `src/CliInvoke/CliInvoke.csproj` — remove `<InternalsVisibleTo Include="CliInvoke.Extensions"/>` (line 61) and the `<AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleToAttribute">` block that targets `CliInvoke.Tests` (lines 58-60).
- `src/CliInvoke/AssemblyInfo.cs` — remove the three `[assembly: InternalsVisibleTo(...)]` lines for `CliInvoke.Specializations`, `CliInvoke.Specializations.Tests`, and `CliInvoke.Tests` (lines 12-14).
- `src/CliInvoke.Specializations/CliInvoke.Specializations.csproj` — remove `<InternalsVisibleTo Include="CliInvoke.Specializations.Tests"/>` (line 38).

The `CliInvoke.Specializations → CliInvoke.Extensions` grant (Specializations.csproj line 37) MUST remain — it is a used grant per T004/T005. No code or public-API change occurs.

## Size

- Files - 3 (all edited)
- Large Files to be created - omitted (no new files)
- Large Edits required - omitted (total lines removed is well under 500)

## Recommended Workflow

### Step 1 - Remove CliInvoke to Extensions and CliInvoke to Tests grants from CliInvoke.csproj

Where: src/CliInvoke/CliInvoke.csproj

- Delete line 61 (`<InternalsVisibleTo Include="CliInvoke.Extensions"/>`).
- Delete the `<AssemblyAttribute ...>` block at lines 58-60 that declares `InternalsVisibleToAttribute` for `CliInvoke.Tests`.

Verify: grep the file for `InternalsVisibleTo` returns no `CliInvoke.Extensions` or `CliInvoke.Tests` entry.

### Step 2 - Remove three grants from AssemblyInfo.cs

Where: src/CliInvoke/AssemblyInfo.cs

- Delete lines 12-14 (`CliInvoke.Specializations`, `CliInvoke.Specializations.Tests`, `CliInvoke.Tests`).
- If the file becomes only a license header plus `using`, that is acceptable; leave the file in place.

Verify: the file contains no `InternalsVisibleTo` attribute.

### Step 3 - Remove Specializations to Specializations.Tests grant

Where: src/CliInvoke.Specializations/CliInvoke.Specializations.csproj

- Delete line 38 (`<InternalsVisibleTo Include="CliInvoke.Specializations.Tests"/>`).
- Confirm line 37 (`CliInvoke.Extensions`) stays.

Verify: the ItemGroup retains only the `CliInvoke.Extensions` grant.

### Step 4 - Build and confirm no downstream breakage

Where: N/A

- Run the inner-loop build for the solution.

Verify: the solution builds; no compile errors from the removed grants (they were unused).

## Context pointers

##### Files

- src/CliInvoke/CliInvoke.csproj — hosts the Extensions grant and the redundant Tests AssemblyAttribute.
- src/CliInvoke/AssemblyInfo.cs — hosts the Specializations / Specializations.Tests / Tests attributes.
- src/CliInvoke.Specializations/CliInvoke.Specializations.csproj — hosts the Specializations.Tests grant (line 38) and the kept Extensions grant (line 37).

##### ADRs

- None constrain this ticket.

##### Domain terms

- InternalsVisibleTo grant (IVT grant) — assembly-scoped grant exposing all internals to a named friend assembly.
- Cross-package coupling point — a dependency surface where one package consumes another's internals via an IVT grant.

##### Ledger records

- DECISIONS-CliInvoke-v3-internals-visibility.md#D009 — remove every unused grant including unused test grants; the 5-grant set is authoritative.
- DECISIONS-CliInvoke-v3-internals-visibility.md#D003 — remove unused grants now; schedule required-grant reduction later.
- DECISIONS-CliInvoke-v3-internals-visibility.md#D004 — used test grants stay; only dead test grants are removed.
- DECISIONS-CliInvoke-v3-internals-visibility.md#T004 — Specializations to Extensions grant is kept (used by PowerShellMiddleware).
- DECISIONS-CliInvoke-v3-internals-visibility.md#T005 — Specializations to Extensions grant is kept (used by CmdMiddleware).

## Acceptance criteria

- [ ] `CliInvoke.csproj` no longer grants `CliInvoke.Extensions` or `CliInvoke.Tests`.
- [ ] `AssemblyInfo.cs` no longer grants `CliInvoke.Specializations`, `CliInvoke.Specializations.Tests`, or `CliInvoke.Tests`.
- [ ] `CliInvoke.Specializations.csproj` no longer grants `CliInvoke.Specializations.Tests` but still grants `CliInvoke.Extensions` (T004/T005).
- [ ] The solution builds cleanly with no new compile errors.
- [ ] No public-API or runtime behavior changed.

## Dependencies

Blocked by - None - can start immediately
