---
title: Relocate PathEnvironmentVariable to CliInvoke; remove FilePathResolverBase
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-DECISIONS-CliInvoke-v3-internals-visibility.md
---

## Goal

Eliminate the CliInvoke.Core to CliInvoke coupling for path resolution by deleting the public abstract `FilePathResolverBase` from Core, moving `PathEnvironmentVariable` into CliInvoke as an internal type, and reworking `FilePathResolver` to implement `IFilePathResolver` directly without depending on Core internals. This is a v3 breaking change (D006).

## What to build

- Delete `src/CliInvoke.Core/FilePathResolverBase.cs` (public abstract class).
- Delete `src/CliInvoke.Core/Internal/IO/PathEnvironmentVariable.cs` (logic moves to CliInvoke).
- Create `src/CliInvoke/Internal/IO/PathEnvironmentVariable.cs` — `internal static class PathEnvironmentVariable` enumerating PATH/PATHEXT (ported logic from the deleted Core file).
- Edit `src/CliInvoke/FilePathResolver.cs` — implement `IFilePathResolver` directly; use the new CliInvoke `PathEnvironmentVariable`; do not reference the removed Core base class or Core's internal escaper. Preserve existing public path-resolution behavior (PATH-first then directory recursion per GLOSSARY Design Decision 1).
- Edit `tests/CliInvoke.Tests/Resolvers/FilePathResolverBaseTests.cs` — rework the `TestableFilePathResolver : FilePathResolverBase` stub to derive from the reworked `FilePathResolver` (or a new test-only base) since `FilePathResolverBase` is gone.

## Size

- Files - 5 (2 deleted, 1 created, 2 edited)
- Large Files to be created - omitted (new file is well under 500 lines)
- Large Edits required - omitted (total is well under 500 lines)

## Recommended Workflow

### Step 1 - Port PathEnvironmentVariable into CliInvoke

Where: src/CliInvoke/Internal/IO/PathEnvironmentVariable.cs (new)

- Create `internal static class PathEnvironmentVariable` with the PATH/PATHEXT enumeration logic copied from the deleted Core file.

Verify: the type compiles as `internal` in the CliInvoke assembly.

### Step 2 - Delete Core PathEnvironmentVariable and FilePathResolverBase

Where: src/CliInvoke.Core/Internal/IO/PathEnvironmentVariable.cs, src/CliInvoke.Core/FilePathResolverBase.cs

- Delete both files.

Verify: grep for `PathEnvironmentVariable` in CliInvoke.Core returns no definitions; grep for `FilePathResolverBase` in src returns no remaining references except the test stub (handled in Step 4).

### Step 3 - Rework FilePathResolver

Where: src/CliInvoke/FilePathResolver.cs

- Change `FilePathResolver` to implement `IFilePathResolver` directly (remove any `: FilePathResolverBase`).
- Replace base-class calls with the new CliInvoke `PathEnvironmentVariable` and the existing `IFilePathResolver` contract.
- Preserve resolution order (PATH first, then directory recursion) and the `Try*` catch discipline (catch `Exception`, per GLOSSARY Design Decision 4).

Verify: `FilePathResolver` builds and its public methods behave as before.

### Step 4 - Fix the test stub

Where: tests/CliInvoke.Tests/Resolvers/FilePathResolverBaseTests.cs

- Replace `TestableFilePathResolver : FilePathResolverBase` with a stub deriving from the reworked `FilePathResolver` (or a minimal `IFilePathResolver` test double).

Verify: the test file compiles; existing resolver tests pass.

### Step 5 - Build and test

Where: N/A

- Build the solution and run the CliInvoke.Tests resolver tests.

Verify: build succeeds; resolver tests green.

## Context pointers

##### Files

- src/CliInvoke.Core/FilePathResolverBase.cs — public abstract class to delete (v3 breaking, D006).
- src/CliInvoke.Core/Internal/IO/PathEnvironmentVariable.cs — logic to relocate.
- src/CliInvoke/Internal/IO/PathEnvironmentVariable.cs — new home for the relocated logic.
- src/CliInvoke/FilePathResolver.cs — reworked to drop the Core base class.
- tests/CliInvoke.Tests/Resolvers/FilePathResolverBaseTests.cs — test stub rework.

##### ADRs

- None constrain this ticket.

##### Domain terms

- Entrypoint package — Core/CliInvoke/Extensions/Specializations each provide a distinct consumer entrypoint; by design may need limited internal access.
- Cross-package coupling point — removing the Core to CliInvoke path-resolution coupling is the goal here.

##### Ledger records

- DECISIONS-CliInvoke-v3-internals-visibility.md#T002 — remove FilePathResolverBase; relocate PathEnvironmentVariable; rework FilePathResolver.
- DECISIONS-CliInvoke-v3-internals-visibility.md#D006 — removing the public FilePathResolverBase is a v3 major-version breaking change.
- DECISIONS-CliInvoke-v3-internals-visibility.md#D002 — minimize the IVT-visible surface; relocate rather than narrow.
- DECISIONS-CliInvoke-v3-internals-visibility.md#D009 — after this, CliInvoke consumes no Core internal for path resolution.

## Acceptance criteria

- [ ] `FilePathResolverBase` no longer exists in CliInvoke.Core; any external consumers would break (expected v3 breaking change).
- [ ] `PathEnvironmentVariable` exists only in CliInvoke as `internal static` with equivalent PATH/PATHEXT behavior.
- [ ] `FilePathResolver` implements `IFilePathResolver` directly and uses the CliInvoke `PathEnvironmentVariable`; no reference to removed Core types.
- [ ] Public path-resolution behavior (PATH-first, directory recursion, `Try*` catch discipline) is preserved.
- [ ] The test stub compiles and resolver tests pass.
- [ ] The solution builds.

## Dependencies

Blocked by - None - can start immediately
