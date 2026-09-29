---
title: Dual-leg test projects
classification: Independent
blocked_by: [TK001]
parent: IMPLEMENTATION-CliInvoke_v3.2.md
---

## Goal

Make the RC-window test run cover the net11.0 leg by dual-targeting the test suite on the work branch, preserving existing OS-dependent skip conventions on both legs. Build-only parity of TK001 is not enough — the `#T003` per-leg parity promise needs runtime evidence.

## What to build

On the v3.2 work branch:

- Change the test project targeting as prescribed by the blueprint: `tests/CliInvoke.Tests` dual-targets `net10.0;net11.0` on the work branch (expand the set to other test projects only where the blueprint requires it — the blueprint names `tests/CliInvoke.Tests` as the minimum).
- The RC-window run covers the net11.0 leg and is CI-validation only — it must not publish beyond the permitted `3.2.0-*` prerelease flow.
- OS-dependent behaviors (spawning real executables: `dotnet`, `powershell.exe`, `pwsh`; PowerShell middleware tests skip when `pwsh` is not on PATH) follow the existing skip conventions on both legs.
- Keep the TUnit-on-Microsoft.Testing.Platform setup (`UseTestingPlatformRunner=true`; test projects are `Exe`) unchanged — only the targeting matrix changes.

## Size

- **Files** - 1

## Recommended Workflow

### Step 1 - Dual-target the test project

Where: tests/CliInvoke.Tests/CliInvoke.Tests.csproj

- Change `TargetFramework` (single) to `TargetFrameworks` (plural) with value `net10.0;net11.0`.
- Confirm Central Package Management `PackageVersion` entries in `tests/Directory.Packages.props` still resolve for both TFMs.

Verify: `dotnet build tests/CliInvoke.Tests` succeeds for both TFMs after TK001 has landed.

### Step 2 - Audit OS-dependent skips on the net11.0 leg

Where: tests/CliInvoke.Tests test files

- Grep for `powershell.exe`/`pwsh`-on-PATH skip guards and executable-availability checks.
- Confirm each guard is TFM-agnostic (operates on runtime discovery, not on `#if NET10_0`/`NET11_0` branches).

Verify: the same skip conditions apply on both legs; no test asserts a leg-specific assumption.

### Step 3 - Run the suite on both legs locally

Where: tests/CliInvoke.Tests

- Run the test suite targeting `net10.0` and `net11.0` with the .NET 11 RC SDK installed.
- Confirm OS-dependent skips behave identically on both legs.

Verify: both runs are green (with expected skips) locally.

## Context pointers

##### Files

- `tests/CliInvoke.Tests/CliInvoke.Tests.csproj` — the targeting change; TUnit on Microsoft.Testing.Platform stays as-is
- `tests/Directory.Packages.props` — Central Package Management versions for test dependencies
- `tests/CliInvoke.Tests/` test files — OS-dependent skip conventions (`powershell.exe`/`pwsh` on PATH)

##### ADRs

- None directly constrain this ticket.

##### Domain terms

- None beyond general glossary usage — this ticket is test-infrastructure only.

##### Ledger records

- `DECISIONS-CliInvoke-dotnet11.md#T002` — dual-target test projects; RC-window net11.0 run is CI-validation only and must not publish
- `DECISIONS-CliInvoke-dotnet11.md#D003` — RC window used only for validation-level confidence; publish constraints per D004
- `DECISIONS-CliInvoke-dotnet11.md#T003` — per-leg behavior-identical parity on net10.0, verified via these dual-leg runs

## Acceptance criteria

- [ ] `tests/CliInvoke.Tests` dual-targets `net10.0;net11.0` on the work branch [`DECISIONS-CliInvoke-dotnet11.md#T002`]
- [ ] The net11.0-leg run is CI-validation only and publishes nothing beyond the `3.2.0-*` prerelease flow [`DECISIONS-CliInvoke-dotnet11.md#T002`]
- [ ] OS-dependent skip conventions hold on both legs with no leg-specific test assumptions [`DECISIONS-CliInvoke-dotnet11.md#T002`]
- [ ] TUnit on Microsoft.Testing.Platform setup remains functional on both legs (no runner-config drift) [`DECISIONS-CliInvoke-dotnet11.md#T002`]

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** - TK001 (the net11.0 leg of the src packages must exist and build before the test project can compile against it — target-agnostic ID substituted to `001-work-branch-dual-targeting-and-language-levels` at publish time)
