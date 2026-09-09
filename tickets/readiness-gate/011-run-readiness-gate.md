---
title: Run the readiness gate
classification: Independent
blocked_by: [001-api-diff-tool, 002-simulation-samples, 003-migration-guide-catalog-and-walkthroughs, 004-v2-to-v3-migration-skill, 005-retire-v1-to-v2-skill-and-refresh-evals, 006-rewrite-skill-packs-v3-first, 007-update-repo-skills, 008-docs-sweep-full-rewrites, 009-docs-sweep-targeted-refresh, 010-release-mechanics]
parent: IMPLEMENTATION-v3-stable-readiness.md
---

# Ticket — Run the readiness gate

## Goal

Execute the 12-criterion flat blocking gate from the blueprint's Part 0. The 3.0.0 stable release blocks until every criterion is green; any red criterion spawns a follow-up fix and a gate re-run rather than an advisory pass.

## What to build

No files are produced by this ticket - it is a verification pass that runs the twelve criteria with their specified check mechanisms:

1. **Scripted API-diff audit green** - run `tools/api-diff.ps1`; every v2-to-v3 public-API delta maps to at least one guide entry in the TL;DR table.
2. **Agent simulation green** - upgrade all three pattern samples from ticket 002 using only the guide; each compiles and behaves correctly (output matches the recorded baseline).
3. **Guide walkthrough coverage** - manual check against the guide table of contents: three canonical patterns plus the shared construction section.
4. **Guide examples compile** - compile every code example in the guide against v3 stable (script or scratch-project pass).
5. **Alpha-line language removed** - grep `site/docs/migration-guides/3.0.0.md` for "begins as 3.0.0-alpha".
6. **Removed-API identifier sweep** - grep `site/docs/` and `skills/` for the removed identifiers, with the labeled-legacy allowlist.
7. **Skill construction framing** - grep plus manual review: init construction as default everywhere; builder only advanced.
8. **Skill pack existence** - `cliinvoke-v1-to-v2-migration` deleted; `v2-to-v3-migration` present.
9. **Pattern-validator rule text** - manual review against the broadened glossary definition.
10. **Docs sweep green** - grep plus manual review of `site/docs/*` and `README.md`, legacy guide exempt.
11. **Release state** - grep csproj files and the CHANGELOG header: `3.0.0`, no prerelease suffix, finalized stable entry.
12. **Glossary term citations** - grep for the `v2-style code` term cited from the guide, the migration skill, and the pattern-validator.

Record the per-criterion result (green/red with evidence) in the ticket or a gate-run note. A red criterion spawns a follow-up; the gate re-runs until all twelve are green.

## Recommended Workflow

### Step 1 - Run the mechanical criteria

Where: `tools/api-diff.ps1`, `site/docs/`, `skills/`

- Run criteria 1, 5, 6, 8, 11, and 12 with their grep and script mechanisms

Verify: each has a recorded green result with the command output as evidence

### Step 2 - Run the simulation

Where: `simulation/v2-samples/`, using only `site/docs/migration-guides/3.0.0.md`

- Upgrade each of the three samples using only the guide; build and run each; compare output to the baseline from ticket 002

Verify: all three compile and behave correctly (criterion 2)

### Step 3 - Run the manual-review criteria

Where: the guide, the skill packs, and `.agents/skills/cliinvoke-pattern-validator/SKILL.md`

- Review criteria 3, 4, 7, 9, and 10 - table-of-contents check, example compilation, framing review, rule-text review, docs review

Verify: each has a recorded green result

### Step 4 - Consolidate the gate result

Where: a gate-run note (ticket comment or scratch file)

- Record all twelve results; if any are red, spawn follow-ups and re-run after fixes

Verify: all twelve criteria green, or an explicit follow-up list exists for the red ones

## Context pointers

##### Files

- `IMPLEMENTATION-v3-stable-readiness.md` (Part 0) - the authoritative criterion list and check mechanisms
- `tools/api-diff.ps1` - criterion 1 evidence source
- `simulation/v2-samples/` - criterion 2 inputs
- `site/docs/migration-guides/3.0.0.md` - criteria 3, 4, 5, 12
- `GLOSSARY.md` - the `v2-style code` definition cited by criteria 9 and 12

##### ADRs

- None directly relevant

##### Domain terms

- `v2-style code` - cited by criteria 9 and 12

##### Ledger records

- `DECISIONS-CliInvoke-v3-stable-readiness.md#T002` - the one flat blocking gate with the finalized 12-criterion pool this ticket executes
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D006` - criteria gate, then release sweep - readiness is proven, not assumed
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D008` - both evidence streams must pass
- `DECISIONS-CliInvoke-v3-stable-readiness.md#T003` - criterion 1's mechanical method
- `DECISIONS-CliInvoke-v3-stable-readiness.md#T006` - criterion 2's per-pattern attribution
- `DECISIONS-CliInvoke-v3-stable-readiness.md#T007` - criterion 3's three-pattern-plus-construction wording

## Acceptance criteria

- [ ] All 12 criteria have a recorded result with evidence (command output, compilation log, or review note)
- [ ] Every criterion is green, or each red criterion has a spawned follow-up and the gate re-runs after fixes
- [ ] The gate result is recorded somewhere durable enough to justify the release

## Dependencies

**Blocked by** - all ten prior tickets - the gate runs only when every part of the blueprint's execution order is complete
