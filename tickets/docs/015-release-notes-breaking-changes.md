---
title: Release notes and README breaking-changes documentation
classification: Independent
blocked_by: ["001-shell-detector-cancellation", "002-adapter-platform-fixes", "003-filepathresolver-unix-match", "004-processwrapper-lifecycle", "005-processwrapper-truncation", "006-externalprocess-synchronization", "007-equality-contracts", "008-powershell-targetfilepath", "009-retry-middleware", "010-redirect-collapse-docs", "011-validator-registration", "012-pathenvironmentvariable-home", "013-caching-resolver-keys"]
parent: IMPLEMENTATION-bug-audit-fixes.md
---

## Goal

Document every breaking and behavior change this batch introduces, so users upgrading see exactly what changed and why.

## What to build

Breaking changes to document (from the blueprint's release-notes surface):

- Unix admin and credential requests now throw `PlatformNotSupportedException` (DECISIONS-CliInvoke-bug-audit-fixes.md#T003).
- `null == null` operator semantics for the result, configuration, and exception-info types (DECISIONS-CliInvoke-bug-audit-fixes.md#T008, DECISIONS-CliInvoke-bug-audit-fixes.md#T010).
- `ProcessId` in `BufferedProcessResult` equality — stricter comparisons (DECISIONS-CliInvoke-bug-audit-fixes.md#T009).
- `UserCredential` hash change — password excluded from the hash (DECISIONS-CliInvoke-bug-audit-fixes.md#T011).
- Truncation `0`-cap semantics — zero is a valid zero-byte cap (DECISIONS-CliInvoke-bug-audit-fixes.md#T021).
- Narrowed catches surfacing previously swallowed failures (DECISIONS-CliInvoke-bug-audit-fixes.md#T017).

Docs updates to cover:

- Retry promise (DECISIONS-CliInvoke-bug-audit-fixes.md#T007)
- Redirect collapse (DECISIONS-CliInvoke-bug-audit-fixes.md#T023)
- Validator registration convention (DECISIONS-CliInvoke-bug-audit-fixes.md#T026)
- Resolver subpath semantics (DECISIONS-CliInvoke-bug-audit-fixes.md#T004)
- Truncation spellings (DECISIONS-CliInvoke-bug-audit-fixes.md#T021)

## Size

- **Files** - 2 (README.md edit plus a release-notes document, following the repository's release-notes location)

## Recommended Workflow

### Step 1 - Collect the landed behavior

Where: N/A

- Read the landed tickets 001-013 and confirm each documented behavior matches what was actually implemented.

Verify: a checklist of all eleven items maps to landed code.

### Step 2 - Write the breaking-changes section

Where: README.md and/or the release-notes document

- Document the six breaking-change items with before/after semantics and migration notes where needed.

Verify: each item cites the actual new behavior, not the audit's prediction.

### Step 3 - Write the docs-updates section

Where: README.md and/or the release-notes document

- Cover the five docs-update items.

Verify: each item is covered or already covered by the fixing ticket's XML documentation.

## Context pointers

##### Files

- `README.md` - repository documentation surface
- The landed tickets 001-013 under `tickets/` - the behavior source

##### Ledger records

- `DECISIONS-CliInvoke-bug-audit-fixes.md#T003`, `#T008`, `#T009`, `#T010`, `#T011`, `#T017`, `#T021` - the breaking changes to document
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T004`, `#T007`, `#T023`, `#T026` - the docs updates to cover

## Acceptance criteria

- [ ] All six breaking-change items are documented with accurate before/after semantics (T003, T008, T009, T010, T011, T017, T021)
- [ ] All five docs-update items are covered (T004, T007, T023, T026, T021)
- [ ] Nothing is documented that a landed ticket did not actually implement

## Dependencies

**Blocked by** - 001-shell-detector-cancellation, 002-adapter-platform-fixes, 003-filepathresolver-unix-match, 004-processwrapper-lifecycle, 005-processwrapper-truncation, 006-externalprocess-synchronization, 007-equality-contracts, 008-powershell-targetfilepath, 009-retry-middleware, 010-redirect-collapse-docs, 011-validator-registration, 012-pathenvironmentvariable-home, 013-caching-resolver-keys
