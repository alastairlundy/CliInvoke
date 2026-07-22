---
title: Switch default `_externalProcessFactory` to per-call construction honouring `UseFilePathResolver`
classification: Independent
blocked_by: [001-build-string-args-config-helper, 002-clirun-funnel-method]
parent: docs/decisions/DECISIONS-CliInvoke-clirun-shape.md
---

## Goal

Change the `_externalProcessFactory` field initializer in `src/CliInvoke/Extensions/CliRun.cs` from `() => new ExternalProcessFactory()` to `() => new ExternalProcessFactory(GetFilePathResolver())` so the default factory is constructed per call with the current static resolver, honouring any custom resolver set via `UseFilePathResolver`. Also update the D001 record wording in the ledger per D008 to distinguish binary or API signatures from behaviour.

## What to build

In `src/CliInvoke/Extensions/CliRun.cs`, change the field initializer at lines 23-24 from:

```csharp
private static Func<IExternalProcessFactory> _externalProcessFactory = () => new
    ExternalProcessFactory();
```

to:

```csharp
// T005: per-call allocation is intentional to honor UseFilePathResolver; do not cache without invalidation.
private static Func<IExternalProcessFactory> _externalProcessFactory = () => new
    ExternalProcessFactory(GetFilePathResolver());
```

The custom-factory path is unaffected: `UseExternalProcessFactory(customFactory)` continues to override the delegate and returns the same custom factory on every call. `GetFilePathResolver()` already caches the resolver via double-check locking at lines 59-70, so the resolver itself is not re-allocated per call — only the `ExternalProcessFactory` and its captured resolver reference are. The factory setter remains unlocked (D008 asymmetry preserved); the resolver setter remains locked (T014 preserved).

In `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md`, apply the D008 re-wording to the D001 record: replace D001's "The public surface (`UseExternalProcessFactory`, `UseFilePathResolver`, the 6 `Run*Async` methods) is unchanged" with "The public API surface is binary-compatible: signatures are unchanged, no types are added or removed. Behaviour is changed per T005 (default factory construction is per-call, not lazy) and T006 (helper is pure, resolution happens at the factory level); see those records for the behaviour deltas." The D001 record itself stays in the ledger for traceability; the new wording is the D008 record's normalized requirement and is what the D001 record will read after this edit.

## Size

- **Files** - 2

## Recommended Workflow

### Step 1 — Update the field initializer and add the T005 code-review note

Where: `src/CliInvoke/Extensions/CliRun.cs`

- Replace the field initializer at lines 23-24 with the `() => new ExternalProcessFactory(GetFilePathResolver())` form.
- Add the `// T005: per-call allocation is intentional to honor UseFilePathResolver; do not cache without invalidation.` comment immediately above the field declaration.

Verify: The file still compiles; the field's type and visibility are unchanged; the custom-factory setter at lines 36-39 is unaffected.

### Step 2 — Apply the D008 re-wording to the D001 record in the ledger

Where: `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md`

- Locate the D001 record and replace the bullet "The public surface (`UseExternalProcessFactory`, `UseFilePathResolver`, the 6 `Run*Async` methods) is unchanged." with the D008 wording: "The public API surface is binary-compatible: signatures are unchanged, no types are added or removed. Behaviour is changed per T005 (default factory construction is per-call, not lazy) and T006 (helper is pure, resolution happens at the factory level); see those records for the behaviour deltas."
- The D001 record and the D008 record both stay in the ledger; the D008 record's `Supersedes: D001` line remains authoritative for the change history.

Verify: The ledger file still parses as Markdown; the D001 and D008 record headings are intact; the bullet text under D001 matches the D008 wording verbatim.

### Step 3 — Build the full solution and run existing tests

Where: repository root

- Run `dotnet build src/CliInvoke.sln` to confirm zero errors and zero new warnings on net8.0, net9.0, and net10.0.
- Run `dotnet test tests/CliInvoke.Tests/CliInvoke.Tests.csproj` from `tests/CliInvoke.Tests/` to confirm no regressions in existing tests.

Verify: Build succeeds; existing tests pass; the default factory's `CreateExternalProcess` is constructed per `Run*Async` call when no custom factory is registered (a temporary debug assertion can confirm the per-call allocation).

## Context pointers

**Files**
- `src/CliInvoke/Extensions/CliRun.cs` — the production file edited by this ticket; the field initializer and the T005 code-review note are added.
- `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md` — the ledger file edited by this ticket; the D001 record wording is replaced per D008.
- `src/CliInvoke/Factories/ExternalProcessFactory.cs` — the default factory implementation; not edited, but its parameterless ctor at lines 30-33 (which allocates its own `FilePathResolver`) is the asymmetry this ticket resolves for default users by routing through `GetFilePathResolver()` instead.

**Domain terms**
- Resource-Owning Type (from `CONTEXT.md`) — `FilePathResolver` and the resolver it owns are not Resource-Owning Types, but the per-call `ExternalProcessFactory` allocation captures a resolver reference; the resolver itself is cached via `GetFilePathResolver()`'s double-check locking.

**Ledger records**
- `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#D001` — disposition of the dead `_externalProcessFactory` field; the field is honoured as the live configuration surface.
- `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#D008` — re-word D001 to distinguish binary or API signatures from behaviour; this ticket applies the D008 wording to the D001 record.
- `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#T005` — default delegate is `() => new ExternalProcessFactory(GetFilePathResolver())`; per-call allocation is intentional.
- Cross-cite (superseded): `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#T002` (Lazy caching) — superseded by T005; covered by this ticket via the active successor.

## Acceptance criteria

- [ ] `_externalProcessFactory` is initialized to `() => new ExternalProcessFactory(GetFilePathResolver())` per `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#T005`.
- [ ] A code-review note at the field documents the T005 rationale.
- [ ] The custom-factory path via `UseExternalProcessFactory(customFactory)` is unaffected; the same custom factory instance is returned on every call after the setter is invoked.
- [ ] The `GetFilePathResolver()` double-check locking at lines 59-70 is preserved.
- [ ] The factory setter remains unlocked and the resolver setter remains locked (D008 asymmetry preserved).
- [ ] The D001 record in `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md` is re-worded per D008 to acknowledge the T005 and T006 behaviour changes.
- [ ] The public API surface is binary-compatible per D008: signatures unchanged, no types added or removed.
- [ ] `dotnet build src/CliInvoke.sln` succeeds on net8.0, net9.0, and net10.0.
- [ ] Existing tests under `tests/CliInvoke.Tests/` continue to pass.

## Dependencies

**Blocked by** - 001-build-string-args-config-helper, 002-clirun-funnel-method
