---
title: Change 15-param ProcessConfiguration ctor visibility from protected to internal
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-config-construction.md
---

## Goal

Enable direct `ProcessConfiguration` construction from `CliInvoke` (Builder, thinned Factory) via the existing `InternalsVisibleTo("CliInvoke")` grant, while keeping the 15-parameter ctor out of the public API surface. This is the foundation change that makes TK002 and TK003 possible.

## What to build

In `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`, change the 15-parameter constructor at lines 54–69 from `protected` to `internal`. The public 3-parameter ctor's `: this(...)` delegation at line 31 is unaffected because it is a same-class call (no cross-assembly visibility rule applies). No other visibility or signature changes are made to the file.

The change replaces the cross-assembly access path that the `BuilderProcessConfiguration` bridge subclass (TK002) currently provides with a direct cross-assembly call that is permitted by the existing `InternalsVisibleTo("CliInvoke")` declaration in `src/CliInvoke.Core/CliInvoke.Core.csproj:57–58`. No new `InternalsVisibleTo` grants are added.

## Size

- Files: 1

## Recommended Workflow

### Step 1 — Change ctor visibility modifier

Where: `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`

- Edit line 54: change `protected ProcessConfiguration(` to `internal ProcessConfiguration(`.
- Leave lines 55–69 (the parameter list and body) unchanged.
- Leave line 29 (the public 3-parameter ctor) and its `: this(...)` delegation at line 31 unchanged — same-class call, not affected by the modifier change.
- Verify: A grep for `protected ProcessConfiguration(` in `src/CliInvoke.Core/` returns no matches; a grep for `internal ProcessConfiguration(` matches exactly the 15-parameter ctor at lines 54–69.

### Step 2 — Build Core and confirm the public surface is unchanged

Where: `src/CliInvoke.Core/CliInvoke.Core.csproj`

- Run `dotnet build src/CliInvoke.Core/CliInvoke.Core.csproj`.
- Verify: Build clean; the public API surface (3-param ctor at line 29, public properties at lines 102+, public methods at lines 197+, 209+, 238+, 251+, 279+, 292+, 306+, 318+) is unchanged. The 15-param ctor is no longer visible to external consumers.

## Context pointers

- Files:
  - `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs` — target of the modifier change (line 54)
  - `src/CliInvoke.Core/CliInvoke.Core.csproj:57–58` — existing `InternalsVisibleTo` grants to `CliInvoke` and `CliInvoke.Tests`; this ticket relies on the `CliInvoke` grant
- Domain terms:
  - "Resource-Owning Type" (from `GLOSSARY.md`) — `ProcessConfiguration` is a resource-owning type; this change does not affect its `IDisposable` contract at lines 197–202; do not reproduce the glossary entry
- Ledger records:
  - `DECISIONS-CliInvoke-config-construction.md#T004` — 15-param ctor visibility = `internal`; bridge subclass deleted; existing `InternalsVisibleTo` is the access path
  - `DECISIONS-CliInvoke-config-construction.md#I001` — clarification: public full ctor was rejected because the 15 parameters are too unwieldly for ordinary users; the internal ctor via existing `InternalsVisibleTo` keeps it out of the public API

## Acceptance criteria

- [ ] The 15-parameter `ProcessConfiguration` constructor (currently at lines 54–69) is declared `internal`, not `protected` or `public` (per `DECISIONS-CliInvoke-config-construction.md#T004`).
- [ ] The public 3-parameter ctor (line 29) and its `: this(...)` delegation (line 31) still compile unchanged (per `DECISIONS-CliInvoke-config-construction.md#T004`).
- [ ] No other visibility or signature changes are made to `ProcessConfiguration.cs` in this ticket (per `DECISIONS-CliInvoke-config-construction.md#T004`).
- [ ] No new `InternalsVisibleTo` declarations are added in `src/CliInvoke.Core/CliInvoke.Core.csproj`; the existing Core → `CliInvoke` and Core → `CliInvoke.Tests` grants suffice (per `DECISIONS-CliInvoke-config-construction.md#T011`).
- [ ] `dotnet build src/CliInvoke.Core/CliInvoke.Core.csproj` succeeds with the change.
- [ ] `dotnet build src/CliInvoke.sln` succeeds (no consumer depends on the previous `protected` visibility across assemblies in the same solution; verify the assumption by building the full solution).

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** — None (can start immediately; the foundation change)
