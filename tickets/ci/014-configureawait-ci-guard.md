---
title: ConfigureAwait sweep and package-free CI guard
classification: Independent
blocked_by: ["004-processwrapper-lifecycle", "005-processwrapper-truncation", "009-retry-middleware"]
parent: IMPLEMENTATION-bug-audit-fixes.md
---

## Goal

Give the library correct continuation semantics everywhere by adding `ConfigureAwait(false)` at every await site in `src`, and keep the policy enforced by a package-free CI (continuous integration) guard.

## What to build

- Add `ConfigureAwait(false)` at every await site in `src` library code. Today there are 6 usages — 5 in `src/CliInvoke/Processes/Internal/ProcessWrapper.cs` and 1 in `src/CliInvoke/Extensions/Middleware/Retry/RetryMiddleware.cs` — but tickets 004, 005, and 009 edit those files first, so re-sweep when they land (DECISIONS-CliInvoke-bug-audit-fixes.md#T013).
- Add a package-free guard that fails on bare awaits in `src`, whitelisting `await foreach` and `await using` (which cannot take `ConfigureAwait`). The guard lands in the workflow (DECISIONS-CliInvoke-bug-audit-fixes.md#T013, DECISIONS-CliInvoke-bug-audit-fixes.md#T027).
- Test projects are excluded from the guard. No analyzer package — the no-new-packages foundation stays intact (DECISIONS-CliInvoke-bug-audit-fixes.md#T001).

## Size

- **Files** - 4 (2 source edits, 1 workflow edit, 1 new guard script)

## Recommended Workflow

### Step 1 - Re-sweep src for bare awaits

Where: src/

- After tickets 004, 005, and 009 land, search all `src` projects for bare `await` expressions.

Verify: the complete list of await sites is current.

### Step 2 - Add ConfigureAwait(false) everywhere

Where: src/CliInvoke/Processes/Internal/ProcessWrapper.cs, src/CliInvoke/Extensions/Middleware/Retry/RetryMiddleware.cs, plus any other sites found

- Append `ConfigureAwait(false)` to every guardable await.

Verify: the sweep search now returns only whitelisted forms.

### Step 3 - Write the package-free guard

Where: new guard script (repository scripts location) or an inline workflow step

- Implement a text or AST-free check that fails on bare awaits in `src` and whitelists `await foreach` and `await using`. No NuGet packages.

Verify: the guard fails on a seeded bare await and passes on a clean tree.

### Step 4 - Wire the guard into CI

Where: .github/workflows/test.yml

- Add the guard step so CI fails on future bare awaits in `src`.

Verify: the workflow runs the guard successfully on a push or pull request run.

## Context pointers

##### Files

- `src/CliInvoke/Processes/Internal/ProcessWrapper.cs` - most await sites
- `src/CliInvoke/Extensions/Middleware/Retry/RetryMiddleware.cs` - one await site
- `.github/workflows/test.yml` - CI wiring

##### Ledger records

- `DECISIONS-CliInvoke-bug-audit-fixes.md#T013` - sweep plus CI guard; whitelist non-guardable awaits; test projects excluded; no analyzer package
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T001` - no new packages
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T027` - the guard lands in the workflow

## Acceptance criteria

- [ ] Every await site in `src` library code uses `ConfigureAwait(false)` (T013)
- [ ] The CI guard fails on bare awaits in `src`, whitelists `await foreach`/`await using`, and excludes test projects (T013)
- [ ] No new packages were added (T001)
- [ ] The guard was verified both ways — fails on a seeded violation, passes when clean

## Dependencies

**Blocked by** - 004-processwrapper-lifecycle, 005-processwrapper-truncation (same file: `ProcessWrapper.cs`), 009-retry-middleware (same file: `RetryMiddleware.cs`)
