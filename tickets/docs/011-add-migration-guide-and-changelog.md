---
title: Add migration guide and CHANGELOG entry
classification: Independent
blocked_by: ["001-create-argumentsspec.md", "002-create-environmentvariablesspec.md", "003-create-processresourcepolicyspec.md", "004-create-usercredentialspec.md", "005-change-iprocessconfigurationbuilder-signatures.md", "006-rewire-processconfigurationbuilder.md", "007-delete-sub-builder-interfaces-and-classes.md", "008-rewrite-configurationextensions.md", "009-replace-sub-builder-tests.md", "010-migrate-docs-references.md"]
parent: docs/decisions/DECISIONS-CliInvoke-configuration-seam-stack.md
---

## Goal

Add the additive migration documentation for the config-seam hard break - a new "Migrating from sub-builder interfaces" guide and a CHANGELOG entry documenting the old-vs-new pattern.

## What to build

CREATE `site/docs/guides/migrating-from-sub-builder-interfaces.md` (per `D009`): a migration guide documenting the old-vs-new pattern for all four concepts (arguments, environment variables, process resource policy, user credential) - how callers change the variable type from `I*Builder` to `XxxSpec` and adapt to the streamlined spec APIs.

MODIFY `CHANGELOG.md` (per `D009`): add an entry referencing the migration guide and the hard break (the config-seam changes land as part of the upcoming v3.0.0 per `D011`).

This is the additive documentation half of `D009`; the in-place reference migration is handled by TK010.

## Size

- **Files** - 2 (create 1 guide; modify 1 CHANGELOG)

## Recommended Workflow

### Step 1 — Write the migration guide

Where: `site/docs/guides/migrating-from-sub-builder-interfaces.md`

- Document the old-vs-new pattern for each of the four concepts, showing the `I*Builder` to `XxxSpec` type change and the streamlined API differences (e.g., `EnvironmentVariablesSpec.SetEnumerable`, `ProcessResourcePolicySpec.SetWorkingSet`, `UserCredentialSpec.SetUserProfileLoading`).
- Link back to the configuration guide and the CHANGELOG entry.

Verify: Guide covers all four concepts with before/after examples.

### Step 2 — Add the CHANGELOG entry

Where: `CHANGELOG.md`

- Add an entry for the v3.0.0 config-seam hard break referencing the migration guide and noting the four sub-builder interfaces were removed.

Verify: CHANGELOG entry present and references the migration guide.

## Context pointers

**Files** - `site/docs/guides/migrating-from-sub-builder-interfaces.md` (create); `CHANGELOG.md` (modify); specs from TK001-TK004; interface/rewire from TK005-TK006
**ADRs** - None
**Domain terms** - config-seam collapse (hard break, no deprecation window); v3.0.0 (the release the changes land in, per `D011`)
**Ledger records** - `DECISIONS-CliInvoke-configuration-seam-stack.md#D009` (migration impact - additive guide + CHANGELOG), `#D011` (versioning - lands in v3.0.0)

## Acceptance criteria

- [ ] `site/docs/guides/migrating-from-sub-builder-interfaces.md` documents the old-vs-new pattern for all four concepts.
- [ ] `CHANGELOG.md` has an entry referencing the migration guide and the hard break.
- [ ] The CHANGELOG entry notes the changes land in v3.0.0 per `D011`.

## Dependencies

**Blocked by** - `001-create-argumentsspec.md`, `002-create-environmentvariablesspec.md`, `003-create-processresourcepolicyspec.md`, `004-create-usercredentialspec.md`, `005-change-iprocessconfigurationbuilder-signatures.md`, `006-rewire-processconfigurationbuilder.md`, `007-delete-sub-builder-interfaces-and-classes.md`, `008-rewrite-configurationextensions.md`, `009-replace-sub-builder-tests.md`, `010-migrate-docs-references.md`
