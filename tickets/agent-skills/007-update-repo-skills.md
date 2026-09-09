---
title: Update repo skills for v3
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-v3-stable-readiness.md
---

# Ticket — Update repo skills for v3

## Goal

Bring the three repo-level skills under `.agents/skills/` in line with v3 stable: broaden the pattern-validator's rule text to the glossary's `v2-style code` definition, update the publish-and-package skill for a 3.0.0 stable release, and verify the inner-loop skill's references still hold after the sweep.

## What to build

1. **`cliinvoke-pattern-validator/SKILL.md`** - update the rule text so it flags `v2-style code` per the broadened glossary definition: removed-or-changed API usage AND builder-by-habit where init construction suffices. State the carve-out explicitly - deliberate advanced-builder use (argument escaping, `UserCredentialSpec`/resource-policy callback flows) is legitimate and must not be flagged. Consumer-agnostic wording - the validator cites the glossary term rather than naming consumers.
2. **`cliinvoke-publish-and-package/SKILL.md`** - update for 3.0.0 stable - no prerelease suffix on versions; SourceLink and continuous-integration-build notes unchanged.
3. **`cliinvoke-inner-loop`** - no content change expected; verify its references (paths, projects, test commands) still hold after the sweep and fix anything broken.

## Size

- **Files** - 2 files to edit, plus verification-only work on a third

## Recommended Workflow

### Step 1 - Broaden the pattern-validator rule text

Where: `.agents/skills/cliinvoke-pattern-validator/SKILL.md`

- Rewrite the rule text to the two-part glossary definition - removed-or-changed API usage plus builder-by-habit
- Add the deliberate-advanced-builder carve-out and make the wording consumer-agnostic by citing `GLOSSARY.md`

Verify: the rule text matches `GLOSSARY.md`'s `v2-style code` entry definition-for-definition, carve-out included

### Step 2 - Update publish-and-package for stable

Where: `.agents/skills/cliinvoke-publish-and-package/SKILL.md`

- Remove any prerelease-suffix expectations; state 3.0.0 stable
- Confirm SourceLink and continuous-integration-build guidance is unchanged and still accurate

Verify: no prerelease language remains; CI guidance matches the current workflows

### Step 3 - Verify inner-loop references

Where: `.agents/skills/cliinvoke-inner-loop/`

- Check its referenced paths, project names, and test commands against the post-sweep repo state

Verify: every path and command in the skill resolves; fix any stale reference

## Context pointers

##### Files

- `.agents/skills/cliinvoke-pattern-validator/SKILL.md` - rule-text update
- `.agents/skills/cliinvoke-publish-and-package/SKILL.md` - stable-release update
- `.agents/skills/cliinvoke-inner-loop/` - verification only
- `GLOSSARY.md` - the `v2-style code` definition the validator adopts

##### ADRs

- None directly relevant

##### Domain terms

- `v2-style code` - the normative definition, cited consumer-agnostically

##### Ledger records

- `DECISIONS-CliInvoke-v3-stable-readiness.md#D009` - the broadened glossary definition; all three artifact families adopt the term, consumer-agnostic
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D003` - the deliberate-advanced-builder carve-out the validator must respect
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D001` - goal 2 (agents stop emitting v2 patterns; the validator is the guard half)
- `DECISIONS-CliInvoke-v3-stable-readiness.md#T002` - satisfies gate criterion 9

## Acceptance criteria

- [ ] `cliinvoke-pattern-validator` rule text flags v2-style code per the broadened glossary definition, with the deliberate-advanced-builder carve-out stated (gate criterion 9)
- [ ] The validator's wording is consumer-agnostic and cites the glossary term
- [ ] `cliinvoke-publish-and-package` expects 3.0.0 stable with no prerelease suffix; SourceLink and CI notes unchanged
- [ ] `cliinvoke-inner-loop` references all resolve against the post-sweep repo state

## Dependencies

**Blocked by** - None - can start immediately
