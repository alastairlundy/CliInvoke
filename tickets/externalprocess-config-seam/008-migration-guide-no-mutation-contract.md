---
title: Migration guide for no-mutation contract on ExternalProcess
classification: Independent
blocked_by: ["001-init-only-processconfiguration-targetfilepath", "002-init-only-externalprocess-configuration", "003-update-processwrapper-ctor-resolved-fileinfo", "004-resolve-filepath-externalprocess-start", "006-drop-resolver-powershellprocessconfiguration", "007-drop-resolver-powershell-invoker-middleware"]
parent: IMPLEMENTATION-externalprocess-config-seam.md
---

## Goal

Document the no-mutation contract for consumers, with before/after code samples covering the init-only setters and the dropped ctor parameters. This is the consumer-facing artifact for the v3 BREAKING change set.

## What to build

New file `docs/decisions/MIGRATION-externalprocess-config-seam.md` (default location per the blueprint; the maintainer may move to `site/docs/migration-guides/` to match the existing v1-to-v2 convention — flag this default in the ticket body).

Content sections:

1. **No-mutation contract** — `Configuration` is not mutated after construction.
2. **init-only `ProcessConfiguration.TargetFilePath` (T004)** — before/after code samples.
3. **init-only `ExternalProcess.Configuration` and `IExternalProcess.Configuration` (T010)** — before/after code samples.
4. **Dropped ctor parameter from `PowershellProcessConfiguration` (T006)** — before/after code samples.
5. **Dropped ctor parameter from `PowershellProcessInvoker` and `PowerShellMiddleware` (T009)** — before/after code samples.
6. **How to obtain the resolved file path** — point consumers to `ProcessResult.ExecutedFilePath` (and `BufferedProcessResult.ExecutedFilePath`, `PipedProcessResult.ExecutedFilePath`).
7. **`ProcessWrapper` ctor change (T002/T003)** — note for direct consumers (this is internal; most consumers will not see it, but mention for completeness).

Open items to capture in the guide body, not as separate work:

- The shipping window (v3 pre-release vs v3 stable) is a release-time decision outside this ticket.
- `CHANGELOG.md` does not exist at repo root; no changelog entry is being added in this ticket set.

## Size

- Files: 1 (new)

## Recommended Workflow

### Step 1 — Draft the migration guide

Where: `docs/decisions/MIGRATION-externalprocess-config-seam.md` (new file)

- Cover the seven sections above (no-mutation contract, init-only setters x2, dropped ctor params x2, resolved path surface, ProcessWrapper ctor note).
- Each before/after section pairs the old API usage with the new equivalent.
- Verify: All seven sections present; before/after samples compile against the post-TK001..TK007 codebase (cross-check the citations in this ticket against actual API names).

### Step 2 — Add the v3-pre-release framing

Where: `docs/decisions/MIGRATION-externalprocess-config-seam.md`

- Add a top-of-file note that this guide accompanies the v3 pre-release that includes the no-mutation contract.
- Verify: Framing is present; the guide does NOT link to `CHANGELOG.md` (no changelog in this decomposition).

### Step 3 — Cross-ref check

Where: `docs/decisions/MIGRATION-externalprocess-config-seam.md`

- Verify all API names in the guide match the post-implementation code: `ProcessConfiguration.TargetFilePath`, `ExternalProcess.Configuration`, `IExternalProcess.Configuration`, `PowershellProcessConfiguration`, `PowershellProcessInvoker`, `PowerShellMiddleware`, `ProcessResult.ExecutedFilePath`.
- Verify: No stale API names; no broken code samples.

## Context pointers

- Files: `docs/decisions/MIGRATION-externalprocess-config-seam.md` (target of creation)
- ADRs: none (the repo has no `docs/adr/` directory per exploration; `site/docs/migration-guides/` is the existing convention for migration guides and may be the preferred location — flag in the ticket body)
- Domain terms: "Process Invocation Pipeline" (GLOSSARY.md — relevant background)
- Ledger records:
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#T013` — migration guide content scope
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#D003` — call-site documentation requirement (extends to external docs)

## Acceptance criteria

- [ ] New file exists at `docs/decisions/MIGRATION-externalprocess-config-seam.md`.
- [ ] All seven sections are present (no-mutation, init-only x2, dropped ctor x2, resolved path, ProcessWrapper note).
- [ ] Each before/after code sample compiles against the post-implementation API.
- [ ] The guide references `ProcessResult.ExecutedFilePath` (and derived types) for the resolved path.
- [ ] The guide does NOT depend on or reference `CHANGELOG.md` (no changelog in this decomposition).
- [ ] API names in the guide cross-check against TK001-TK007 deliverables.

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** — 001-init-only-processconfiguration-targetfilepath, 002-init-only-externalprocess-configuration, 003-update-processwrapper-ctor-resolved-fileinfo, 004-resolve-filepath-externalprocess-start, 006-drop-resolver-powershellprocessconfiguration, 007-drop-resolver-powershell-invoker-middleware (the guide references the final API surface from all six code tickets; accuracy depends on them landing first).
