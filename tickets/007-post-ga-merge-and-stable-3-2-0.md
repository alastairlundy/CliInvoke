---
title: Post-GA merge and stable 3.2.0
classification: Independent
blocked_by: [TK003, TK004, TK005, TK006]
parent: IMPLEMENTATION-CliInvoke_v3.2.md
---

## Goal

After .NET 11 GA (expected November 2026), merge the validated work branch to `main`, move the toolchain to the stable .NET 11 band, and publish stable `3.2.0` with `net10.0;net11.0` assets.

## What to build

Post-GA, on `main`:

- Merge the work branch to `main` after .NET 11 GA. The merge is the gate for everything below — nothing in this ticket happens before GA.
- Flip `global.json` to the stable `11.0.1xx` band (`rollForward: latestFeature`) and set CI to install .NET 10.x + .NET 11.x SDKs at that point.
- Stable `3.2.0` publishes with `net10.0;net11.0` assets; v3.x stables before GA remain net10.0-only.
- Contributors build with the .NET 11 SDK alone from then on — the .NET 10 SDK cannot build the net11.0 leg.
- Scope guard: nothing in this ticket touches `4.0.0-*` — the v4 train (pipes/events/streaming) resolves its TFM interaction separately and inherits this TFM shape from its first alpha, publishing net11.0 assets only post-GA.

## Size

- **Files** - 2-4 (`global.json`, CI workflow files; CHANGELOG/version bumps as release flow requires)

## Recommended Workflow

### Step 1 - Confirm the post-GA precondition

Where: repo root

- Verify .NET 11 has reached GA.
- Verify all work-branch tickets (TK001-TK006) are merged and the `3.2.0-*` prerelease train validated both legs.

Verify: GA confirmed; prerelease train validated on NuGet.

### Step 2 - Merge the work branch to main

Where: repo root

- Merge the v3.2 work branch into `main`.
- Resolve any drift accumulated on `main` during the RC window (e.g., v3.1 stabilization changes) without re-opening RC-era decisions.

Verify: merge commit on `main`; CI green on the merged state.

### Step 3 - Move the stable-band pin and CI SDKs

Where: global.json, .github/workflows

- Pin `global.json` to the stable `11.0.1xx` band (`rollForward: latestFeature`).
- Set CI to install .NET 10.x + .NET 11.x SDKs.

Verify: `dotnet --version` resolves into the 11.0.1xx band; both legs build in CI.

### Step 4 - Publish stable 3.2.0

Where: .github/workflows/publish.yml, csprojs

- Race the three csprojs' `PackageVersion` from `3.2.0-*` prerelease to stable `3.2.0` (and finalize `PackageReleaseNotes`/CHANGELOG per the release process).
- Publish stable `3.2.0` with `net10.0;net11.0` assets.

Verify: stable package on NuGet carries both-TFM assets; v3.x stables predating GA remain net10.0-only.

## Context pointers

##### Files

- `global.json` — the stable `11.0.1xx` band move (the exact change deferred from TK001)
- `.github/workflows/test.yml`, `.github/workflows/publish.yml` — dual-SDK CI and stable publish enablement
- The three src csprojs — `PackageVersion` stabilization to `3.2.0`

##### ADRs

- None directly constrain this ticket.

##### Domain terms

- None beyond general glossary usage — this ticket is release-mechanics only.

##### Ledger records

- `DECISIONS-CliInvoke-dotnet11.md#T001` — stable `11.0.1xx` band pin; CI installs both SDKs; contributors use the .NET 11 SDK alone
- `DECISIONS-CliInvoke-dotnet11.md#D003` — the net11.0 TFM work merges only after .NET 11 GA
- `DECISIONS-CliInvoke-dotnet11.md#D004` — stable `3.2.0` follows the RC-window prerelease train
- `DECISIONS-CliInvoke-dotnet11.md#D009` — pointer-only scope guard: this ticket touches nothing on `4.0.0-*`; the v4 train inherits the TFM shape but its net11.0 assets publish only post-GA on the v4 train's own schedule

## Acceptance criteria

- [ ] The work branch merges to `main` only after .NET 11 GA [`DECISIONS-CliInvoke-dotnet11.md#D003`]
- [ ] `global.json` pins the stable `11.0.1xx` band with `rollForward: latestFeature` [`DECISIONS-CliInvoke-dotnet11.md#T001`]
- [ ] CI installs .NET 10.x + .NET 11.x SDKs at merge time; contributors can build with the .NET 11 SDK alone [`DECISIONS-CliInvoke-dotnet11.md#T001`]
- [ ] Stable `3.2.0` publishes with `net10.0;net11.0` assets [`DECISIONS-CliInvoke-dotnet11.md#D004`]
- [ ] v3.x stable releases published before GA remain net10.0-only (no retroactive change) [`DECISIONS-CliInvoke-dotnet11.md#D003`]
- [ ] No `4.0.0-*` version or v4-train surface is touched by this ticket [`DECISIONS-CliInvoke-dotnet11.md#D009`]

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** - TK003 (the RC-window CI and prerelease flow it finalizes is the machinery this ticket transitions to stable publishing; published as `003-rc-window-ci-and-prerelease-publish`), TK004 (suspend/resume flow must be merged and validated; published as `004-suspend-resume-via-startsuspended`), TK005 (adapter swaps must be merged and validated; published as `005-adapter-layer-process-api-swaps`), TK006 (docs must describe the final train state before stable publish; published as `006-support-matrix-docs-and-release-notes`)
