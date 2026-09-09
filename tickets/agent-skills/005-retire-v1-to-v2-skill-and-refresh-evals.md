---
title: Retire v1-to-v2 skill and refresh evals
classification: Independent
blocked_by: [004-v2-to-v3-migration-skill]
parent: IMPLEMENTATION-v3-stable-readiness.md
---

# Ticket — Retire v1-to-v2 skill and refresh evals

## Goal

Delete `skills/cliinvoke/cliinvoke-v1-to-v2-migration/` wholesale - it teaches migration to a non-current major and is superseded by the new v2-to-v3 skill - and refresh `skills/evals/` so no eval references the deleted skill and the new migration skill has eval coverage.

## What to build

1. **Delete** `skills/cliinvoke/cliinvoke-v1-to-v2-migration/` in its entirety (SKILL.md plus any references).
2. **Update or remove** every eval under `skills/evals/` that references the deleted skill - remove evals whose subject is gone, rewrite evals that merely name it incidentally.
3. **Add eval coverage** for `skills/cliinvoke/v2-to-v3-migration/` so the new migration skill's trigger and reference behavior is exercised.

The site's v1-to-v2 migration guide (under `site/docs/migration-guides/`) is NOT touched by this ticket - that guide stays, labeled for legacy users, per the legacy-artifact decision.

## Size

- **Files** - roughly 4 to 8 files (1 directory deleted, plus eval files removed or added)

## Recommended Workflow

### Step 1 - Confirm the replacement skill exists

Where: `skills/cliinvoke/v2-to-v3-migration/`

- Verify the new migration skill from ticket 004 is present and complete

Verify: the superseding skill is in place before deleting its predecessor

### Step 2 - Delete the v1-to-v2 skill directory

Where: `skills/cliinvoke/cliinvoke-v1-to-v2-migration/`

- Remove the directory and all contents

Verify: `git status` shows the deletion; no dangling references remain elsewhere in `skills/`

### Step 3 - Sweep evals for references to the deleted skill

Where: `skills/evals/`

- Find every eval referencing the deleted skill; remove or rewrite each

Verify: a search for the deleted skill's name across `skills/evals/` returns no hits

### Step 4 - Add eval coverage for the migration skill

Where: `skills/evals/`

- Add eval task(s) exercising the new migration skill - trigger recognition on a v2-style sample and correct reference selection

Verify: the new eval is consistent with the existing eval format in `skills/evals/`

## Context pointers

##### Files

- `skills/cliinvoke/cliinvoke-v1-to-v2-migration/` - the directory to delete
- `skills/cliinvoke/v2-to-v3-migration/` - the superseding skill (must exist first)
- `skills/evals/` - the eval suite to refresh

##### ADRs

- None directly relevant

##### Domain terms

- `v2-style code` - used in the new eval's task framing

##### Ledger records

- `DECISIONS-CliInvoke-v3-stable-readiness.md#D007` - retire the skill, keep the guide - the skill is deleted because it is superseded, and the site guide is explicitly out of this ticket's scope
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D005` - the new migration skill this deletion makes room for
- `DECISIONS-CliInvoke-v3-stable-readiness.md#T002` - deletion satisfies gate criterion 8

## Acceptance criteria

- [ ] `skills/cliinvoke/cliinvoke-v1-to-v2-migration/` no longer exists (gate criterion 8)
- [ ] No eval under `skills/evals/` references the deleted skill
- [ ] The v2-to-v3 migration skill has at least one eval exercising it
- [ ] The site's v1-to-v2 migration guide is untouched and still labeled for legacy users

## Dependencies

**Blocked by** - [004-v2-to-v3-migration-skill](../agent-skills/004-v2-to-v3-migration-skill.md) - the skill is deleted only once superseded by the new migration skill, and its replacement evals target that skill
