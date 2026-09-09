---
title: Docs sweep - targeted refresh
classification: Independent
blocked_by: [003-migration-guide-catalog-and-walkthroughs]
parent: IMPLEMENTATION-v3-stable-readiness.md
---

# Ticket — Docs sweep targeted refresh

## Goal

Purge v2-era content from every remaining public-facing document - keep each page's structure, update its content to v3 stable, and label the legacy v1-to-v2 guide as the explicit exception. No pruning: files about removed concepts are refreshed to state their removal.

## What to build

Targeted refresh (keep structure, purge v2-era content) for:

1. `site/docs/guides/architecture.md`, `site/docs/guides/middleware.md`, `site/docs/guides/resource-disposal.md`, `site/docs/guides/troubleshooting.md`
2. `site/docs/guides/migrating-from-sub-builder-interfaces.md` - refresh to state v3's final construction shape; verify the sub-builder seam's v3 status against source before writing
3. `site/docs/comparison.md`, `site/docs/everything-wrong-with-process-in-csharp.md`, `site/docs/building-cliinvoke.md`, `site/docs/Supported-OperatingSystems.md`
4. `site/docs/readme.md`, `site/docs/guides/readme.md`, `site/docs/migration-readme.md`, `site/docs/migration-guides/readme.md`
5. `site/docs/guides/menu.yml`, `site/docs/menu.yml`, `site/docs/migration-guides/menu.yml` - labels, links, and v2-era framing
6. `README.md` (repo root) - v3 usage, resource-disposal guidance, version badges

Legacy exception (explicitly excluded from the sweep's removal criteria):

- `site/docs/migration-guides/migrating-v1-to-v2.md` plus `site/docs/migration-guides/v1-to-v2/` - kept, clearly labeled "legacy - for users still on v1", and excluded from the removed-API-identifier grep sweeps via the allowlist

## Size

- **Files** - roughly 14 to 16 files to edit
- **Large Edits required** - the broad sweep across the doc tree is expected to total more than 500 changed lines

## Recommended Workflow

### Step 1 - Sweep for v2-era identifiers

Where: `site/docs/**`, `README.md`

- Grep for `ProcessConfigurationFactory`, `PipedProcessResult`, `CliRun.UseExternalProcessFactory`, `ExitConfiguration` setter usage, `Piped*Async` methods, and `ArgumentsList`
- Build a per-file punch list, excluding the legacy v1-to-v2 paths

Verify: the punch list covers every flagged file before editing starts

### Step 2 - Verify the sub-builder seam's v3 status

Where: `src/CliInvoke.Core/`, `site/docs/guides/migrating-from-sub-builder-interfaces.md`

- Check the v3 source for what happened to the sub-builder interfaces before writing anything

Verify: the page's refreshed claims match the actual v3 API

### Step 3 - Refresh the guide pages

Where: `site/docs/guides/architecture.md`, `middleware.md`, `resource-disposal.md`, `troubleshooting.md`, `migrating-from-sub-builder-interfaces.md`

- Purge v2-era content per the punch list while keeping each page's structure
- State removed concepts as removed rather than deleting the surrounding text

Verify: the step 1 grep returns hits only in the legacy-exempt paths

### Step 4 - Refresh the standalone pages and readmes

Where: `comparison.md`, `everything-wrong-with-process-in-csharp.md`, `building-cliinvoke.md`, `Supported-OperatingSystems.md`, the four readme files, and root `README.md`

- Update code samples to v3, version references to 3.0.0 stable, badges, and any v2-era framing

Verify: no v2-only code sample or version reference remains

### Step 5 - Update menus and label the legacy exception

Where: the three menu.yml files, `site/docs/migration-guides/migrating-v1-to-v2.md`, `site/docs/migration-guides/v1-to-v2/`

- Fix labels and links in the menus
- Label the legacy v1-to-v2 guide clearly for users still on v1

Verify: menu links resolve; the legacy label is present and unambiguous

## Context pointers

##### Files

- `site/docs/guides/**` (five pages), `site/docs/*.md` (four standalone pages), four readme files, three menu.yml files
- `README.md` (repo root)
- `site/docs/migration-guides/migrating-v1-to-v2.md` and `site/docs/migration-guides/v1-to-v2/` - the legacy exception, to label not to rewrite
- `site/docs/migration-guides/3.0.0.md` - the completed guide to cross-link where upgrade context helps
- `GLOSSARY.md` - shared vocabulary source
- `src/CliInvoke.Core/` - verify the sub-builder seam's v3 status here

##### ADRs

- None directly relevant

##### Domain terms

- `v2-style code` - the classification term used when judging whether content is v2-era

##### Ledger records

- `DECISIONS-CliInvoke-v3-stable-readiness.md#T005` - targeted refresh treatment; no pruning; shared vocabulary from the glossary
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D007` - the legacy exception - the v1-to-v2 guide stays, labeled
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D001` - the all-public-docs-updated-to-v3 constraint
- `DECISIONS-CliInvoke-v3-stable-readiness.md#T002` - satisfies gate criteria 6 and 10

## Acceptance criteria

- [ ] Zero removed-API identifiers (`ProcessConfigurationFactory`, `PipedProcessResult`, `CliRun.UseExternalProcessFactory`, `ExitConfiguration` setter usage, `Piped*Async` methods) in `site/docs/` and `skills/` outside labeled legacy content (gate criterion 6)
- [ ] No v2-era instructions in `site/docs/*` and `README.md` except the labeled legacy v1-to-v2 guide (gate criterion 10)
- [ ] The legacy v1-to-v2 guide is clearly labeled "legacy - for users still on v1" and its links still resolve
- [ ] The sub-builder-interfaces page states v3's construction shape verified against source
- [ ] Menu labels and links are current; root `README.md` shows v3 usage and disposal guidance

## Dependencies

**Blocked by** - [003-migration-guide-catalog-and-walkthroughs](../migration-guide/003-migration-guide-catalog-and-walkthroughs.md) - refreshed pages cross-link the migration guide, so the guide must exist in its final shape first
