---
title: Change IProcessConfigurationBuilder ConfigureXxx signatures to specs
classification: Independent
blocked_by: ["001-create-argumentsspec.md", "002-create-environmentvariablesspec.md", "003-create-processresourcepolicyspec.md", "004-create-usercredentialspec.md"]
parent: docs/decisions/DECISIONS-CliInvoke-configuration-seam-stack.md
---

## Goal

Update the four `ConfigureXxx` method signatures on `IProcessConfigurationBuilder` to accept the new spec types (`Action<XxxSpec>`) instead of `Action<I*Builder>`, making the configuration builder the single rich seam.

## What to build

Modify `src/CliInvoke.Core/Builders/IProcessConfigurationBuilder.cs`. Change the four signatures (per `D003`):
- `ConfigureArguments(Action<ArgumentsSpec>)`
- `ConfigureEnvironmentVariables(Action<EnvironmentVariablesSpec>)`
- `ConfigureProcessResourcePolicy(Action<ProcessResourcePolicySpec>)`
- `ConfigureUserCredential(Action<UserCredentialSpec>)`

Keep `SetProcessResourcePolicy(ProcessResourcePolicy)` unchanged. The spec types are produced by TK001-TK004, so this ticket is blocked by them.

## Size

- **Files** - 1 (modify `src/CliInvoke.Core/Builders/IProcessConfigurationBuilder.cs`)

## Recommended Workflow

### Step 1 — Update the four ConfigureXxx signatures

Where: `src/CliInvoke.Core/Builders/IProcessConfigurationBuilder.cs`

- Change `ConfigureArguments(Action<IArgumentsBuilder>)` to `ConfigureArguments(Action<ArgumentsSpec>)`.
- Change `ConfigureEnvironmentVariables(Action<IEnvironmentVariablesBuilder>)` to `ConfigureEnvironmentVariables(Action<EnvironmentVariablesSpec>)`.
- Change `ConfigureProcessResourcePolicy(Action<IProcessResourcePolicyBuilder>)` to `ConfigureProcessResourcePolicy(Action<ProcessResourcePolicySpec>)`.
- Change `ConfigureUserCredential(Action<IUserCredentialBuilder>)` to `ConfigureUserCredential(Action<UserCredentialSpec>)`.
- Leave `SetProcessResourcePolicy(ProcessResourcePolicy)` unchanged.

Verify: Interface compiles and references the four spec types from `CliInvoke.Core.Configuration`.

## Context pointers

**Files** - `src/CliInvoke.Core/Builders/IProcessConfigurationBuilder.cs` (modify); `src/CliInvoke.Core/Configuration/ArgumentsSpec.cs`, `EnvironmentVariablesSpec.cs`, `ProcessResourcePolicySpec.cs`, `UserCredentialSpec.cs` (produced by TK001-TK004)
**ADRs** - None
**Domain terms** - config-seam collapse (single entry point per configuration concept)
**Ledger records** - `DECISIONS-CliInvoke-configuration-seam-stack.md#D001` (session goal - one entry point per configuration concept), `#D003` (new entry-point shape - `ConfigureXxx(Action<XxxSpec>)`)

## Acceptance criteria

- [ ] `IProcessConfigurationBuilder.ConfigureArguments` accepts `Action<ArgumentsSpec>`.
- [ ] `ConfigureEnvironmentVariables` accepts `Action<EnvironmentVariablesSpec>`.
- [ ] `ConfigureProcessResourcePolicy` accepts `Action<ProcessResourcePolicySpec>`.
- [ ] `ConfigureUserCredential` accepts `Action<UserCredentialSpec>`.
- [ ] `SetProcessResourcePolicy(ProcessResourcePolicy)` is unchanged.

## Dependencies

**Blocked by** - `001-create-argumentsspec.md`, `002-create-environmentvariablesspec.md`, `003-create-processresourcepolicyspec.md`, `004-create-usercredentialspec.md`
