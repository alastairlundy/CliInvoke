---
title: RC-window CI behavior
classification: Independent
blocked_by: [TK001, TK002]
parent: IMPLEMENTATION-CliInvoke_v3.2.md
---

## Goal

Configure the CI landscape for the .NET 11 RC window: work-branch Release CI that installs the .NET 11 RC SDK and validates both legs.

## What to build

On the v3.2 work branch, adjust CI workflow configuration so that:

- Work-branch Release CI installs the .NET 11 SDK during the RC window (needed to build and validate both legs).
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

### Step 2 - Verify train isolation

Where: versioning assets (csprojs, CHANGELOG.md, workflows)

- Confirm v3.1 artifacts (`3.1.0-beta.1` line on `main`) remain net10-only and untouched by this work branch.

Verify: no `main`-branch job references the v3.2 work-branch version

## Context pointers

##### Files

- `.github/workflows/test.yml` — validation matrix and gate order
- `.github/workflows/publish.yml` 
- `scripts/guard-configureawait.sh` — hard gate that must run on both legs

##### ADRs

- None directly constrain this ticket.

##### Domain terms

- None beyond general glossary usage — this ticket is CI/release-plumbing only.

##### Ledger records

- `DECISIONS-CliInvoke-dotnet11.md#D003` — post-GA merge gate; RC-window validation rules; stable band untouched; 
- `DECISIONS-CliInvoke-dotnet11.md#D004` — v3.1 isolation
- `DECISIONS-CliInvoke-dotnet11.md#T001` — SDK/CI mechanics; stable band pin moves only at the post-GA merge
- `DECISIONS-CliInvoke-dotnet11.md#D006` — guards run on both legs
- `DECISIONS-CliInvoke-dotnet11.md#T002` — RC-window net11.0 validation run must not publish

## Acceptance criteria

- [ ] Work-branch Release CI installs the .NET 11 RC SDK and validates both legs [`DECISIONS-CliInvoke-dotnet11.md#D004`]
- [ ] ConfigureAwait guard and XML-doc-as-error gates run on both legs in CI order [`DECISIONS-CliInvoke-dotnet11.md#D006`]
- [ ] v3.1 remains net10-only and unblocked, with no post-RC API-shift republish churn entering v3.1 [`DECISIONS-CliInvoke-dotnet11.md#D004`]

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** - TK001 (work branch must dual-target so CI has two legs to validate; target-agnostic ID published as `001-work-branch-dual-targeting-and-language-levels`), TK002 (dual-leg test matrix must exist for CI validation runs; published as `002-dual-leg-test-projects`)