---
title: Document IVT-minimization principle (ADR plus CONTRIBUTING)
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-DECISIONS-CliInvoke-v3-internals-visibility.md
---

## Goal

Record the IVT-minimization principle and the D003 to D006 decisions in a new ADR, and document the principle in CONTRIBUTING.md so contributors know a new grant requires justification. No CI guard is added (D007).

## What to build

- Create `docs/adr/0001-ivt-minimization.md` (create the `docs/adr` directory; it does not yet exist). The ADR records:
  - D002 principle — grant IVT only to assemblies that strictly require it; promote/relocate specific needed types rather than narrow a grant.
  - D003 — remove unused grants now; schedule required-grant reduction later.
  - D004 — test-assembly grants excluded from reduction (used test grants stay).
  - D005 — per-type promote-vs-relocate decision.
  - D006 — version window (promotion equals v3 major; relocation to Core is non-breaking).
- Edit `CONTRIBUTING.md` — add a section stating: a new InternalsVisibleTo grant requires justification; unused grants are removed; test grants are excluded from reduction.

## Size

- Files - 2 (1 created, 1 edited)
- Large Files to be created - omitted (ADR is well under 500 lines)

## Recommended Workflow

### Step 1 - Create the ADR

Where: docs/adr/0001-ivt-minimization.md (new; create docs/adr)

- Write the ADR capturing D002 and D003 to D006 with the exact definitions from the ledger.

Verify: the file exists under docs/adr and cites D002 to D006.

### Step 2 - Update CONTRIBUTING.md

Where: CONTRIBUTING.md

- Add an IVT-minimization section: new grant needs justification; unused grants removed; test grants excluded from reduction.

Verify: CONTRIBUTING.md mentions the principle and the test-grant exclusion.

## Context pointers

##### Files

- docs/adr/ — new directory; first ADR is 0001.
- CONTRIBUTING.md — add the principle.

##### ADRs

- This ticket creates the ADR; no prior ADR constrains it.

##### Domain terms

- InternalsVisibleTo grant (IVT grant) — the mechanism being governed.
- Cross-package coupling point — what the principle reduces.
- Polyfill leakage — the failure mode the principle mitigates (I001).
- Entrypoint package — why some coupling is by design.

##### Ledger records

- DECISIONS-CliInvoke-v3-internals-visibility.md#D001 — session goal (tighter encapsulation, fewer coupling points).
- DECISIONS-CliInvoke-v3-internals-visibility.md#D002 — minimize plus promote/relocate principle.
- DECISIONS-CliInvoke-v3-internals-visibility.md#D003 — remove unused grants now.
- DECISIONS-CliInvoke-v3-internals-visibility.md#D004 — test grants excluded.
- DECISIONS-CliInvoke-v3-internals-visibility.md#D005 — per-type mechanism.
- DECISIONS-CliInvoke-v3-internals-visibility.md#D006 — version window.
- DECISIONS-CliInvoke-v3-internals-visibility.md#D007 — create ADR plus document in CONTRIBUTING; no CI guard.

## Acceptance criteria

- [ ] `docs/adr/0001-ivt-minimization.md` exists and records D002 and D003 to D006.
- [ ] `CONTRIBUTING.md` documents the IVT-minimization principle (justification required, unused removed, test grants excluded).
- [ ] No CI configuration is added (D007 Option B).

## Dependencies

Blocked by - None - can start immediately
