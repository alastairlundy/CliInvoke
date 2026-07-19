---
title: Rewrite guides section landing as curated orientation
classification: Independent
blocked_by: [002]
parent: docs/decisions/DECISIONS-CliInvoke-docs-site-fixes.md
---

## Goal

Transform `site/docs/guides/readme.md` from a bare link list into a curated orientation page that introduces the guides section, names the audience, suggests a reading order, and summarises each guide. Also fix `site/docs/guides/menu.yml` to include the missing choosing-invocation-pattern entry. This satisfies D001 and D002.

## What to build

The current `site/docs/guides/readme.md` is a single paragraph followed by a bulleted list of links. It gives the reader no orientation — no sense of who the guides are for, which to read first, or how they relate to each other. The section feels assembled, not curated.

Rewrite the page to include -

1. **Audience** — who the guides are for (developers who have installed CliInvoke and want to understand how to use it well).
2. **Reading order** — a suggested sequence through the guides, with a one-line reason for the order.
3. **Guide summaries** — for each of the five guides (choosing-invocation-pattern, architecture, configuration, resource-disposal, troubleshooting), a 1-2 sentence summary of what the guide covers and when to read it.

Also fix `site/docs/guides/menu.yml`, which currently lists only four guides and omits `choosing-invocation-pattern.md`.

## Recommended Workflow

### Step 1 — Audit current guides section

Where: `site/docs/guides/readme.md`, `site/docs/guides/menu.yml`

- Read the current `readme.md` to identify what exists.
- Read `menu.yml` to confirm the missing entry.
- Skim each of the five guide pages to extract a 1-2 sentence summary for each.

Verify: Confirm the five guides are choosing-invocation-pattern, architecture, configuration, resource-disposal, and troubleshooting.

### Step 2 — Write the audience and reading-order sections

Where: `site/docs/guides/readme.md`

- Write a short paragraph naming the audience for the guides section.
- Write a suggested reading order with a one-line reason for each position. The order should make sense for a reader who has just finished the getting-started pages.

Verify: A reader can answer "should I read this section, and where do I start?" from the first two sections of the page.

### Step 3 — Write per-guide summaries

Where: `site/docs/guides/readme.md`

- For each of the five guides, write a 1-2 sentence summary covering what the guide teaches and when to read it.
- Each summary should link to the guide page.

Verify: Every guide in the section has a summary. The summaries are accurate and consistent in tone.

### Step 4 — Fix the menu configuration

Where: `site/docs/guides/menu.yml`

- Add the missing `choosing-invocation-pattern.md` entry.
- Verify the order of entries matches the suggested reading order (or a sensible default).

Verify: The menu includes all five guides.

### Step 5 — Build and verify locally

Where: `site/`

- Run `lunet build` (or `lunet serve`) to confirm the guides landing page and sidebar render correctly.
- Check that all links resolve and the page reads as a curated orientation.

Verify: The page feels cohesive, not assembled. The sidebar includes all five guides.

## Context pointers

**Files** -
- `site/docs/guides/readme.md` — the guides landing page to rewrite.
- `site/docs/guides/menu.yml` — the sidebar config; missing `choosing-invocation-pattern.md`.
- `site/docs/guides/choosing-invocation-pattern.md` — substantial guide; needs a summary.
- `site/docs/guides/architecture.md` — will be rewritten by ticket 002; summary must match the new content.
- `site/docs/guides/configuration.md` — substantial guide; needs a summary.
- `site/docs/guides/resource-disposal.md` — substantial guide; needs a summary.
- `site/docs/guides/troubleshooting.md` — substantial guide; needs a summary.

**ADRs** - None.

**Domain terms** -
- **Process Invocation Pipeline** — referenced in the architecture guide summary.
- **Resource-Owning Type** — referenced in the resource-disposal guide summary.

**Ledger records** -
- `DECISIONS-CliInvoke-docs-site-fixes.md#D001` — the session goal; the guides section must be helpful and polished.
- `DECISIONS-CliInvoke-docs-site-fixes.md#D002` — the Guides section landing must be a curated orientation covering audience, reading order, and summaries; the section must feel cohesive.

## Acceptance criteria

- [ ] `site/docs/guides/readme.md` contains an audience section, a reading-order section, and per-guide summaries.
- [ ] Every guide in the section (choosing-invocation-pattern, architecture, configuration, resource-disposal, troubleshooting) has a 1-2 sentence summary.
- [ ] `site/docs/guides/menu.yml` includes all five guides, including `choosing-invocation-pattern.md`.
- [ ] All links in the landing page resolve to existing guide pages.
- [ ] The page reads as a curated orientation, not a link list.
- [ ] The reading order is justified with a one-line reason per position.

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** - `002-write-architecture-guide.md` — the architecture guide must be written before the guides landing can summarise it accurately.
