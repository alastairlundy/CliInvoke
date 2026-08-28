---
title: Verify GLOSSARY D008 terms present
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-DECISIONS-CliInvoke-v3-internals-visibility.md
---

## Goal

Confirm GLOSSARY.md defines the four D008 terms with the exact ledger definitions; add any missing term. (Verification shows all four already present at lines 59 to 73, so no edit is expected.)

## What to build

- Read `GLOSSARY.md` "Internal Visibility and Coupling" section.
- Confirm the four terms exist with the exact D008 definitions: InternalsVisibleTo grant, Cross-package coupling point, Polyfill leakage, Entrypoint package.
- If any term is missing or diverges from the ledger definition, add/correct it.

## Size

- Files - 1 (verify; edit only if a term is missing)
- Large Files to be created - omitted
- Large Edits required - omitted

## Recommended Workflow

### Step 1 - Verify the four terms

Where: GLOSSARY.md

- Check lines 59 to 73 for the four D008 terms and their exact definitions.

Verify: all four terms present and match DECISIONS-CliInvoke-v3-internals-visibility.md#D008.

### Step 2 - Add missing term if needed

Where: GLOSSARY.md

- If a term is absent, append it under "Internal Visibility and Coupling" with the exact ledger wording.

Verify: the term now matches D008.

## Context pointers

##### Files

- GLOSSARY.md — "Internal Visibility and Coupling" section (lines 57 to 73).

##### ADRs

- None constrain this ticket.

##### Domain terms

- The four D008 terms are the subject of this ticket; do not reproduce the glossary.

##### Ledger records

- DECISIONS-CliInvoke-v3-internals-visibility.md#D008 — the four terms and their exact definitions.

## Acceptance criteria

- [ ] GLOSSARY.md defines InternalsVisibleTo grant, Cross-package coupling point, Polyfill leakage, and Entrypoint package with the exact D008 definitions.
- [ ] If any term was missing, it has been added.

## Dependencies

Blocked by - None - can start immediately
