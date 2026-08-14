---
title: Migrate in-repo and site docs references
classification: Independent
blocked_by: ["001-create-argumentsspec.md", "002-create-environmentvariablesspec.md", "003-create-processresourcepolicyspec.md", "004-create-usercredentialspec.md", "005-change-iprocessconfigurationbuilder-signatures.md", "006-rewire-processconfigurationbuilder.md", "007-delete-sub-builder-interfaces-and-classes.md", "008-rewrite-configurationextensions.md"]
parent: docs/decisions/DECISIONS-CliInvoke-configuration-seam-stack.md
---

## Goal

Update every in-repo and site documentation reference to the removed sub-builder interfaces so no sub-builder references remain after the hard break.

## What to build

MODIFY the following files (per `D009`, `D004`):
- `README.md` - update `UserCredentialBuilder` reference (line 228) to `UserCredentialSpec`.
- `src/CliInvoke.Core/README.md` - update the builder enumeration (lines 22-27) to list the four specs instead of the four interfaces.
- `site/docs/guides/configuration.md` - update the interface to type mapping table (lines 302-305), the `ConfigureXxx` examples (lines 314-315), and the working-set / credential notes (lines 148-167, 481-482).
- `site/docs/guides/architecture.md` - update the builder descriptions (lines 42-43, 124-128) to reference the specs.
- `site/docs/guides/troubleshooting.md` - update `UserCredentialBuilder` references (lines 29, 55) to `UserCredentialSpec`.
- `site/docs/guides/resource-disposal.md` - update the disposal table (line 42) to reference `UserCredentialSpec` and its `IDisposable` lifecycle.
- `AGENTS.md` - update the resource-disposal note (line 28) to reference `UserCredentialSpec`.

This is the in-place migration half of `D009`; the additive migration guide and CHANGELOG entry are handled by TK011.

## Size

- **Files** - 7 (modify 7 documentation files)

## Recommended Workflow

### Step 1 — Update root and Core README references

Where: `README.md`, `src/CliInvoke.Core/README.md`

- Replace `UserCredentialBuilder` (README line 228) with `UserCredentialSpec`.
- Replace the four-interface enumeration (Core README lines 22-27) with the four specs.

Verify: No `UserCredentialBuilder` or interface name remains in either file.

### Step 2 — Update site guide references

Where: `site/docs/guides/configuration.md`, `architecture.md`, `troubleshooting.md`, `resource-disposal.md`

- Update the mapping table, `ConfigureXxx` examples, and working-set/credential notes in `configuration.md`.
- Update builder descriptions in `architecture.md`.
- Update `UserCredentialBuilder` to `UserCredentialSpec` in `troubleshooting.md`.
- Update the disposal table in `resource-disposal.md` to reference `UserCredentialSpec` and its `IDisposable` lifecycle.

Verify: No sub-builder interface or class names remain in the site guides.

### Step 3 — Update AGENTS.md

Where: `AGENTS.md`

- Update the resource-disposal note (line 28) to reference `UserCredentialSpec`.

Verify: `AGENTS.md` references `UserCredentialSpec` for credential disposal.

## Context pointers

**Files** - `README.md`, `src/CliInvoke.Core/README.md`, `site/docs/guides/configuration.md`, `architecture.md`, `troubleshooting.md`, `resource-disposal.md`, `AGENTS.md` (all modify)
**ADRs** - None
**Domain terms** - config-seam collapse (in-place migration so the new pattern is the only visible surface)
**Ledger records** - `DECISIONS-CliInvoke-configuration-seam-stack.md#D009` (migration impact - in-place update), `#D004` (credential disposal lifecycle)

## Acceptance criteria

- [ ] `README.md` and `src/CliInvoke.Core/README.md` reference the four specs, not the interfaces.
- [ ] `site/docs/guides/configuration.md`, `architecture.md`, `troubleshooting.md`, `resource-disposal.md` contain no sub-builder interface/class references.
- [ ] `resource-disposal.md` documents `UserCredentialSpec` and its `IDisposable` lifecycle.
- [ ] `AGENTS.md` references `UserCredentialSpec` for credential disposal.

## Dependencies

**Blocked by** - `001-create-argumentsspec.md`, `002-create-environmentvariablesspec.md`, `003-create-processresourcepolicyspec.md`, `004-create-usercredentialspec.md`, `005-change-iprocessconfigurationbuilder-signatures.md`, `006-rewire-processconfigurationbuilder.md`, `007-delete-sub-builder-interfaces-and-classes.md`, `008-rewrite-configurationextensions.md`
