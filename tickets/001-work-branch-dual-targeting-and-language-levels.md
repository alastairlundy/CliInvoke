---
title: Work-branch dual targeting and language levels
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-CliInvoke_v3.2.md
---

## Goal

Set up the v3.2 work branch so all three shipped packages build both the `net10.0` and `net11.0` legs with per-TFM language defaults, positioning the train at `3.2.0-alpha.1`. This is the structural prerequisite for every other v3.2 ticket.

## What to build

On the new v3.2 work branch (created from `main`; `main` itself is untouched during the RC window), update the three entrypoint packages:

- `src/CliInvoke.Core/CliInvoke.Core.csproj`, `src/CliInvoke/CliInvoke.csproj`, `src/CliInvoke.Specializations/CliInvoke.Specializations.csproj`:
  - Change `<TargetFrameworks>` from `net10.0` to `net10.0;net11.0` in all three csprojs.
  - Remove the `<LangVersion>14</LangVersion>` pins from all three csprojs — the net10.0 leg then compiles as C# 14 and the net11.0 leg as C# 15 (stable features only) via per-TFM MSBuild defaults. No preview-gated language surface anywhere in `src/` or `tests/` (`LangVersion=preview` is prohibited).
  - Race each package's `PackageVersion` from `3.1.0-beta.1` to `3.2.0-alpha.1` on the work branch (later beta/RC bumps as the train advances; per-package `PackageVersion` + Central Package Management discipline unchanged).
- `global.json` (work branch only): pin the .NET 11 RC band so both legs build locally — `main`'s stable-band pin (`10.0.100`, `rollForward: latestFeature`) is untouched during the whole RC window. The `11.0.1xx` stable-band pin happens only at the post-GA merge. Note that `rollForward` cannot cross the 10→11 major boundary — only the .NET 11 SDK can build the net11.0 leg.
- Both legs must keep `IsAotCompatible` and `IsTrimmable` passing (`PolyEnsure` behavior must hold on both legs); per-leg parity failures block the prerelease publish.

## Size

- **Files** - 4 (three src csprojs + `global.json`)

## Recommended Workflow

### Step 1 - Create the work branch

Where: repo root

- Create the v3.2 work branch from `main`.
- Confirm `main`'s `global.json` remains `10.0.100` with `rollForward: latestFeature`.

Verify: `git status` shows the work branch checked out and `global.json` on `main` untouched.

### Step 2 - Dual-target the three src csprojs

Where: `src/CliInvoke.Core/CliInvoke.Core.csproj`, `src/CliInvoke/CliInvoke.csproj`, `src/CliInvoke.Specializations/CliInvoke.Specializations.csproj`

- Change `TargetFrameworks` to `net10.0;net11.0` in each.
- Remove the `<LangVersion>14</LangVersion>` pin from each.
- Change each `PackageVersion` to `3.2.0-alpha.1`.

Verify: `dotnet build src/CliInvoke.sln` succeeds and produces `net10.0` + `net11.0` output for each package.

### Step 3 - Pin the .NET 11 RC band on the work branch

Where: `global.json` (work branch only)

- Update the work-branch `global.json` to pin the .NET 11 RC band so both legs build.
- Keep `rollForward: latestFeature` semantics; do not touch `main`'s stable-band pin during the RC window.

Verify: both legs build locally with the .NET 11 RC SDK installed.

### Step 4 - Run the CI-order gates on both legs

Where: scripts/guard-configureawait.sh, all csprojs

- Run the ConfigureAwait guard (`scripts/guard-configureawait.sh src`) — it is a hard CI gate that runs before build.
- Confirm the XML-doc-as-error gates hold on both legs (build warnings-as-errors emit on both TFMs).
- Confirm `IsAotCompatible`/`IsTrimmable` hold on both legs.

Verify: guard passes; `dotnet build` exits clean on both TFMs with AOT/trim checks satisfied.

## Context pointers

##### Files

- `src/CliInvoke.Core/CliInvoke.Core.csproj`, `src/CliInvoke/CliInvoke.csproj`, `src/CliInvoke.Specializations/CliInvoke.Specializations.csproj` — `TargetFrameworks` change, `LangVersion` pin removal, `PackageVersion` racing
- `global.json` — work-branch .NET 11 RC band pin
- `scripts/guard-configureawait.sh` — hard CI gate run before build

##### ADRs

- None directly constrain this ticket.

##### Domain terms

- Entrypoint package — the three csprojs are the entrypoint packages whose TFM surface this ticket changes
- IVT grant / Polyfill leakage (GLOSSARY.md) — per-leg source drift among the entrypoint packages ripples through internal grants; per-leg parity is guarded by CI

##### Ledger records

- `DECISIONS-CliInvoke-dotnet11.md#D002` — dual-target `net10.0;net11.0`, both legs AOT/trimmable
- `DECISIONS-CliInvoke-dotnet11.md#D006` — LangVersion pins removed; per-TFM defaults; no preview-gated surface
- `DECISIONS-CliInvoke-dotnet11.md#T001` — only the .NET 11 SDK builds both legs; stable band untouched during RC window
- `DECISIONS-CliInvoke-dotnet11.md#D004` — `3.2.0-*` prerelease vehicle; v3.1 isolation (version racing to `3.2.0-alpha.1`)
- `DECISIONS-CliInvoke-dotnet11.md#D003` — post-GA merge gate; the stable band moves only at the post-GA merge

## Acceptance criteria

- [ ] All three src csprojs carry `net10.0;net11.0` [`DECISIONS-CliInvoke-dotnet11.md#D002`]
- [ ] No `<LangVersion>` pins remain in src csprojs; net10.0 leg compiles as C# 14 and net11.0 leg as C# 15 via per-TFM defaults; no preview-gated surface [`DECISIONS-CliInvoke-dotnet11.md#D006`]
- [ ] `global.json` on the work branch pins the .NET 11 RC band; `main`'s stable-band pin is untouched during the RC window [`DECISIONS-CliInvoke-dotnet11.md#T001`]
- [ ] `IsAotCompatible`/`IsTrimmable` hold on both legs for all three packages [`DECISIONS-CliInvoke-dotnet11.md#D002`]
- [ ] `PackageVersion` on the work branch reads `3.2.0-alpha.1` on all three packages [`DECISIONS-CliInvoke-dotnet11.md#D004`]
- [ ] ConfigureAwait guard and XML-doc-as-error gates pass on both legs [`DECISIONS-CliInvoke-dotnet11.md#D006`]

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** - None - can start immediately
