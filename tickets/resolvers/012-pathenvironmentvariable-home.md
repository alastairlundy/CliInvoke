---
title: PathEnvironmentVariable HOME expansion restructure
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-bug-audit-fixes.md
---

## Goal

Fix the stale token index in `EnumerateDirectories` so mixed tilde and `$HOME` PATH (environment variable) entries expand correctly, with cleaner single-pass code.

## What to build

- Restructure `EnumerateDirectories` in `src/CliInvoke/Internal/IO/PathEnvironmentVariable.cs` (reported at `:56-82`): expand `~` first, then locate `$HOME` on the expanded string in one sequential pass, so the token index is never stale (DECISIONS-CliInvoke-bug-audit-fixes.md#T024).
- One `GetFolderPath` fetch per entry (DECISIONS-CliInvoke-bug-audit-fixes.md#T024).
- Mixed `~`/`$HOME` entries now expand correctly — accepted behavior change.

## Size

- **Files** - 2 (1 source edit, 1 test file created or edited)

## Recommended Workflow

### Step 1 - Confirm the two-pass structure and the stale index

Where: src/CliInvoke/Internal/IO/PathEnvironmentVariable.cs

- Read the method; confirm the current expansion order produces a stale index for mixed entries.

Verify: a mixed `~`/`$HOME` entry mis-expands (or is unexpanded) before the fix.

### Step 2 - Restructure into one sequential pass

Where: src/CliInvoke/Internal/IO/PathEnvironmentVariable.cs

- Expand `~` first, then locate `$HOME` on the expanded string, in a single sequential pass per entry.
- Fetch `GetFolderPath` once per entry.

Verify: mixed `~`/`$HOME` entries expand correctly; per-entry fetch count is one.

### Step 3 - Add OS-conditional tests

Where: tests/ (test file covering PathEnvironmentVariable)

- Add tests for mixed `~`/`$HOME` entries and single-token entries (DECISIONS-CliInvoke-bug-audit-fixes.md#T027).
- Make Unix-path cases OS-conditional — they verify on Ubuntu CI (continuous integration); Windows-local runs cannot cover them.

Verify: dotnet test passes; Unix cases green on Ubuntu CI.

## Context pointers

##### Files

- `src/CliInvoke/Internal/IO/PathEnvironmentVariable.cs` - the only source file this ticket edits

##### Domain terms

- None beyond the glossary's general scope — this ticket is internal IO (input/output) helper work.

##### Ledger records

- `DECISIONS-CliInvoke-bug-audit-fixes.md#T024` - expand tilde first, then locate $HOME on the expanded string, one sequential pass; one GetFolderPath fetch per entry; mixed entries now correct
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T001`, `DECISIONS-CliInvoke-bug-audit-fixes.md#T027` - existing files only; OS-conditional tests verified on Ubuntu CI

## Acceptance criteria

- [ ] `~` is expanded before `$HOME` is located, in one sequential pass, with no stale token index (T024)
- [ ] One `GetFolderPath` fetch per entry (T024)
- [ ] Mixed `~`/`$HOME` entries expand correctly; tests are OS-conditional and pass on Ubuntu CI (T027)

## Dependencies

**Blocked by** - None - can start immediately
