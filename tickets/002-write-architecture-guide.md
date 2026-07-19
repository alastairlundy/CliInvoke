---
title: Write architecture guide
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CliInvoke-docs-site-fixes.md
---

## Goal

Replace the 6-line placeholder at `site/docs/guides/architecture.md` with a substantial conceptual guide that explains CliInvoke's internal data-flow, the relationship between the three invocation patterns and the configuration lifecycle, and the key abstractions. This satisfies D001 and D002.

## What to build

The current `site/docs/guides/architecture.md` contains only a front-matter block and a single sentence - "Placeholder for Architecture conceptual guide. Migrate content from DocFX source and include diagrams and examples." Readers who navigate to this page from the guides section or the docs hub find nothing of value.

Write a full conceptual guide covering -

1. The four-stage data-flow - Builder → Configuration Model → Invoker → Result. Explain each stage, its responsibilities, and the key types involved.
2. How the three invocation patterns (`CliRun`, `IProcessInvoker`, `IExternalProcess`) map onto this data-flow. Show where each pattern enters and exits the pipeline.
3. The role of the Process Invocation Pipeline (the layered interceptor pattern) and where cross-cutting concerns (logging, path resolution, result validation) fit.
4. Diagrams (Mermaid or ASCII) that visualise the data-flow and the pattern mapping.

The guide should be substantial (prose, examples, diagrams), consistent in tone and structure with the other guides (choosing-invocation-pattern, configuration, resource-disposal, troubleshooting), and cross-reference them where appropriate.

## Recommended Workflow

### Step 1 — Survey existing architecture content

Where: `site/docs/guides/architecture.md`, `site/docs/architecture.md`, `PATTERNS.md`, `CONTEXT.md`

- Read the current placeholder to confirm what exists.
- Read `site/docs/architecture.md` (the top-level docs page) for any existing content that can be migrated or adapted.
- Read `PATTERNS.md` for the canonical description of the three invocation patterns.
- Read `CONTEXT.md` for the glossary definition of Process Invocation Pipeline and Process Invocation Context.

Verify: Identify all source material available for the architecture guide.

### Step 2 — Write the data-flow section

Where: `site/docs/guides/architecture.md`

- Describe the four-stage lifecycle - Builder → Configuration Model → Invoker → Result.
- For each stage, explain its responsibilities, the key types, and the invariants (e.g., the model is immutable after `Build()`, the invoker does not retain references after returning).
- Include a Mermaid or ASCII diagram showing the data-flow.

Verify: A reader can trace a configuration from construction to result and name the type at each stage.

### Step 3 — Write the invocation-pattern mapping section

Where: `site/docs/guides/architecture.md`

- Explain how `CliRun`, `IProcessInvoker`, and `IExternalProcess` each consume the configuration model and produce a result.
- Show where each pattern enters and exits the data-flow. `CliRun` builds the configuration internally; `IProcessInvoker` takes it as a parameter; `IExternalProcess` splits the start and capture into separate calls.
- Include a diagram or table mapping patterns to stages.

Verify: A reader who has read the choosing-invocation-pattern guide can see how their chosen pattern fits into the internal architecture.

### Step 4 — Write the Process Invocation Pipeline section

Where: `site/docs/guides/architecture.md`

- Describe the layered interceptor pattern used to execute cross-cutting concerns around process execution.
- Explain the Process Invocation Context (the state-bearing object passed through the pipeline).
- Name the built-in interceptors and their order, if documented.

Verify: A reader understands where custom middleware fits and how to reason about the pipeline.

### Step 5 — Add cross-references

Where: `site/docs/guides/architecture.md`

- Link to the other guides where their detail is relevant - choosing-invocation-pattern for pattern selection, configuration for the model reference, resource-disposal for ownership rules.
- Link to `PATTERNS.md` for full API-level detail.

Verify: All cross-reference links resolve to existing pages.

### Step 6 — Build and verify locally

Where: `site/`

- Run `lunet build` (or `lunet serve`) to confirm the guide renders without errors.
- Check that diagrams render, links resolve, and the page is consistent in tone with the other guides.

Verify: The page is substantial, polished, and consistent with the rest of the guides section.

## Context pointers

**Files** -
- `site/docs/guides/architecture.md` — the placeholder to replace.
- `site/docs/architecture.md` — existing top-level docs page that may contain migratable content.
- `PATTERNS.md` (repo root) — canonical description of the three invocation patterns.
- `CONTEXT.md` (repo root) — glossary definitions for Process Invocation Pipeline and Process Invocation Context.

**ADRs** - None.

**Domain terms** -
- **Process Invocation Pipeline** — the layered interceptor pattern; the guide must explain this.
- **Process Invocation Context** — the state-bearing object passed through the pipeline.
- **Resource-Owning Type** — relevant when discussing disposal ownership in the data-flow.

**Ledger records** -
- `DECISIONS-CliInvoke-docs-site-fixes.md#D001` — the session goal; the guide must be helpful and polished, not a placeholder.
- `DECISIONS-CliInvoke-docs-site-fixes.md#D002` — the Guides section must feel cohesive; each guide must be a substantial page with prose, examples, and links.

## Acceptance criteria

- [ ] The body of `site/docs/guides/architecture.md` no longer contains the placeholder text.
- [ ] The guide covers the four-stage data-flow (Builder → Model → Invoker → Result) with prose and a diagram.
- [ ] The guide explains how the three invocation patterns map onto the data-flow.
- [ ] The guide describes the Process Invocation Pipeline and Process Invocation Context.
- [ ] The guide includes at least one diagram (Mermaid or ASCII).
- [ ] The guide cross-references the other guides (choosing-invocation-pattern, configuration, resource-disposal) where appropriate.
- [ ] All cross-reference links resolve to existing pages.
- [ ] The guide is consistent in tone and structure with the other guides in the section.

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** - None - can start immediately.
