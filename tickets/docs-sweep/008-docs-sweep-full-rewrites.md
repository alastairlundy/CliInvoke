---
title: Docs sweep - full rewrites
classification: Independent
blocked_by: [003-migration-guide-catalog-and-walkthroughs]
parent: IMPLEMENTATION-v3-stable-readiness.md
---

# Ticket — Docs sweep full rewrites

## Goal

Give the high-traffic documentation pages coherent v3 stories through full rewrites: init construction throughout, the construction story as the centerpiece, and the three-pattern canon with v3 examples. These are the pages users actually land on, so partial refreshes are not enough for them.

## What to build

Full rewrites of five pages:

1. `site/docs/getting-started.md`
2. `site/docs/getting-started-quickstart.md`
3. `site/docs/getting-started-readme.md`
4. `site/docs/guides/configuration.md` - the construction story as the centerpiece: init construction the default, builder advanced (escaping, `UserCredentialSpec`/resource-policy callbacks), dependency injection unchanged
5. `site/docs/guides/choosing-invocation-pattern.md` - the three-pattern canon with v3 examples, cross-linked to the migration guide

Rewrite rules:

- Init construction throughout; the builder is absent or explicitly advanced-labeled
- Terminology stays consistent with the refreshed pages (ticket 009) - shared vocabulary comes from `GLOSSARY.md`, in particular the `v2-style code` entry
- Cross-link the migration guide where upgrade context helps
- Removed concepts (for example, `ProcessConfigurationFactory`) are stated as removed, not silently dropped

## Size

- **Files** - 5 files to rewrite
- **Large Edits required** - full rewrites of five high-traffic pages are expected to total more than 500 changed lines

## Recommended Workflow

### Step 1 - Inventory v2-era content in the five pages

Where: the five pages listed above

- Grep for `ProcessConfigurationFactory`, `ArgumentsList`, `Piped*Async`, and builder-default framing
- Note which sections each page needs

Verify: a complete inventory exists before writing

### Step 2 - Rewrite the three getting-started pages

Where: `site/docs/getting-started.md`, `getting-started-quickstart.md`, `getting-started-readme.md`

- Rewrite with init construction throughout; builder absent or advanced-labeled
- Keep page structure recognizable so inbound links and menu entries still make sense

Verify: no v2 construction snippet survives in the three pages

### Step 3 - Rewrite guides/configuration.md

Where: `site/docs/guides/configuration.md`

- Make the construction story the centerpiece - init default, builder advanced, dependency injection unchanged
- Cover `TargetFilePath` required, the `ArgumentsList` merge, and working-directory validation at construction

Verify: the page's construction claims match the frozen v3 API

### Step 4 - Rewrite guides/choosing-invocation-pattern.md

Where: `site/docs/guides/choosing-invocation-pattern.md`

- Present the three-pattern canon with v3 examples
- Cross-link the migration guide for v2 users

Verify: the three patterns and their boundaries match `GLOSSARY.md` and the guide

### Step 5 - Consistency pass against the refreshed pages

Where: all five rewritten pages

- Check terminology against `GLOSSARY.md` and spot-check that cross-links resolve

Verify: links resolve; no drift from the shared vocabulary

## Context pointers

##### Files

- `site/docs/getting-started.md`, `site/docs/getting-started-quickstart.md`, `site/docs/getting-started-readme.md` - rewrites
- `site/docs/guides/configuration.md`, `site/docs/guides/choosing-invocation-pattern.md` - rewrites
- `site/docs/migration-guides/3.0.0.md` - the completed guide to cross-link (must exist first)
- `GLOSSARY.md` - shared vocabulary source
- `src/CliInvoke.Core/` - the frozen v3 API the pages must describe accurately

##### ADRs

- `docs/adr/0003-cli-run-defaults-facade.md` - constrains the CliRun facade description

##### Domain terms

- `v2-style code` - the classification term, cited from `GLOSSARY.md`

##### Ledger records

- `DECISIONS-CliInvoke-v3-stable-readiness.md#T005` - full rewrites for high-traffic pages, targeted refresh for the rest (the rest is ticket 009)
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D002` - the locked GA construction story the configuration page centers on
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D003` - builder positioned advanced
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D001` - the all-public-docs-updated-to-v3 constraint

## Acceptance criteria

- [ ] All five pages are fully rewritten with init construction throughout
- [ ] `guides/configuration.md` presents the construction story as its centerpiece with the builder advanced-labeled
- [ ] `guides/choosing-invocation-pattern.md` presents the three-pattern canon with v3 examples and cross-links the migration guide
- [ ] No v2-era construction snippet remains in the five pages
- [ ] Terminology is consistent with `GLOSSARY.md` and cross-links resolve

## Dependencies

**Blocked by** - [003-migration-guide-catalog-and-walkthroughs](../migration-guide/003-migration-guide-catalog-and-walkthroughs.md) - the rewritten pages cross-link the migration guide, so the guide must exist in its final shape first
