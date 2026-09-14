---
title: Document kill-tree best-effort semantics and the descendant race
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-handoff-process-flaws-doc.md
---

# TK005 - Document kill-tree best-effort semantics and the descendant race

## Goal

Set accurate expectations for forceful exit: tree-kill is best-effort, matching .NET's own `Kill(entireProcessTree: true)` semantics, and descendants spawned while the tree is being killed may survive.

## What to build

Documentation-only change (no added latency, no descendant re-kill):

- Document that forceful exit (timeout and requested-cancellation paths) uses `Kill(entireProcessTree: true)` and is best-effort: descendants spawned while the tree is being killed may survive the kill.
- State explicitly that no post-kill delay or descendant re-kill ships, so the documented behavior matches the implementation exactly - this mirrors .NET's own documented semantics for `Kill(entireProcessTree: true)`.
- Locate the right doc surfaces in `site/docs/` (candidates - the architecture guide's cancellation/termination coverage, the troubleshooting guide, and the everything-wrong article if it discusses kill behavior) and place the statement where readers of forceful-exit behavior would actually look.

The graceful-interrupt path is out of scope for this ticket. Do not add a delay, a second pass, or any behavior change.

## Size

- Files - 1 to 2 (1-2 edits in `site/docs/`)

## Recommended Workflow

### Step 1 - Verify the actual kill behavior in code

Where: `src/CliInvoke/Processes/Internal/ProcessWrapper.cs`, `src/CliInvoke/Processes/Internal/ControlAdapters/`

- Confirm the forceful-exit path calls `Kill(entireProcessTree: true)` with no post-kill delay and no descendant re-kill.

Verify: Every statement you plan to write traces to a code statement.

### Step 2 - Locate the doc surfaces

Where: `site/docs/`

- Find where forceful exit, timeout, and cancellation termination are currently described.

Verify: You know where a reader would look for kill semantics.

### Step 3 - Write the best-effort statement

Where: the located `site/docs/` files

- Add the best-effort framing, the mid-kill descendant race, and the no-delay/no-re-kill statement.

Verify: A reader can predict what survives a forceful exit without reading the code.

## Context pointers

### Files

- `src/CliInvoke/Processes/Internal/ProcessWrapper.cs` - cancellation and kill machinery to fact-check against
- `site/docs/guides/architecture.md` - candidate surface for termination semantics
- `site/docs/guides/troubleshooting.md` - candidate surface for "my descendant survived" expectations

### ADRs

- None constraining

### Domain terms

- Canceled (GLOSSARY.md) - the result-model state for library-terminated processes; use it consistently when describing the timeout/cancellation paths

### Ledger records

- `DECISIONS-CliInvoke-process-flaw-mitigations.md#D001` - session scope covering the five mitigation items
- `DECISIONS-CliInvoke-process-flaw-mitigations.md#T005` - tree-kill stays best-effort, no post-kill delay or descendant re-kill, residual race documented, matching .NET's `Kill(entireProcessTree: true)` semantics

## Acceptance criteria

- [ ] The docs state forceful exit uses `Kill(entireProcessTree: true)` and is best-effort
- [ ] The docs describe the residual race - descendants spawned mid-kill may survive
- [ ] The docs state no post-kill delay or descendant re-kill ships
- [ ] Every documented statement matches the code's actual behavior
- [ ] No behavior change - no delay, no re-kill, graceful-interrupt path untouched

## Dependencies

**Blocked by** - None - can start immediately
