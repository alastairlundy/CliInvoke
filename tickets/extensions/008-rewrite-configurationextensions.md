---
title: Rewrite ConfigurationExtensions.FromProcessStartInfo
classification: Independent
blocked_by: ["002-create-environmentvariablesspec.md", "004-create-usercredentialspec.md", "005-change-iprocessconfigurationbuilder-signatures.md"]
parent: docs/decisions/DECISIONS-CliInvoke-configuration-seam-stack.md
---

## Goal

Rewrite `FromProcessStartInfo` to flow environment variables and credentials through the single `IProcessConfigurationBuilder` seam via `ConfigureEnvironmentVariables` and `ConfigureUserCredential` lambdas, dropping the sub-builder pre-step.

## What to build

Modify `src/CliInvoke.Extensions/Configuration/ConfigurationExtensions.cs`. Rewrite `FromProcessStartInfo(ProcessStartInfo)` (per `D008`):
- Apply environment variables via the `ConfigureEnvironmentVariables` lambda (using `SetEnumerable` on `EnvironmentVariablesSpec`), removing the `EnvironmentVariablesBuilder` pre-step (lines 55-68).
- Apply credentials via the `ConfigureUserCredential` lambda (using `SetDomain`/`SetUsername`/`SetPassword`/`SetUserProfileLoading` on `UserCredentialSpec`), removing the `UserCredentialBuilder` pre-step (lines 93-108).
- Respect the `UserCredentialSpec` lifecycle established by `D004` inside the `ConfigureUserCredential` lambda (the parent builder's `Dispose()` chain owns the `SecureString`).
- No other `CliInvoke.Extensions` helper uses the sub-builder interfaces and remains unchanged.

## Size

- **Files** - 1 (modify `src/CliInvoke.Extensions/Configuration/ConfigurationExtensions.cs`)

## Recommended Workflow

### Step 1 — Rewrite environment-variable application

Where: `src/CliInvoke.Extensions/Configuration/ConfigurationExtensions.cs`

- Remove the `EnvironmentVariablesBuilder` instantiation and `SetEnumerable`/`Build` pre-step (lines 55-68).
- Inside the existing `ConfigureEnvironmentVariables` lambda, call `envConfig.SetEnumerable(kvp)` directly.

Verify: Environment variables are applied via the `ConfigureEnvironmentVariables` lambda only.

### Step 2 — Rewrite credential application

Where: `src/CliInvoke.Extensions/Configuration/ConfigurationExtensions.cs`

- Remove the `UserCredentialBuilder` instantiation and `SetDomain`/`SetPassword`/`SetUsername`/`LoadUserProfile`/`Build` pre-step (lines 93-108).
- Replace `SetUserCredential(userCredentialBuilder.Build())` with a `ConfigureUserCredential` lambda setting domain, username, password, and profile loading on `UserCredentialSpec`.

Verify: Credentials are applied via the `ConfigureUserCredential` lambda; the `SecureString` lifecycle is owned by the parent builder's disposal chain.

## Context pointers

**Files** - `src/CliInvoke.Extensions/Configuration/ConfigurationExtensions.cs` (modify, lines 49-112); spec classes from TK002/TK004; interface signatures from TK005
**ADRs** - None
**Domain terms** - config-seam collapse; SecureString lifecycle
**Ledger records** - `DECISIONS-CliInvoke-configuration-seam-stack.md#D008` (DI/Extensions impact - flow through single seam)

## Acceptance criteria

- [ ] `FromProcessStartInfo` applies environment variables via `ConfigureEnvironmentVariables` with no `EnvironmentVariablesBuilder` pre-step.
- [ ] `FromProcessStartInfo` applies credentials via `ConfigureUserCredential` with no `UserCredentialBuilder` pre-step.
- [ ] The `UserCredentialSpec` lifecycle per `D004` is respected inside the `ConfigureUserCredential` lambda.
- [ ] No other `CliInvoke.Extensions` helper is changed.

## Dependencies

**Blocked by** - `002-create-environmentvariablesspec.md`, `004-create-usercredentialspec.md`, `005-change-iprocessconfigurationbuilder-signatures.md`
