---
title: Create ProcessResourcePolicySpec
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CliInvoke-configuration-seam-stack.md
---

## Goal

Introduce the sealed `ProcessResourcePolicySpec` class as the single process-resource-policy configuration seam in `CliInvoke.Core`, replacing `ProcessResourcePolicyBuilder` / `IProcessResourcePolicyBuilder`.

## What to build

Create `src/CliInvoke.Core/Configuration/ProcessResourcePolicySpec.cs`. Sealed class in namespace `CliInvoke.Core.Configuration` (no interface remains, per the Interface + Impl to Sealed Impl collapse).

API surface (per `T006`, which supersedes `T003`): `SetProcessorAffinity(nint)`, `SetWorkingSet(nint minWorkingSet, nint maxWorkingSet)`, `SetPriorityClass(ProcessPriorityClass)`, `ConfigurePriorityBoost(bool)`, `Build()`. `SetMinWorkingSet` and `SetMaxWorkingSet` are dropped and replaced by the combined `SetWorkingSet`. The original method names are preserved for the unchanged methods; the `Configure` prefix is dropped from methods that did not have it.

Constructor (per `T008`): parameterless only (no niche knob survives the collapse).

Internal state (per `T011`): 5 nullable fields mirroring `ProcessResourcePolicyBuilder` at `src/CliInvoke/Builders/ProcessResourcePolicyBuilder.cs`. `SetWorkingSet` carries the min/max validation logic formerly split across `SetMinWorkingSet`/`SetMaxWorkingSet`.

Note: `SetProcessResourcePolicy(ProcessResourcePolicy)` on `IProcessConfigurationBuilder` remains the path for handing a fully configured `ProcessResourcePolicy`; the spec is the gradual `ConfigureProcessResourcePolicy(Action<ProcessResourcePolicySpec>)` path.

## Size

- **Files** - 1 (create `src/CliInvoke.Core/Configuration/ProcessResourcePolicySpec.cs`)

## Recommended Workflow

### Step 1 — Create the ProcessResourcePolicySpec class skeleton

Where: `src/CliInvoke.Core/Configuration/ProcessResourcePolicySpec.cs`

- Define `public sealed class ProcessResourcePolicySpec` in namespace `CliInvoke.Core.Configuration`.
- Add the 5 nullable internal fields - `nint? ProcessorAffinity`, `nint? MinWorkingSet`, `nint? MaxWorkingSet`, `ProcessPriorityClass PriorityClass`, `bool EnablePriorityBoost`.
- Implement the parameterless constructor with the same defaults as `ProcessResourcePolicyBuilder` (affinity from `ProcessResourcePolicy.Default`, priority `Normal`, boost `false`).

Verify: Class compiles; defaults match the former builder.

### Step 2 — Implement the setter methods and Build

Where: `src/CliInvoke.Core/Configuration/ProcessResourcePolicySpec.cs`

- Implement `SetProcessorAffinity(nint)` (range check 0x0001 to 2x processor count), `SetWorkingSet(nint min, nint max)` (combined min/max validation from the former split methods), `SetPriorityClass(ProcessPriorityClass)`, `ConfigurePriorityBoost(bool)`.
- Implement `Build()` returning a `ProcessResourcePolicy` from the 5 fields.

Verify: `SetWorkingSet` enforces the former min/max ordering constraints; `Build()` produces an equivalent `ProcessResourcePolicy`.

## Context pointers

**Files** - `src/CliInvoke/Builders/ProcessResourcePolicyBuilder.cs` (reference for behaviour and defaults, lines 15-152); `src/CliInvoke.Core/Configuration/` (target folder)
**ADRs** - None
**Domain terms** - config-seam collapse (Interface + Impl to Sealed Impl reduction)
**Ledger records** - `DECISIONS-CliInvoke-configuration-seam-stack.md#D003` (single entry-point shape), `#D006` (spec naming), `#D007` (file placement), `#T006` (API surface - supersedes `T003`), `#T008` (constructors), `#T011` (internal state)

## Acceptance criteria

- [ ] `ProcessResourcePolicySpec` is a sealed class in namespace `CliInvoke.Core.Configuration` with no interface.
- [ ] Exposes `SetProcessorAffinity(nint)`, `SetWorkingSet(nint minWorkingSet, nint maxWorkingSet)`, `SetPriorityClass(ProcessPriorityClass)`, `ConfigurePriorityBoost(bool)`, `Build()`; `SetMinWorkingSet`/`SetMaxWorkingSet` are absent.
- [ ] Parameterless constructor only.
- [ ] `Build()` returns a `ProcessResourcePolicy` equivalent to the former builder output.

## Dependencies

**Blocked by** - None - can start immediately
