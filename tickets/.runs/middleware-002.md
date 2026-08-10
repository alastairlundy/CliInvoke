# Implement-Tickets Run Report

## Run Header

| Field | Value |
|-------|-------|
| Run ID | middleware-002 |
| Mode | Self-Contained |
| Workspace | middleware (in-place) |
| Circuit Breaker | 3 strikes |
| Attribution | human+ai-coauthor |
| AI Identity | Copilot App \<223556219+Copilot@users.noreply.github.com\> |
| Start Time | 2026-08-10T17:35:17Z |
| End Time | 2026-08-10T18:10:00Z |

## Stats

| Metric | Count |
|--------|-------|
| Loaded | 2 |
| Ready | 2 |
| Skipped | 0 |
| Batches | 2 |
| Dispatch Units | 2 |
| Committed | 2 |
| Escalated | 0 |
| Conflicted | 0 |

## Per-Ticket Outcomes

| ID | Title | Status | Commit | Strikes |
|----|-------|--------|--------|---------|
| TK004 | ProcessInvoker with-middleware constructor and chain routing | committed | `a1d8d5d4` | 0 |
| TK005 | Add middleware chain semantics unit tests | committed | `2cffbda6` | 0 |

## Failures

None.

## Conflicts

None.

## Deviations

- TK004 sub-agent used `ToList()` which requires `System.Linq` — fixed by adding `using System.Linq;` (ImplicitUsings is disabled in the CliInvoke project).
- TK005 sub-agent adapted `ProcessConfigurationBuilder.ForTargetFilePath()` to `new ProcessConfigurationBuilder("test.exe")` to match actual API surface.
- TK005 sub-agent used `IsEquivalentTo` instead of `IsEqualTo` for List comparisons (TUnit reference equality).

## Next Steps

- Both tickets in this run are complete and committed.
- Remaining middleware tickets (TK006-TK009) can be implemented in a future run.
