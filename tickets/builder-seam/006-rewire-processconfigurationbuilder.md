---
title: Rewire ProcessConfigurationBuilder to hold and use specs
classification: Independent
blocked_by: ["001-create-argumentsspec.md", "002-create-environmentvariablesspec.md", "003-create-processresourcepolicyspec.md", "004-create-usercredentialspec.md", "005-change-iprocessconfigurationbuilder-signatures.md"]
parent: docs/decisions/DECISIONS-CliInvoke-configuration-seam-stack.md
---

## Goal

Rewire `ProcessConfigurationBuilder` so it holds the four spec instances as shared fields, creates them eagerly, invokes the `ConfigureXxx` actions on them, extracts via `spec.Build()`, and disposes disposable specs - replacing the former sub-builder fields.

## What to build

Modify `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`:
- Replace the four sub-builder fields (`_argumentsBuilder`, `_environmentVariablesBuilder`, `_processResourcePolicyBuilder`, `_userCredentialBuilder`) with spec fields (`_argumentsSpec`, `_environmentVariablesSpec`, `_processResourcePolicySpec`, `_userCredentialSpec`). Create them eagerly in the constructor, matching the existing eager pattern at `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs:54-57` (per `T012`). `ArgumentValidationLogic` is passed to `ArgumentsSpec` if the parent ctor accepts an optional one.
- Each `ConfigureXxx` invokes the action on the shared spec field (per `T010`). A second `ConfigureXxx` call re-mutates the same shared spec.
- `Build()` extracts via `spec.Build()` for each concept (per `T010`, `T011`), producing the same `BuilderProcessConfiguration` as today.
- `Dispose()` chains into disposal of every disposable spec, notably `UserCredentialSpec`, per `D004`/`T010`.

## Size

- **Files** - 1 (modify `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`)

## Recommended Workflow

### Step 1 — Replace fields and create specs eagerly

Where: `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`

- Remove the four `readonly` sub-builder field declarations (lines 41-44).
- Add the four spec fields and initialise them in the constructor (replacing lines 54-57), preserving the eager creation pattern.

Verify: Constructor compiles and all four specs are allocated even if unused.

### Step 2 — Update ConfigureXxx to invoke on shared specs

Where: `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`

- Update `ConfigureArguments`, `ConfigureEnvironmentVariables`, `ConfigureProcessResourcePolicy`, `ConfigureUserCredential` to invoke the action on the corresponding shared spec field instead of the former sub-builder.

Verify: Each `ConfigureXxx` mutates the shared spec; repeated calls re-mutate the same instance.

### Step 3 — Update Build to extract via spec.Build

Where: `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`

- Update `Build()` (lines 399-415) to call `_argumentsSpec.Build()`, `_environmentVariablesSpec.Build()`, `_processResourcePolicySpec.Build()`, `_userCredentialSpec.Build()` and pass the results into `BuilderProcessConfiguration` unchanged.

Verify: `Build()` produces an equivalent `ProcessConfiguration`.

### Step 4 — Update Dispose to chain spec disposal

Where: `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`

- Update `Dispose()` (lines 418-423) to dispose every disposable spec (notably `_userCredentialSpec`) in addition to `_standardInput`, per `D004`/`T010`.

Verify: `Dispose()` disposes `UserCredentialSpec` (and thus its `SecureString`) without double-dispose.

## Context pointers

**Files** - `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs` (modify, fields 41-44, ctor 51-73, Configure methods 115-151/145-151/228-235/292-300, Build 399-415, Dispose 418-423); spec classes from TK001-TK004; interface from TK005
**ADRs** - None
**Domain terms** - config-seam collapse; SecureString lifecycle (disposal chain anchored on `UserCredentialSpec`)
**Ledger records** - `DECISIONS-CliInvoke-configuration-seam-stack.md#D004` (credential disposal path), `#T010` (spec lifecycle/ownership), `#T011` (internal state representation), `#T012` (spec factory wiring)

## Acceptance criteria

- [ ] Four spec fields replace the four sub-builder fields; all are created eagerly in the constructor.
- [ ] Each `ConfigureXxx` invokes the action on the shared spec field; repeated calls re-mutate the same spec.
- [ ] `Build()` extracts each concept via `spec.Build()` and produces an equivalent configuration.
- [ ] `Dispose()` disposes every disposable spec (notably `UserCredentialSpec`) per `D004`.

## Dependencies

**Blocked by** - `001-create-argumentsspec.md`, `002-create-environmentvariablesspec.md`, `003-create-processresourcepolicyspec.md`, `004-create-usercredentialspec.md`, `005-change-iprocessconfigurationbuilder-signatures.md`
