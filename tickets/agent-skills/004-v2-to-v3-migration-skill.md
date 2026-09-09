---
title: New v2-to-v3 migration skill
classification: Independent
blocked_by: [003-migration-guide-catalog-and-walkthroughs]
parent: IMPLEMENTATION-v3-stable-readiness.md
---

# Ticket — New v2-to-v3 migration skill

## Goal

Create `skills/cliinvoke/v2-to-v3-migration/` - a self-contained skill with a SKILL.md plus one reference per walkthrough unit, mirroring the migration guide's structure so the two artifacts stay auditable against each other. This is the agent-facing half of the v2-modernization goal.

## What to build

Create the new skill pack:

1. `skills/cliinvoke/v2-to-v3-migration/SKILL.md` - trigger conditions for detecting v2-style code, the `GLOSSARY.md` `v2-style code` definition citation, and the three-canonical-pattern partition.
2. `references/CliRun.md` - static-call replacements (facade changes, config passed as the object).
3. `references/IProcessInvoker.md` - invoker and factory replacements (factory removal fallout, dependency-injection `configure` bindings).
4. `references/IExternalProcess.md` - lifecycle and constructor replacements (constructor C only, sealed, `ExitConfiguration` at construction).
5. `references/Construction.md` - the init-first and builder-advanced mapping, including a builder-methods-to-init-properties table and the keep-builder cases (argument escaping, `UserCredentialSpec`, resource-policy callback flows).

Each reference maps v2-style code to v3 replacements and mirrors the corresponding guide walkthrough for mutual auditability. Detection logic stays in-skill rather than leaning on `cliinvoke-pattern-validator`. Some mapping content is intentionally duplicated between the skill and the guide - that duplication is an accepted cost, not a defect to refactor away.

## Size

- **Files** - 5 files to create
- **Large Edits required** - five new reference-and-skill files mirroring the guide's walkthroughs are expected to total more than 500 lines

## Recommended Workflow

### Step 1 - Read the completed guide walkthroughs

Where: `site/docs/migration-guides/3.0.0.md`

- Read the three pattern walkthroughs and the shared construction section written in ticket 003

Verify: each walkthrough unit is identified and its structure understood

### Step 2 - Write the SKILL.md

Where: `skills/cliinvoke/v2-to-v3-migration/SKILL.md`

- Write trigger conditions (how an agent recognizes v2-style code), cite the `GLOSSARY.md` `v2-style code` definition, and state the three-pattern partition
- Point each pattern at its reference file

Verify: the SKILL.md follows the repo's existing skill-pack conventions (compare with `skills/cliinvoke/select-execution-pattern/SKILL.md`)

### Step 3 - Write the three pattern references

Where: `references/CliRun.md`, `references/IProcessInvoker.md`, `references/IExternalProcess.md`

- Mirror each guide walkthrough one-to-one; map v2-style code to v3 replacements with Before/After examples

Verify: every guide walkthrough has exactly one corresponding reference with matching scope

### Step 4 - Write the construction reference

Where: `references/Construction.md`

- Map builder methods to init properties in a table
- List the keep-builder cases - argument escaping, `UserCredentialSpec`, resource-policy callback flows
- State init construction as the default and builder as advanced

Verify: the table covers every builder method in the frozen v3 API

### Step 5 - Cross-audit skill and guide

Where: the new skill directory and the guide

- Walk both artifact structures side by side; confirm each guide unit has a skill counterpart and vice versa

Verify: a unit present in one artifact but not the other is either justified or fixed

## Context pointers

##### Files

- `site/docs/migration-guides/3.0.0.md` - the guide whose structure this skill mirrors (must be complete first)
- `GLOSSARY.md` - the `v2-style code` definition to cite
- `skills/cliinvoke/select-execution-pattern/SKILL.md` - the repo's skill-pack conventions to follow
- `skills/cliinvoke/cliinvoke-v1-to-v2-migration/` - the superseded skill this one replaces (deleted in ticket 005)

##### ADRs

- None directly relevant

##### Domain terms

- `v2-style code` - the normative classification term, cited from `GLOSSARY.md`

##### Ledger records

- `DECISIONS-CliInvoke-v3-stable-readiness.md#T004` - one self-contained skill at `skills/cliinvoke/v2-to-v3-migration/` with one reference per usage pattern
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D005` - the v3-first rewrite plus migration-skill target state
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D009` - the glossary definition all three artifact families adopt
- `DECISIONS-CliInvoke-v3-stable-readiness.md#T007` - the three-canonical-pattern partition the references follow
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D007` - this skill supersedes the deleted v1-to-v2 skill

## Acceptance criteria

- [ ] `skills/cliinvoke/v2-to-v3-migration/` exists with a SKILL.md and exactly four references (CliRun, IProcessInvoker, IExternalProcess, Construction)
- [ ] Each reference mirrors one guide walkthrough unit; the structures are auditable against each other
- [ ] The SKILL.md cites the `GLOSSARY.md` `v2-style code` definition
- [ ] The construction reference includes a builder-methods-to-init-properties table and names the keep-builder cases
- [ ] Detection logic lives in-skill, not in `cliinvoke-pattern-validator`

## Dependencies

**Blocked by** - [003-migration-guide-catalog-and-walkthroughs](../migration-guide/003-migration-guide-catalog-and-walkthroughs.md) - the skill references mirror the guide's walkthrough structure for mutual auditability, so the guide must be complete first
