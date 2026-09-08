---
title: FilePathResolver Unix filename match - narrowed catch and contract docs
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-bug-audit-fixes.md
---

## Goal

Fix the Unix directory-enumeration match predicate so relative subdirectory resolution works on Unix, and pin the resolver's contract in XML documentation.

## What to build

- The match predicate at `src/CliInvoke/FilePathResolver.cs:229-230` compares `f.Name` against `fileName` (extracted via `Path.GetFileName`) on both platforms — `Ordinal` comparison on Unix, `OrdinalIgnoreCase` on Windows (DECISIONS-CliInvoke-bug-audit-fixes.md#T004).
- Add XML remarks documenting the inferred-directory semantics for relative subpaths (DECISIONS-CliInvoke-bug-audit-fixes.md#T004).
- Narrow the bare catch at `src/CliInvoke/FilePathResolver.cs:187-189` with a justification comment (DECISIONS-CliInvoke-bug-audit-fixes.md#T017). See the catch-discipline reconciliation step below before editing.
- Excluded, glossary-locked: the Windows extension-lowercasing quirk (`:207-225`) and the PATH-first strategy. Do not touch either.

## Size

- **Files** - 2 (1 source edit, 1 test edit)

## Recommended Workflow

### Step 1 - Confirm sites and the catch's governing method

Where: src/CliInvoke/FilePathResolver.cs

- Read the file; confirm the match predicate (reported at lines 229-230) and the bare catch (reported at lines 187-189).
- Determine which method contains the bare catch. The glossary's catch discipline for `Try*` methods requires `FilePathResolverBase.TryResolveFilePath` (and direct `IFilePathResolver` implementers following the same discipline) to catch `Exception` so a `Try*` method never propagates. If the bare catch sits inside a `Try*`-governed path, the glossary convention governs: keep the broad catch and write the justification comment citing the convention instead of narrowing. If it is not `Try*`-governed, narrow it per T017.

Verify: the governing method for the catch is identified and the reconciliation decision is recorded in the justification comment.

### Step 2 - Fix the match predicate

Where: src/CliInvoke/FilePathResolver.cs

- Change the predicate to compare `f.Name` against `fileName` on both platforms, using `Ordinal` on Unix and `OrdinalIgnoreCase` on Windows.

Verify: a relative subpath containing a directory component resolves on Unix (manual or test check).

### Step 3 - Document the contract

Where: src/CliInvoke/FilePathResolver.cs

- Add XML remarks documenting the inferred-directory semantics for relative subpaths.

Verify: XML documentation builds without warnings.

### Step 4 - Add regression tests

Where: tests/CliInvoke.Tests/Resolvers/FilePathResolverTests.cs

- Add regression tests for relative-subdirectory resolution (DECISIONS-CliInvoke-bug-audit-fixes.md#T027).
- Make Unix-path cases OS-conditional — they verify on Ubuntu CI (continuous integration); Windows-local runs cannot cover them.

Verify: dotnet test passes; Unix cases green on Ubuntu CI.

## Context pointers

##### Files

- `src/CliInvoke/FilePathResolver.cs` - the only source file this ticket edits
- `tests/CliInvoke.Tests/Resolvers/FilePathResolverTests.cs` - regression tests

##### Domain terms

- Catch discipline for `Try*` methods - `FilePathResolverBase.TryResolveFilePath` catches `Exception` by convention and direct implementers must follow the same discipline; reconcile with T017 per Step 1.
- Resolution order rationale - PATH-first, then directory recursion, is a performance contract; reordering requires a new decision record. This ticket must not reorder.

##### Ledger records

- `DECISIONS-CliInvoke-bug-audit-fixes.md#T004` - fix the match predicate on both platforms with per-OS comparison; document inferred-directory semantics; Windows lowercasing quirk and PATH-first excluded
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T017` - narrow the bare catch with justification; reconcile with the glossary Try* catch discipline
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T001` - existing files only
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T027` - regression tests; OS-conditional Unix verification

## Acceptance criteria

- [ ] Match predicate compares `f.Name` against `fileName` on both platforms — `Ordinal` on Unix, `OrdinalIgnoreCase` on Windows (T004)
- [ ] XML remarks document the inferred-directory semantics for relative subpaths (T004)
- [ ] The bare catch is narrowed with a justification comment, or the glossary `Try*` catch discipline is cited in the justification if the site is convention-governed (T017)
- [ ] The Windows extension-lowercasing quirk and the PATH-first strategy are untouched
- [ ] Relative-subdirectory regression tests pass on Ubuntu CI (T027)

## Dependencies

**Blocked by** - None - can start immediately
