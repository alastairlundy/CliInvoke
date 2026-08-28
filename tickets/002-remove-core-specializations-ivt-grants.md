---
title: Remove Core to Specializations unused IVT grants (T003-dependent)
classification: Independent
blocked_by: ["004-relocate-shellargumentescaper-rework-argumentsspec"]
parent: IMPLEMENTATION-DECISIONS-CliInvoke-v3-internals-visibility.md
---

## Goal

After TK004 relocates `ShellArgumentEscaper` out of CliInvoke.Core, remove the two now-unused `CliInvoke.Core → CliInvoke.Specializations[.Tests]` InternalsVisibleTo grants so Core no longer exposes its internals to Specializations.

## What to build

Edit `src/CliInvoke.Core/CliInvoke.Core.csproj` to delete:

- line 71 `<InternalsVisibleTo Include="CliInvoke.Specializations"/>`
- line 72 `<InternalsVisibleTo Include="CliInvoke.Specializations.Tests"/>`

The `CliInvoke.Core → CliInvoke` grant (line 69) and `CliInvoke.Core → CliInvoke.Tests` grant (line 70) MUST remain — line 69 is kept per T001 (MiddlewareChain/ConditionalMiddleware) and line 70 is kept per I002 (tests consume `MiddlewareChain`).

## Size

- Files - 1 (edited)
- Large Files to be created - omitted
- Large Edits required - omitted

## Recommended Workflow

### Step 1 - Confirm TK004 is complete

Where: N/A

- Verify TK004 has removed `src/CliInvoke.Core/Internal/ShellArgumentEscaper.cs` and Specializations no longer references any CliInvoke.Core internal.

Verify: grep across `src/CliInvoke.Specializations` for `CliInvoke.Core.Internal` returns no matches.

### Step 2 - Remove the two Core to Specializations grants

Where: src/CliInvoke.Core/CliInvoke.Core.csproj

- Delete lines 71-72.
- Keep lines 69 (`CliInvoke`) and 70 (`CliInvoke.Tests`).

Verify: the ItemGroup retains exactly `CliInvoke` and `CliInvoke.Tests`.

### Step 3 - Build Core and Specializations

Where: N/A

- Build the solution.

Verify: CliInvoke.Core and CliInvoke.Specializations both compile; no missing-internal errors.

## Context pointers

##### Files

- src/CliInvoke.Core/CliInvoke.Core.csproj — hosts the four Core grants (lines 69-72).
- src/CliInvoke.Specializations/Middleware/PowerShellMiddleware.cs and CmdMiddleware.cs — must no longer consume Core internals after TK004.

##### ADRs

- None constrain this ticket.

##### Domain terms

- InternalsVisibleTo grant (IVT grant) — assembly-scoped grant exposing all internals to a named friend assembly.
- Cross-package coupling point — a dependency surface where one package consumes another's internals via an IVT grant.

##### Ledger records

- DECISIONS-CliInvoke-v3-internals-visibility.md#D009 — the two Core to Specializations grants become unused after T003 and are removed in this pass.
- DECISIONS-CliInvoke-v3-internals-visibility.md#T001 — Core to CliInvoke grant (line 69) is kept, not removed.
- DECISIONS-CliInvoke-v3-internals-visibility.md#T003 — relocation that makes these grants unused.
- (I002 verifies Core to CliInvoke.Tests is used and stays.)

## Acceptance criteria

- [ ] `CliInvoke.Core.csproj` no longer grants `CliInvoke.Specializations` or `CliInvoke.Specializations.Tests`.
- [ ] `CliInvoke.Core → CliInvoke` (T001) and `CliInvoke.Core → CliInvoke.Tests` (I002) grants remain.
- [ ] CliInvoke.Core and CliInvoke.Specializations compile after removal.

## Dependencies

Blocked by - 004-relocate-shellargumentescaper-rework-argumentsspec
