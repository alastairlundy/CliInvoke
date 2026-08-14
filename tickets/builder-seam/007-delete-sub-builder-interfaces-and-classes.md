---
title: Delete removed sub-builder interfaces and classes
classification: Independent
blocked_by: ["005-change-iprocessconfigurationbuilder-signatures.md", "006-rewire-processconfigurationbuilder.md", "008-rewrite-configurationextensions.md", "009-replace-sub-builder-tests.md"]
parent: docs/decisions/DECISIONS-CliInvoke-configuration-seam-stack.md
---

## Goal

Remove the four sub-builder interfaces from `CliInvoke.Core` and the four sub-builder classes from `CliInvoke`, completing the hard break now that every reference has been migrated.

## What to build

DELETE the four interfaces in `src/CliInvoke.Core/Builders/` (per `D002`):
- `IArgumentsBuilder.cs`
- `IEnvironmentVariablesBuilder.cs`
- `IProcessResourcePolicyBuilder.cs`
- `IUserCredentialBuilder.cs` (including its `IDisposable` inheritance)

DELETE the four classes in `src/CliInvoke/Builders/` (per `D002`, `D004`):
- `ArgumentsBuilder.cs`
- `EnvironmentVariablesBuilder.cs`
- `ProcessResourcePolicyBuilder.cs`
- `UserCredentialBuilder.cs`

This ticket is blocked by TK005 (interface no longer references the types), TK006 (builder no longer uses the classes), TK008 (extensions no longer use the classes), and TK009 (tests no longer use the classes) so the solution compiles after deletion.

## Size

- **Files** - 8 (delete 8 files)

## Recommended Workflow

### Step 1 — Delete the four sub-builder interfaces

Where: `src/CliInvoke.Core/Builders/`

- Delete `IArgumentsBuilder.cs`, `IEnvironmentVariablesBuilder.cs`, `IProcessResourcePolicyBuilder.cs`, `IUserCredentialBuilder.cs`.

Verify: No remaining references to these interfaces in `CliInvoke.Core`.

### Step 2 — Delete the four sub-builder classes

Where: `src/CliInvoke/Builders/`

- Delete `ArgumentsBuilder.cs`, `EnvironmentVariablesBuilder.cs`, `ProcessResourcePolicyBuilder.cs`, `UserCredentialBuilder.cs`.

Verify: No remaining references to these classes anywhere in the solution (grep for `ArgumentsBuilder`, `EnvironmentVariablesBuilder`, `ProcessResourcePolicyBuilder`, `UserCredentialBuilder`).

## Context pointers

**Files** - `src/CliInvoke.Core/Builders/IArgumentsBuilder.cs`, `IEnvironmentVariablesBuilder.cs`, `IProcessResourcePolicyBuilder.cs`, `IUserCredentialBuilder.cs` (delete); `src/CliInvoke/Builders/ArgumentsBuilder.cs`, `EnvironmentVariablesBuilder.cs`, `ProcessResourcePolicyBuilder.cs`, `UserCredentialBuilder.cs` (delete)
**ADRs** - None
**Domain terms** - config-seam collapse (hard break - no deprecation window required)
**Ledger records** - `DECISIONS-CliInvoke-configuration-seam-stack.md#D002` (hard break - remove sub-builder interfaces), `#D004` (credential disposal path moved to spec)

## Acceptance criteria

- [ ] The four sub-builder interfaces are deleted from `CliInvoke.Core/Builders/`.
- [ ] The four sub-builder classes are deleted from `CliInvoke/Builders/`.
- [ ] No remaining compile-time or source references to any deleted type across the solution.

## Dependencies

**Blocked by** - `005-change-iprocessconfigurationbuilder-signatures.md`, `006-rewire-processconfigurationbuilder.md`, `008-rewrite-configurationextensions.md`, `009-replace-sub-builder-tests.md`
