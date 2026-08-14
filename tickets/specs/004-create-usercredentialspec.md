---
title: Create UserCredentialSpec
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CliInvoke-configuration-seam-stack.md
---

## Goal

Introduce the sealed `UserCredentialSpec` class as the single user-credential configuration seam in `CliInvoke.Core`, replacing `UserCredentialBuilder` / `IUserCredentialBuilder` and anchoring the `SecureString` disposal lifecycle.

## What to build

Create `src/CliInvoke.Core/Configuration/UserCredentialSpec.cs`. Sealed class in namespace `CliInvoke.Core.Configuration` implementing `IDisposable` (no interface remains, per the Interface + Impl to Sealed Impl collapse).

API surface (per `T004`): `SetDomain(string)`, `SetUsername(string)`, `SetPassword(SecureString)`, `SetUserProfileLoading(bool)` (the former `LoadUserProfile` renamed to follow the `Set*` cadence without the verb-on-verb collision), `Build()`, `Dispose()`.

Constructor (per `T009`): parameterless only (the all-fields constructor is dropped; callers use the `Set*` methods inside the `ConfigureUserCredential` lambda).

Internal state (per `T011`): 4 fields + `SecureString`, mirroring `UserCredentialBuilder` at `src/CliInvoke/Builders/UserCredentialBuilder.cs`.

Disposal (per `D004`): `UserCredentialSpec.Dispose()` handles the `SecureString` lifecycle. Avoid double-dispose of the same `SecureString` reference - either make `Dispose` idempotent, or transfer ownership to the built `UserCredential` and have the spec release its reference without disposing. The parent builder's `Dispose()` chain (established in TK006) invokes this.

## Size

- **Files** - 1 (create `src/CliInvoke.Core/Configuration/UserCredentialSpec.cs`)

## Recommended Workflow

### Step 1 — Create the UserCredentialSpec class skeleton

Where: `src/CliInvoke.Core/Configuration/UserCredentialSpec.cs`

- Define `public sealed class UserCredentialSpec : IDisposable` in namespace `CliInvoke.Core.Configuration`.
- Add the 4 fields (`string? Domain`, `string? UserName`, `SecureString? Password`, `bool? LoadUserProfile`) plus the `SecureString` reference.
- Implement the parameterless constructor initialising fields to null/false.

Verify: Class compiles and implements `IDisposable`.

### Step 2 — Implement the Set methods and Build

Where: `src/CliInvoke.Core/Configuration/UserCredentialSpec.cs`

- Implement `SetDomain(string)`, `SetUsername(string)`, `SetPassword(SecureString)`, `SetUserProfileLoading(bool)` with the same argument-validation semantics as `UserCredentialBuilder`.
- Implement `Build()` returning a `UserCredential` from the 4 fields.

Verify: `Build()` produces an equivalent `UserCredential`; `SetUserProfileLoading` replaces the former `LoadUserProfile` name.

### Step 3 — Implement Dispose with SecureString lifecycle

Where: `src/CliInvoke.Core/Configuration/UserCredentialSpec.cs`

- Implement `Dispose()` to dispose the held `SecureString` exactly once (idempotent guard) and call `GC.SuppressFinalize(this)`.
- Decide ownership transfer vs idempotent dispose per `D004`; ensure no double-dispose of the same `SecureString` reference.

Verify: `Dispose()` is safe to call once; the `SecureString` is released without double-dispose.

## Context pointers

**Files** - `src/CliInvoke/Builders/UserCredentialBuilder.cs` (reference for behaviour and disposal, lines 20-124); `src/CliInvoke.Core/Builders/IUserCredentialBuilder.cs` (former `IDisposable` interface); `src/CliInvoke.Core/Configuration/` (target folder)
**ADRs** - None
**Domain terms** - config-seam collapse (Interface + Impl to Sealed Impl reduction); SecureString lifecycle (the disposal ownership chain for credentials)
**Ledger records** - `DECISIONS-CliInvoke-configuration-seam-stack.md#D003` (single entry-point shape), `#D004` (credential disposal path), `#D006` (spec naming), `#D007` (file placement), `#T004` (API surface), `#T009` (constructors), `#T011` (internal state)

## Acceptance criteria

- [ ] `UserCredentialSpec` is a sealed class in namespace `CliInvoke.Core.Configuration` implementing `IDisposable`, with no interface.
- [ ] Exposes `SetDomain(string)`, `SetUsername(string)`, `SetPassword(SecureString)`, `SetUserProfileLoading(bool)`, `Build()`, `Dispose()`; `LoadUserProfile` is renamed to `SetUserProfileLoading`.
- [ ] Parameterless constructor only.
- [ ] `Dispose()` handles the `SecureString` lifecycle per `D004` without double-dispose.
- [ ] `Build()` returns a `UserCredential` equivalent to the former builder.

## Dependencies

**Blocked by** - None - can start immediately
