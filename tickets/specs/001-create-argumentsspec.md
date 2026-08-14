---
title: Create ArgumentsSpec
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CliInvoke-configuration-seam-stack.md
---

## Goal

Introduce the sealed `ArgumentsSpec` class as the single arguments configuration seam in `CliInvoke.Core`, replacing `ArgumentsBuilder` / `IArgumentsBuilder` per the config-seam collapse.

## What to build

Create `src/CliInvoke.Core/Configuration/ArgumentsSpec.cs`. Sealed class in namespace `CliInvoke.Core.Configuration`. It carries the `ArgumentValidationLogic` capability forward (mirroring `ArgumentsBuilder`'s dual-constructor pattern) and holds the same internal state (`StringBuilder` + validation logic) so the migration stays mechanical.

API surface (per `T001`): `Add(string, bool)`, `Add(IFormattable, bool)`, `AddEnumerable(IEnumerable<string>, bool)`, `AddEnumerable(IEnumerable<IFormattable>, bool)`. `EscapeCharacters` is `private` (implementation detail, not public API).

Constructors (per `D010`): parameterless `ArgumentsSpec()` and `ArgumentsSpec(Func<string, bool> argumentValidationLogic)`. The default case behaves as a null check via `ArgumentNullException.ThrowIfNull`, matching `ArgumentsBuilder.IsValidArgument` at `src/CliInvoke/Builders/ArgumentsBuilder.cs:296-318`.

Reconciliation note: `T010`/`T011` require the parent builder to extract via `spec.Build()` uniformly, so `ArgumentsSpec` also exposes `Build()` returning the accumulated arguments string (the `ToString()` equivalent from `ArgumentsBuilder`). This supplements `T001`'s `Add*` enumeration and is required by the seam.

## Size

- **Files** - 1 (create `src/CliInvoke.Core/Configuration/ArgumentsSpec.cs`)

## Recommended Workflow

### Step 1 — Create the ArgumentsSpec class skeleton

Where: `src/CliInvoke.Core/Configuration/ArgumentsSpec.cs`

- Define `public sealed class ArgumentsSpec` in namespace `CliInvoke.Core.Configuration`.
- Add `private readonly StringBuilder _buffer` and `private readonly Func<string, bool> _argumentValidationLogic` fields.
- Implement the parameterless constructor (default validation = null check) and the `(Func<string, bool> argumentValidationLogic)` constructor.

Verify: Class compiles; both constructors present and the default validation rejects null via `ArgumentNullException`.

### Step 2 — Implement the Add and AddEnumerable methods

Where: `src/CliInvoke.Core/Configuration/ArgumentsSpec.cs`

- Implement `Add(string, bool)`, `Add(IFormattable, bool)`, `AddEnumerable(IEnumerable<string>, bool)`, `AddEnumerable(IEnumerable<IFormattable>, bool)` mirroring `ArgumentsBuilder` semantics (space joining between arguments, validation via `_argumentValidationLogic`, escaping through a `private` `EscapeCharactersWithoutWrapping` helper).
- Keep `EscapeCharacters` as a `private` method only.

Verify: Each method appends to `_buffer`, honours the validation logic, and escapes content without double-wrapping.

### Step 3 — Implement Build

Where: `src/CliInvoke.Core/Configuration/ArgumentsSpec.cs`

- Add `Build()` returning `_buffer.ToString()`.

Verify: `Build()` returns the accumulated arguments string.

## Context pointers

**Files** - `src/CliInvoke/Builders/ArgumentsBuilder.cs` (reference for behaviour, lines 22-295, validation at 296-318); `src/CliInvoke.Core/Configuration/` (target folder)
**ADRs** - None
**Domain terms** - config-seam collapse (the "Interface + Impl to Sealed Impl" reduction that removes the four sub-builder interfaces)
**Ledger records** - `DECISIONS-CliInvoke-configuration-seam-stack.md#D003` (single entry-point shape), `#D006` (spec naming), `#D007` (file placement), `#T001` (API surface), `#D010` (validation-logic reuse), `#T011` (internal state), `#T010` (parent extracts via `spec.Build()`)

## Acceptance criteria

- [ ] `ArgumentsSpec` is a sealed class in namespace `CliInvoke.Core.Configuration`.
- [ ] Exposes only `Add(string, bool)`, `Add(IFormattable, bool)`, `AddEnumerable(IEnumerable<string>, bool)`, `AddEnumerable(IEnumerable<IFormattable>, bool)`; `EscapeCharacters` is `private`.
- [ ] Two constructors present - parameterless (null-check default) and `(Func<string, bool>)`; default behaves as a null check per `ArgumentsBuilder.IsValidArgument`.
- [ ] `Build()` returns the accumulated arguments string.
- [ ] Internal state is `StringBuilder` + validation logic.

## Dependencies

**Blocked by** - None - can start immediately
