---
title: Migration guide - catalog plus walkthroughs
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-v3-stable-readiness.md
---

# Ticket — Migration guide catalog plus walkthroughs

## Goal

Extend the existing draft at `site/docs/migration-guides/3.0.0.md` into the single, self-sufficient v2-to-v3 guide: an exhaustive breaking-change catalog, task walkthroughs for each canonical invocation pattern plus a shared construction section, removal of the alpha-line caveat, and glossary cross-links. A v2 user must be able to upgrade using only this guide.

## What to build

Edit `site/docs/migration-guides/3.0.0.md` (existing draft, 103 lines, alpha-line breaking changes only) in four moves:

1. **Catalog (TL;DR table)** - keep the existing alpha-line rows; add the GA construction-story rows with inline decision citations:
   - `ProcessConfigurationFactory` removed - construct `ProcessConfiguration` directly with init setters
   - `TargetFilePath` becomes `required` - `[SetsRequiredMembers]` on the convenience constructor
   - `ArgumentsList` merged into init-only `ArgumentList`
   - Working-directory validation moved into `ProcessConfiguration` construction (throws on non-existent directory)
   - Builder kept, positioned advanced - argument escaping, `UserCredentialSpec` and resource-policy callback flows
   - `ProcessConfigurationBuilder` init setters delegate where applicable; dependency-injection registration unchanged
2. **Task walkthroughs** - one per canonical invocation pattern plus a shared construction section:
   - `CliRun` static-call users (facade changes, config now passed as the object)
   - `IProcessInvoker` users (factory removal fallout, dependency-injection `configure` bindings)
   - `IExternalProcess` users (constructor C only, sealed type, `ExitConfiguration` at construction)
   - Shared construction section - init construction as the default, builder as the advanced path, dependency injection unchanged
   - Each walkthrough carries "Before (v2.x)" and "After (v3)" code that compiles against v3 stable
3. **Remove the alpha caveat** - delete the "begins as 3.0.0-alpha.x... will reach a stable 3.0.0" blockquote; state that the breaking-change set is final
4. **Cross-links** - cite `GLOSSARY.md`'s `v2-style code` entry where patterns are classified

The guide's completeness is audited against the frozen public API surface by ticket 001's tool.

## Size

- **Files** - 1 file to edit (`site/docs/migration-guides/3.0.0.md`)
- **Large Edits required** - the four walkthroughs plus the shared construction section and expanded catalog are expected to add more than 500 lines

## Recommended Workflow

### Step 1 - Verify the locked construction story against source

Where: `src/CliInvoke.Core/` (ProcessConfiguration, factory removal, required members)

- Confirm against the v3 source that `ProcessConfigurationFactory` is gone, `TargetFilePath` is `required`, `ArgumentList` is init-only, and working-directory validation throws at construction

Verify: every catalog row you are about to write matches the actual v3 API

### Step 2 - Add the GA construction-story catalog rows

Where: `site/docs/migration-guides/3.0.0.md`

- Keep the existing alpha-line rows; append the six construction rows listed above with inline citations to the decision records

Verify: the TL;DR table now covers both the alpha-line set and the GA construction story

### Step 3 - Write the three walkthroughs and the shared construction section

Where: `site/docs/migration-guides/3.0.0.md`

- Write one walkthrough per canonical invocation pattern, each with Before (v2.x) and After (v3) code
- Write the shared construction section - init first, builder advanced, dependency injection unchanged

Verify: the guide table of contents shows the three patterns plus the shared section

### Step 4 - Make the Before/After examples compile against v3

Where: `site/docs/migration-guides/3.0.0.md`

- Extract each After (v3) snippet and compile it against the local v3 build (a scratch console project or file-based app is acceptable)

Verify: every After (v3) example compiles against v3 stable

### Step 5 - Remove the alpha caveat and add glossary cross-links

Where: `site/docs/migration-guides/3.0.0.md`

- Delete the "begins as 3.0.0-alpha.x" blockquote; state the set is final
- Cite `GLOSSARY.md`'s `v2-style code` term where patterns are classified

Verify: a grep for "3.0.0-alpha" in the guide returns no release-state language; the glossary term is cited

## Context pointers

##### Files

- `site/docs/migration-guides/3.0.0.md` - the file to extend (existing alpha-line draft)
- `src/CliInvoke.Core/` - the frozen v3 public API the catalog must match
- `GLOSSARY.md` - the `v2-style code` entry to cross-link
- `docs/adr/0003-cli-run-defaults-facade.md` - the CliRun facade decisions the walkthrough must not contradict

##### ADRs

- `docs/adr/0003-cli-run-defaults-facade.md`

##### Domain terms

- `v2-style code` - the normative classification term, cited from `GLOSSARY.md`

##### Ledger records

- `DECISIONS-CliInvoke-v3-stable-readiness.md#D004` - catalog plus task-path shape (Option A) this ticket implements
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D002` - the locked GA construction story every catalog row and walkthrough must honour
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D003` - builder positioned advanced; init construction the default
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D009` - the `v2-style code` glossary term to cite
- `DECISIONS-CliInvoke-v3-stable-readiness.md#T007` - the three-canonical-pattern partition plus the orthogonal construction dimension
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D001` - goal 1 (self-sufficient upgrade guide) and the all-public-docs constraint

## Acceptance criteria

- [ ] The TL;DR catalog covers the alpha-line rows plus all six GA construction-story rows with inline citations
- [ ] The guide has a task walkthrough for each of the three canonical invocation patterns plus a shared construction section (gate criterion 3)
- [ ] Every After (v3) code example compiles against v3 stable (gate criterion 4)
- [ ] No "begins as 3.0.0-alpha.x" release-state language remains (gate criterion 5)
- [ ] `GLOSSARY.md`'s `v2-style code` entry is cited where patterns are classified (gate criterion 12)
- [ ] Every construction claim matches the frozen v3 API as verified against source

## Dependencies

**Blocked by** - None - can start immediately
