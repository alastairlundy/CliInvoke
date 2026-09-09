---
title: Three v2 simulation sample projects
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-v3-stable-readiness.md
---

# Ticket — Three v2 simulation sample projects

## Goal

Commit three small v2 sample projects under `simulation/v2-samples/`, one per canonical invocation pattern, each pinned to CliInvoke 2.x and each exercising that pattern's construction migration. These are the inputs the agent-driven simulation upgrades using only the migration guide, providing per-pattern evidence for readiness gate criterion 2.

## What to build

Create the directory `simulation/v2-samples/` at the repo root, excluded from `CliInvoke.sln`, containing three small console projects:

1. `clirun-sample/` - static facade usage including configuration construction as a v2 user would write it (v2 construction style: `ProcessConfigurationFactory` / builder habit, `ArgumentsList`, non-required `TargetFilePath`).
2. `iprocessinvoker-sample/` - invoker usage including dependency-injection setup and middleware registration as v2 code.
3. `iexternalprocess-sample/` - direct lifecycle control including v2-era `ExitConfiguration` usage.

Each project:

- References the CliInvoke 2.x package, pinned to an exact 2.x version.
- Compiles against v2 as committed.
- Produces observable output (stdout text and a non-zero or zero exit path) so a later upgrade can behavior-check its output, not just its compilation.

The samples are committed in-repo for gate repeatability. Their upgrade (to v3, using only the guide) happens during the readiness gate run, not in this ticket.

## Size

- **Files** - 6 to 9 files to create (three csproj files plus Program.cs and optional solution-exclusion notes)

## Recommended Workflow

### Step 1 - Create the directory and project skeletons

Where: `simulation/v2-samples/clirun-sample/`, `simulation/v2-samples/iprocessinvoker-sample/`, `simulation/v2-samples/iexternalprocess-sample/`

- Create the three directories and minimal console project files
- Confirm the directory is not added to `CliInvoke.sln`

Verify: `dotnet build` on each project succeeds in isolation

### Step 2 - Pin the v2 package reference

Where: the three csproj files

- Add an exact-version CliInvoke 2.x package reference to each project

Verify: restore resolves 2.x, not a floating or 3.x version

### Step 3 - Write the v2-pattern sample code

Where: the three Program.cs files

- Write idiomatic v2 code per pattern - static facade calls with v2 construction in clirun-sample; invoker plus DI and middleware in iprocessinvoker-sample; direct lifecycle control with v2 `ExitConfiguration` usage in iexternalprocess-sample
- Each sample prints deterministic output so behavior can be checked after upgrade

Verify: all three run successfully against the 2.x package and their output is deterministic

### Step 4 - Record baseline output

Where: a notes file or commit message per sample

- Capture each sample's v2 output as the behavior baseline the gate run compares against

Verify: the baseline output is recorded somewhere committed or reproducible

## Context pointers

##### Files

- `CliInvoke.sln` - the samples must NOT be added here (explicit exclusion)
- `site/docs/guides/choosing-invocation-pattern.md` - the canonical three-pattern partition the samples follow
- `IMPLEMENTATION-v3-stable-readiness.md` (Part 5, criterion 2 of Part 0) - the blueprint section defining the samples

##### ADRs

- None directly relevant

##### Domain terms

- None required beyond the blueprint's pattern names (`CliRun`, `IProcessInvoker`, `IExternalProcess`)

##### Ledger records

- `DECISIONS-CliInvoke-v3-stable-readiness.md#T006` - three pattern samples, one per canonical invocation pattern, independently upgraded
- `DECISIONS-CliInvoke-v3-stable-readiness.md#T007` - the three-pattern taxonomy; construction migration is orthogonal and exercised within each sample
- `DECISIONS-CliInvoke-v3-stable-readiness.md#D008` - this is stream 2 (simulation) of the evidence streams
- `DECISIONS-CliInvoke-v3-stable-readiness.md#T002` - success per sample feeds gate criterion 2

## Acceptance criteria

- [ ] `simulation/v2-samples/` exists with exactly three sample projects, one per canonical invocation pattern
- [ ] Each project is pinned to an exact CliInvoke 2.x version and compiles as committed
- [ ] Each sample exercises its pattern's v2 construction style (v2 configuration construction, v2 `ExitConfiguration` usage where relevant)
- [ ] The directory is excluded from `CliInvoke.sln`
- [ ] Each sample produces deterministic output with a recorded baseline for post-upgrade behavior comparison

## Dependencies

**Blocked by** - None - can start immediately
