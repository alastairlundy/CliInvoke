---
title: Create EnvironmentVariablesSpec
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CliInvoke-configuration-seam-stack.md
---

## Goal

Introduce the sealed `EnvironmentVariablesSpec` class as the single environment-variables configuration seam in `CliInvoke.Core`, replacing `EnvironmentVariablesBuilder` / `IEnvironmentVariablesBuilder`.

## What to build

Create `src/CliInvoke.Core/Configuration/EnvironmentVariablesSpec.cs`. Sealed class in namespace `CliInvoke.Core.Configuration`.

API surface (per `T005`, which supersedes `T002`): `SetEnumerable(IEnumerable<KeyValuePair<string, string>>)`, `Build()`, `Clear()`. The `SetPair`, `SetDictionary`, and `SetReadOnlyDictionary` methods are dropped; existing callers migrate to `SetEnumerable` (interface implementation for the dictionary overloads, array wrapping for the single-pair case). `Clear()` is retained for state-reset semantics.

Constructors (per `T007`): parameterless `()` and `(StringComparer stringComparer, bool throwExceptionIfDuplicateKeyFound)`. The bool-only and protected `(IDictionary<string, string>, StringComparer, bool)` constructors are dropped.

Internal state (per `T011`): `Dictionary<string, string>` + `StringComparer` + throw-on-duplicate flag, mirroring `EnvironmentVariablesBuilder` at `src/CliInvoke/Builders/EnvironmentVariablesBuilder.cs`.

## Size

- **Files** - 1 (create `src/CliInvoke.Core/Configuration/EnvironmentVariablesSpec.cs`)

## Recommended Workflow

### Step 1 — Create the EnvironmentVariablesSpec class skeleton

Where: `src/CliInvoke.Core/Configuration/EnvironmentVariablesSpec.cs`

- Define `public sealed class EnvironmentVariablesSpec` in namespace `CliInvoke.Core.Configuration`.
- Add `private readonly Dictionary<string, string> _environmentVariables`, `private readonly StringComparer _stringComparer`, and `private readonly bool _throwExceptionIfDuplicateKeyFound` fields.
- Implement the parameterless constructor (defaults - `StringComparer.Ordinal`, throw-on-duplicate `true`) and the `(StringComparer, bool)` constructor.

Verify: Class compiles; both constructors initialise the dictionary with the chosen comparer.

### Step 2 — Implement SetEnumerable, Build, and Clear

Where: `src/CliInvoke.Core/Configuration/EnvironmentVariablesSpec.cs`

- Implement `SetEnumerable(IEnumerable<KeyValuePair<string, string>>)` mirroring the former `SetInternal` logic (null check, per-pair key/value validation, add vs override based on the throw flag).
- Implement `Build()` returning the dictionary as `IReadOnlyDictionary<string, string>`.
- Implement `Clear()` clearing the dictionary.

Verify: `SetEnumerable` populates the dictionary honouring the duplicate-key policy; `Build()` returns the read-only view; `Clear()` empties it.

## Context pointers

**Files** - `src/CliInvoke/Builders/EnvironmentVariablesBuilder.cs` (reference for behaviour, lines 19-170); `src/CliInvoke.Core/Configuration/` (target folder)
**ADRs** - None
**Domain terms** - config-seam collapse (Interface + Impl to Sealed Impl reduction)
**Ledger records** - `DECISIONS-CliInvoke-configuration-seam-stack.md#D003` (single entry-point shape), `#D006` (spec naming), `#D007` (file placement), `#T005` (API surface - supersedes `T002`), `#T007` (constructors), `#T011` (internal state)

## Acceptance criteria

- [ ] `EnvironmentVariablesSpec` is a sealed class in namespace `CliInvoke.Core.Configuration`.
- [ ] Exposes only `SetEnumerable(IEnumerable<KeyValuePair<string, string>>)`, `Build()`, `Clear()`; `SetPair`/`SetDictionary`/`SetReadOnlyDictionary` are absent.
- [ ] Two constructors - parameterless and `(StringComparer, bool throwExceptionIfDuplicateKeyFound)`.
- [ ] `Build()` returns `IReadOnlyDictionary<string, string>`; `Clear()` resets state.
- [ ] Internal state is `Dictionary<string, string>` + `StringComparer` + throw flag.

## Dependencies

**Blocked by** - None - can start immediately
