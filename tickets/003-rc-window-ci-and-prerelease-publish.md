---
title: RC-window CI behavior and 3.2.0 prerelease publish flow
classification: Independent
blocked_by: [TK001, TK002]
parent: IMPLEMENTATION-CliInvoke_v3.2.md
---

## Goal

Configure the CI landscape for the .NET 11 RC window: work-branch Release CI that installs the .NET 11 RC SDK and validates both legs, no publish from CI on `main`, and a `3.2.0-*` prerelease-only publish flow from the work branch.

## What to build

On the v3.2 work branch, adjust CI workflow configuration so that:

- Work-branch Release CI installs the .NET 11 SDK during the RC window (needed to build and validate both legs).
- `main` keeps no publish during the RC window; only read-only CI validation jobs there may install preview/RC SDKs.
- The only permitted publish path during the RC window is the work-branch `3.2.0-*` prerelease flow (`3.2.0-alpha.1`, later beta/RC as needed). Stable version publishes remain untouched (`main`'s stable-band pin and stable-version publishes are out of scope during the RC window).
- The work-branch RC-window run covers the net11.0 test leg and is CI-validation only — nothing publishes outside the `3.2.0-*` prerelease flow.
- The ConfigureAwait guard (`scripts/guard-configureawait.sh src`) and XML-doc-as-error gates run on both legs in CI.

This ticket is the enforcement point for the isolation rules of the train: v3.1 stays net10-only and unblocked; RC-era API risk is quarantined in the v3.2 train; stable v3.x ships net10.0-only assets until .NET 11 GA.

## Size

- **Files** - 1-2 (CI workflow YAML files on the work branch)

## Recommended Workflow

### Step 1 - Provision the .NET 11 RC SDK for work-branch jobs

Where: .github/workflows (work branch, e.g. test.yml and publish.yml)

- Add .NET 11 RC SDK installation to the jobs that build/test/publish on the work branch.
- Leave `main`'s jobs on the stable 10.x SDK band except read-only validation jobs that may install preview/RC SDKs.

Verify: work-branch CI runs build both legs; `main` runs are unchanged.

### Step 2 - Wire the RC-window validation matrix

Where: .github/workflows/test.yml (work branch)

- Ensure the test suite runs on both `net10.0` and `net11.0` legs on the work branch.
- Keep the ConfigureAwait guard and XML-doc-as-error gates running on both legs ahead of the test run (replicate CI order: guard, build, test).

Verify: a work-branch run executes guard → build (both legs) → test (both legs) in order and reports discrete per-leg results.

### Step 3 - Scope the prerelease publish path

Where: .github/workflows/publish.yml (work branch)

- Restrict the RC-window publish path to `3.2.0-*` prerelease versions only.
- Ensure stable-version publishes are still gated off until the post-GA merge.
- Confirm no publish triggers from `main` during the RC window.

Verify: a dry-run/manual review shows the publish job gates on prerelease version plus work-branch ref.

### Step 4 - Verify train isolation

Where: versioning assets (csprojs, CHANGELOG.md, workflows)

- Confirm v3.1 artifacts (`3.1.0-beta.1` line on `main`) remain net10-only and untouched by this work branch.
- Confirm no stable `3.2.0` (non-prerelease) publish is possible from CI during the RC window.

Verify: no `main`-branch job references the v3.2 work-branch version; no stable publish path exists in the RC-window window.

## Context pointers

##### Files

- `.github/workflows/test.yml` — validation matrix and gate order
- `.github/workflows/publish.yml` — prerelease-only publish scoping for the RC window
- `scripts/guard-configureawait.sh` — hard gate that must run on both legs

##### ADRs

- None directly constrain this ticket.

##### Domain terms

- None beyond general glossary usage — this ticket is CI/release-plumbing only.

##### Ledger records

- `DECISIONS-CliInvoke-dotnet11.md#D003` — post-GA merge gate; RC-window validation rules; stable band untouched; no stable publish during RC window
- `DECISIONS-CliInvoke-dotnet11.md#D004` — `3.2.0-*` prerelease vehicle; Release CI needs the .NET 11 SDK during the RC window; v3.1 isolation
- `DECISIONS-CliInvoke-dotnet11.md#T001` — SDK/CI mechanics; stable band pin moves only at the post-GA merge
- `DECISIONS-CliInvoke-dotnet11.md#D006` — guards run on both legs
- `DECISIONS-CliInvoke-dotnet11.md#T002` — RC-window net11.0 validation run must not publish

## Acceptance criteria

- [ ] Work-branch Release CI installs the .NET 11 RC SDK and validates both legs [`DECISIONS-CliInvoke-dotnet11.md#D004`]
- [ ] No publish from CI on `main` during the RC window; read-only validation jobs there may install preview/RC SDKs [`DECISIONS-CliInvoke-dotnet11.md#D003`]
- [ ] The only RC-window publish path is the work-branch `3.2.0-*` prerelease flow; stable publishes are gated off until post-GA [`DECISIONS-CliInvoke-dotnet11.md#D004`]
- [ ] ConfigureAwait guard and XML-doc-as-error gates run on both legs in CI order [`DECISIONS-CliInvoke-dotnet11.md#D006`]
- [ ] v3.1 remains net10-only and unblocked, with no post-RC API-shift republish churn entering v3.1 [`DECISIONS-CliInvoke-dotnet11.md#D004`]

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** - TK001 (work branch must dual-target so CI has two legs to validate; target-agnostic ID published as `001-work-branch-dual-targeting-and-language-levels`), TK002 (dual-leg test matrix must exist for CI validation runs; published as `002-dual-leg-test-projects`)
