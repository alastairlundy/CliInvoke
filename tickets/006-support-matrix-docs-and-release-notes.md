---
title: Support-matrix docs and release notes
classification: Independent
blocked_by: [TK005]
parent: IMPLEMENTATION-CliInvoke_v3.2.md
---

## Goal

Make the dual-target shape legible to consumers by updating README/`site/docs` support statements to cover `net10.0 + net11.0` and noting the multi-target bump in release notes — while deliberately keeping every adoption/decline detail out of user-facing docs.

## What to build

On the v3.2 work branch:

- Update the README and `site/docs` support statements (e.g., `site/docs/building-cliinvoke.md` and any support-matrix pages — `Supported-OperatingSystems.md` covers OS support; confirm whether a TFM support statement lives there or elsewhere in `site/docs`) to state that the packages ship `net10.0` and `net11.0` assets on this train.
- Note the multi-target bump in release notes (following the existing release-notes flow; per-package `PackageReleaseNotes` in csprojs).
- Respect the negative scope strictly:
  - **No** "what .NET 11 adds" docs section.
  - **No** per-platform capability table.
  - **No** user-facing adopt/decline list for the Process API work — that record stays ledger-only (`docs/decisions/DECISIONS-CliInvoke-dotnet11.md`).
- CHANGELOG flow rides the existing release process (update when the train advances; no new CHANGELOG structure).

## Size

- **Files** - 3-5 (README, relevant `site/docs` pages, `CHANGELOG.md`, per-package `PackageReleaseNotes` in csprojs)

## Recommended Workflow

### Step 1 - Locate the support statements

Where: README.md, site/docs

- Find every statement of supported target frameworks or .NET versions in the README and `site/docs`.

Verify: a complete location list of support statements exists.

### Step 2 - Update statement content

Where: README.md, site/docs pages found in Step 1

- Amend each support statement to cover `net10.0 + net11.0` for the v3.2 train.
- Do not add adoption/decline sections or capability tables.

Verify: prose review confirms no out-of-scope sections were added.

### Step 3 - Update release notes and changelog

Where: CHANGELOG.md, PackageReleaseNotes in the three src csprojs

- Note the multi-target bump in release notes.
- Follow the existing release process for CHANGELOG updates (Keep-a-Changelog flow with release notes per release).

Verify: release notes mention the dual-target bump; CHANGELOG entry follows the existing format.

## Context pointers

##### Files

- `README.md` — top-level support position
- `site/docs/building-cliinvoke.md`, `site/docs/Supported-OperatingSystems.md`, and `site/docs` pages carrying TFM statements
- `CHANGELOG.md` and the three csprojs' `PackageReleaseNotes`

##### ADRs

- None directly constrain this ticket.

##### Domain terms

- None beyond general glossary usage — this ticket is documentation only.

##### Ledger records

- `DECISIONS-CliInvoke-dotnet11.md#T004` — docs scope limited to support statements + release notes; no "what .NET 11 adds" section, no capability table, no user-facing adopt/decline list; ledger-only adoption record
- `DECISIONS-CliInvoke-dotnet11.md#D004` — the v3.2 train identity these docs describe

## Acceptance criteria

- [ ] README and `site/docs` support statements cover `net10.0 + net11.0` [`DECISIONS-CliInvoke-dotnet11.md#T004`]
- [ ] Release notes note the multi-target bump [`DECISIONS-CliInvoke-dotnet11.md#T004`]
- [ ] No "what .NET 11 adds" docs section and no per-platform capability table exist [`DECISIONS-CliInvoke-dotnet11.md#T004`]
- [ ] No user-facing adopt/decline list; the `D008`/`T003` details remain documented only in the ledger [`DECISIONS-CliInvoke-dotnet11.md#T004`]
- [ ] CHANGELOG updates ride the existing release process with no new structure [`DECISIONS-CliInvoke-dotnet11.md#T004`]

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** - TK005 (docs must describe the finalized adoption state of the train, which TK005's adapter swaps settle; published as `005-adapter-layer-process-api-swaps`)
