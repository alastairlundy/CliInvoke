---
title: Replace sub-builder tests with deepened-interface coverage
classification: Independent
blocked_by: ["001-create-argumentsspec.md", "002-create-environmentvariablesspec.md", "003-create-processresourcepolicyspec.md", "004-create-usercredentialspec.md", "006-rewire-processconfigurationbuilder.md"]
parent: docs/decisions/DECISIONS-CliInvoke-configuration-seam-stack.md
---

## Goal

Delete the four absorbed sub-builder test files and add equivalent coverage against the deepened `IProcessConfigurationBuilder` interface, so tests target the public seam rather than internal modules.

## What to build

DELETE (per `D005`):
- `tests/CliInvoke.Tests/Builders/ArgumentsBuilderTests.cs`
- `tests/CliInvoke.Tests/Builders/EnvironmentVariablesBuilderTests.cs`
- `tests/CliInvoke.Tests/Builders/ProcessResourcePolicyBuilderTests.cs`
- `tests/CliInvoke.Tests/Builders/UserCredentialBuilderTests.cs`

MODIFY `tests/CliInvoke.Tests/Builders/ProcessConfigurationBuilderTests.cs` (per `D005`): add equivalent coverage against the deepened `IProcessConfigurationBuilder` - the four `ConfigureXxx` entry points and the spec APIs (`ArgumentsSpec`, `EnvironmentVariablesSpec`, `ProcessResourcePolicySpec`, `UserCredentialSpec`). Specs may grow internal helpers for assertions but must not expose a public test surface of their own.

## Size

- **Files** - 5 (delete 4 test files; modify 1 test file)

## Recommended Workflow

### Step 1 — Delete the four sub-builder test files

Where: `tests/CliInvoke.Tests/Builders/`

- Delete `ArgumentsBuilderTests.cs`, `EnvironmentVariablesBuilderTests.cs`, `ProcessResourcePolicyBuilderTests.cs`, `UserCredentialBuilderTests.cs`.

Verify: The four files no longer exist and the test project still compiles (after TK006 rewires the builder).

### Step 2 — Add deepened-interface coverage

Where: `tests/CliInvoke.Tests/Builders/ProcessConfigurationBuilderTests.cs`

- Add tests exercising `ConfigureArguments(Action<ArgumentsSpec>)`, `ConfigureEnvironmentVariables(Action<EnvironmentVariablesSpec>)`, `ConfigureProcessResourcePolicy(Action<ProcessResourcePolicySpec>)`, and `ConfigureUserCredential(Action<UserCredentialSpec>)` against `IProcessConfigurationBuilder`, asserting the resulting `ProcessConfiguration` matches the former sub-builder behaviour.
- Migrate representative assertions from the deleted files; use internal spec helpers if needed (no public test surface on specs).

Verify: `dotnet test` passes from `tests/CliInvoke.Tests/` and coverage of the four `ConfigureXxx` entry points is present.

## Context pointers

**Files** - `tests/CliInvoke.Tests/Builders/ArgumentsBuilderTests.cs`, `EnvironmentVariablesBuilderTests.cs`, `ProcessResourcePolicyBuilderTests.cs`, `UserCredentialBuilderTests.cs` (delete); `tests/CliInvoke.Tests/Builders/ProcessConfigurationBuilderTests.cs` (modify); builder from TK006; specs from TK001-TK004
**ADRs** - None
**Domain terms** - config-seam collapse (test surface is the deepened interface)
**Ledger records** - `DECISIONS-CliInvoke-configuration-seam-stack.md#D005` (test surface - delete sub-builder tests, add interface coverage)

## Acceptance criteria

- [ ] The four sub-builder test files are deleted.
- [ ] `ProcessConfigurationBuilderTests.cs` covers the four `ConfigureXxx` entry points and spec APIs against `IProcessConfigurationBuilder`.
- [ ] Equivalent behaviour to the deleted tests is preserved; specs expose no public test surface.
- [ ] `dotnet test` passes.

## Dependencies

**Blocked by** - `001-create-argumentsspec.md`, `002-create-environmentvariablesspec.md`, `003-create-processresourcepolicyspec.md`, `004-create-usercredentialspec.md`, `006-rewire-processconfigurationbuilder.md`
