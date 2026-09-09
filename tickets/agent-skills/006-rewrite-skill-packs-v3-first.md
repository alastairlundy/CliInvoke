---
title: Rewrite user-facing skill packs to v3-first
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-v3-stable-readiness.md
---

# Ticket — Rewrite user-facing skill packs to v3-first

## Goal

Rewrite the existing user-facing skill packs (`skills/cliinvoke`, `skills/cliinvoke-core`) so they teach v3 syntax - init construction as the default, `ProcessConfigurationBuilder` as advanced - and no longer emit v2 construction patterns.

## What to build

Rewrites, file by file:

1. **`skills/cliinvoke/select-execution-pattern/SKILL.md`** and its references `references/CliRun.md`, `references/IProcessInvoker.md`, `references/IExternalProcessCreation.md` - replace v2 construction snippets with init construction; note middleware-asymmetric coverage (middleware applies to `ProcessInvoker`/`IProcessInvoker` only; `IExternalProcess` is the bypass pattern).
2. **`skills/cliinvoke-core/generate-process-configuration/references/ConfiguringWithBuilders.md`** and `references/SettingValues.md` - rewrite init-first; builder only as advanced; cover the `ArgumentsList` merge into init-only `ArgumentList` and the `required` `TargetFilePath`.
3. **`skills/cliinvoke-core/implement-resource-lifecycle/references/IExternalProcess.md`** and `references/ProcessConfiguration.md` - v3 constructor seams, the no-mutation contract, and disposal guidance consistent with the README.
4. **`skills/cliinvoke/package-installation-choice`** - refresh version guidance to 3.0.0 stable with no prerelease.

Every rewritten file positions init construction as the default and the builder as advanced - the builder remains legitimate for argument escaping and `UserCredentialSpec`/resource-policy callback flows. Every reference file is re-reviewed against the frozen v3 public surface.

## Size

- **Files** - roughly 8 to 10 files to edit
- **Large Edits required** - the construction snippets across the pack are expected to total more than 500 changed lines

## Recommended Workflow

### Step 1 - Verify the construction API against source

Where: `src/CliInvoke.Core/`

- Confirm the init property names, the `required` `TargetFilePath`, the merged `ArgumentList`, and the builder's retained advanced methods

Verify: every snippet you are about to write compiles against v3

### Step 2 - Rewrite select-execution-pattern

Where: `skills/cliinvoke/select-execution-pattern/SKILL.md` plus `references/CliRun.md`, `references/IProcessInvoker.md`, `references/IExternalProcessCreation.md`

- Replace v2 construction snippets with init construction
- State the middleware-coverage asymmetry per pattern

Verify: no v2 construction pattern remains in the four files

### Step 3 - Rewrite generate-process-configuration references

Where: `skills/cliinvoke-core/generate-process-configuration/references/ConfiguringWithBuilders.md`, `references/SettingValues.md`

- Rewrite init-first; reposition the builder as the advanced path
- Cover the `ArgumentsList` merge and the `required` `TargetFilePath`

Verify: builder content appears only under an advanced framing

### Step 4 - Rewrite implement-resource-lifecycle references

Where: `skills/cliinvoke-core/implement-resource-lifecycle/references/IExternalProcess.md`, `references/ProcessConfiguration.md`

- Document the v3 constructor seams, the no-mutation contract, and disposal guidance per the README

Verify: disposal guidance matches the README's Resource Cleanup section

### Step 5 - Refresh package-installation-choice and final sweep

Where: `skills/cliinvoke/package-installation-choice`

- Update version guidance to 3.0.0 stable with no prerelease language
- Grep the whole pack for `ProcessConfigurationFactory`, `ArgumentsList`, and builder-by-default framing

Verify: the grep sweep is clean (advanced-builder framing aside)

## Context pointers

##### Files

- `src/CliInvoke.Core/` - the frozen v3 API all snippets must match
- `skills/cliinvoke/select-execution-pattern/` - pack 1 of the rewrites
- `skills/cliinvoke-core/generate-process-configuration/` and `skills/cliinvoke-core/implement-resource-lifecycle/` - pack 2 of the rewrites
- `skills/cliinvoke/package-installation-choice/` - version-guidance refresh
- `README.md` - the resource-disposal guidance the lifecycle references must match

##### ADRs

- None directly relevant

##### Domain terms

- `v2-style code` - the definition this rewrite makes obsolete in pack content; builder-by-habit is v2-style, deliberate advanced-builder use is not

##### Ledger records

- `DECISIONS-CliInvoke-v3-stable-readiness.md#D005` - the v3-first rewrite target state this ticket implements
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D002` - the locked GA construction story (init default, factory removed)
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D003` - builder positioned advanced; kept for escaping and callback flows
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D001` - goal 2 (agents stop emitting v2 patterns at the source)
- `DECISIONS-CliInvoke-v3-stable-readiness.md#T002` - satisfies gate criterion 7

## Acceptance criteria

- [ ] Every SKILL.md and reference in the rewritten packs teaches init construction as the default
- [ ] `ProcessConfigurationBuilder` appears only as advanced, with the escaping and callback-flow carve-outs stated (gate criterion 7)
- [ ] The `ArgumentsList` merge and the `required` `TargetFilePath` are covered in the generate-process-configuration references
- [ ] Middleware-asymmetric coverage is stated per pattern in select-execution-pattern
- [ ] No v2 construction pattern remains anywhere in the four packs (grep clean outside advanced framing)
- [ ] All snippets compile against v3 stable

## Dependencies

**Blocked by** - None - can start immediately (vocabulary comes from `GLOSSARY.md` and the frozen API, not from the guide)
