---
title: Validator registration semantics - Add after RemoveAll
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-bug-audit-fixes.md
---

## Goal

Replace the misleading `TryAdd` semantics in the custom result validator extensions with honest `Add` semantics and document the registration convention.

## What to build

- Replace `TryAdd{Lifetime}` with `Add{Lifetime}` in both methods of `src/CliInvoke/Extensions/DependencyInjection/AddCustomResultValidatorsExtensions.cs` (reported at `:54-69` and `:109-123`) (DECISIONS-CliInvoke-bug-audit-fixes.md#T026).
- XML documentation states the single-threaded registration convention; `IServiceCollection` remains non-thread-safe by convention; no locking is added (DECISIONS-CliInvoke-bug-audit-fixes.md#T026).

## Size

- **Files** - 2 (1 source edit, 1 test edit)

## Recommended Workflow

### Step 1 - Confirm both method bodies

Where: src/CliInvoke/Extensions/DependencyInjection/AddCustomResultValidatorsExtensions.cs

- Read the file; confirm both `TryAdd{Lifetime}` call sites and the preceding `RemoveAll` calls.

Verify: both sites located.

### Step 2 - Swap TryAdd for Add

Where: src/CliInvoke/Extensions/DependencyInjection/AddCustomResultValidatorsExtensions.cs

- Replace `TryAdd{Lifetime}` with `Add{Lifetime}` in both methods.

Verify: a double registration now throws rather than silently skipping.

### Step 3 - Document the convention

Where: src/CliInvoke/Extensions/DependencyInjection/AddCustomResultValidatorsExtensions.cs

- Add XML documentation stating the single-threaded registration convention.

Verify: XML documentation builds without warnings.

### Step 4 - Update tests

Where: tests/CliInvoke.Extensions.Tests/DependencyInjectionExtensionTests.cs

- Update or add tests reflecting the new registration semantics (DECISIONS-CliInvoke-bug-audit-fixes.md#T027).

Verify: dotnet test passes for the Extensions test project.

## Context pointers

##### Files

- `src/CliInvoke/Extensions/DependencyInjection/AddCustomResultValidatorsExtensions.cs` - the only source file this ticket edits
- `tests/CliInvoke.Extensions.Tests/DependencyInjectionExtensionTests.cs` - registration semantics tests

##### Domain terms

- Entrypoint package - Extensions is a consumer entrypoint; the change makes its DI (dependency injection) registration semantics match what the code actually does.

##### Ledger records

- `DECISIONS-CliInvoke-bug-audit-fixes.md#T026` - Add after RemoveAll in both methods; single-threaded convention documented; no locking added
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T001`, `DECISIONS-CliInvoke-bug-audit-fixes.md#T027` - existing files only; regression tests

## Acceptance criteria

- [ ] Both methods use `Add{Lifetime}` after `RemoveAll` (T026)
- [ ] XML documentation states the single-threaded registration convention (T026)
- [ ] Registration tests pass (T027)

## Dependencies

**Blocked by** - None - can start immediately
