---
title: CachingFilePathResolver key normalization and race documentation
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-bug-audit-fixes.md
---

## Goal

Prevent redundant Windows cache entries caused by path casing differences while keeping the cache lock-free, and document the benign race.

## What to build

- Normalize cache keys per-OS (operating system) casing rules in `src/CliInvoke/Extensions/Caching/CachingFilePathResolver.cs` — case-insensitive on Windows, as-is elsewhere (DECISIONS-CliInvoke-bug-audit-fixes.md#T025).
- Document the benign concurrent-compute race in XML remarks (DECISIONS-CliInvoke-bug-audit-fixes.md#T025).
- No single-flight lock; the per-hit `File.Exists` re-verification (the time-of-check to time-of-use mitigation) stays as-is (DECISIONS-CliInvoke-bug-audit-fixes.md#T025).

## Size

- **Files** - 2 (1 source edit, 1 test edit)

## Recommended Workflow

### Step 1 - Confirm cache key construction

Where: src/CliInvoke/Extensions/Caching/CachingFilePathResolver.cs

- Read the file; confirm how cache keys are built today and where casing differences create duplicate entries on Windows.

Verify: the duplicate-entry behavior is understood and reproducible in a test.

### Step 2 - Add per-OS key normalization

Where: src/CliInvoke/Extensions/Caching/CachingFilePathResolver.cs

- Normalize keys case-insensitively on Windows; leave them as-is elsewhere.

Verify: two paths differing only by casing hit one cache entry on Windows.

### Step 3 - Document the race

Where: src/CliInvoke/Extensions/Caching/CachingFilePathResolver.cs

- Add XML remarks describing the benign concurrent-compute race (both threads may compute the same value; the result is idempotent).

Verify: XML documentation builds without warnings.

### Step 4 - Add tests

Where: tests/CliInvoke.Extensions.Tests/ (caching resolver test file)

- Test the Windows casing behavior (OS-conditional) and confirm the `File.Exists` re-verification is untouched (DECISIONS-CliInvoke-bug-audit-fixes.md#T027).

Verify: dotnet test passes for the Extensions test project.

## Context pointers

##### Files

- `src/CliInvoke/Extensions/Caching/CachingFilePathResolver.cs` - the only source file this ticket edits
- `tests/CliInvoke.Extensions.Tests/` - caching behavior tests

##### Ledger records

- `DECISIONS-CliInvoke-bug-audit-fixes.md#T025` - per-OS key normalization; race documented; no single-flight lock; File.Exists re-verification stays
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T001`, `DECISIONS-CliInvoke-bug-audit-fixes.md#T027` - existing files only; regression tests

## Acceptance criteria

- [ ] Cache keys are normalized per-OS casing rules — case-insensitive on Windows, as-is elsewhere (T025)
- [ ] The benign concurrent-compute race is documented in XML remarks (T025)
- [ ] The per-hit `File.Exists` re-verification is unchanged and no lock was added (T025)

## Dependencies

**Blocked by** - None - can start immediately
