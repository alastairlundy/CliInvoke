---
title: Document working-directory inherit semantics and correct the normalization claim
classification: Independent
blocked_by: [003-encoding-default-documentation.md]
parent: IMPLEMENTATION-handoff-process-flaws-doc.md
---

# TK004 - Document working-directory inherit semantics and correct the normalization claim

## Goal

Make the empty-`WorkingDirectoryPath` behavior explicit and consistent with what the code actually does - the child inherits the caller's current directory - and correct the site doc's framing so it no longer implies CliInvoke normalizes the value.

## What to build

Documentation-only change (no normalization code ships):

- Document that an empty `WorkingDirectoryPath` means the child process inherits the caller's current directory, and that this behavior is consistent on Windows and Unix.
- Correct the corresponding claim in `site/docs/everything-wrong-with-process-in-csharp.md` (lines 244-245), which currently frames the semantics as depending on `UseShellExecute` in a way that can be read as CliInvoke normalizing or branching on the value. Keep any raw `System.Diagnostics.Process` caveat that is factually about .NET itself, but make CliInvoke's own behavior unmistakable.
- Cross-check `site/docs/guides/configuration.md` (the `WorkingDirectoryPath` rows around lines 55 and 576 and the builder comparison table) so the documented default and validation behavior match the code - the `Directory.Exists` validation on non-empty values is unchanged.

Normalization at adapter or construction time is explicitly out of scope; this ticket documents, it does not change behavior.

## Size

- Files - 2 (2 edits - `site/docs/everything-wrong-with-process-in-csharp.md`, `site/docs/guides/configuration.md`)

## Recommended Workflow

### Step 1 - Read the current claims

Where: `site/docs/everything-wrong-with-process-in-csharp.md`, `site/docs/guides/configuration.md`

- Read lines 244-245 of the everything-wrong article and the `WorkingDirectoryPath` coverage in the configuration guide.

Verify: You can quote what the docs currently imply about normalization and `UseShellExecute`.

### Step 2 - Verify the actual behavior in code

Where: `src/CliInvoke/Processes/Internal/ControlAdapters/BaseProcessControlAdapter.cs`, `src/CliInvoke.Core/`

- Confirm the adapter passes `WorkingDirectoryPath` straight to `ProcessStartInfo.WorkingDirectory` with no normalization.
- Confirm the `Directory.Exists` validation applies only to non-empty values.

Verify: Every statement you plan to write traces to a code statement.

### Step 3 - Rewrite the doc claims

Where: `site/docs/everything-wrong-with-process-in-csharp.md`, `site/docs/guides/configuration.md`

- State the inherit semantics for the empty case and its cross-runtime consistency.
- Remove any implication that CliInvoke normalizes the value; keep the raw .NET caveat only where it is genuinely about .NET.

Verify: The corrected section reads unambiguously to someone who has not read the code.

## Context pointers

### Files

- `site/docs/everything-wrong-with-process-in-csharp.md` - the claim to correct (lines 244-245)
- `site/docs/guides/configuration.md` - `WorkingDirectoryPath` rows to cross-check (lines 55, 576)
- `src/CliInvoke/Processes/Internal/ControlAdapters/BaseProcessControlAdapter.cs` - where `WorkingDirectory` is applied
- `src/CliInvoke.Core/` - where `WorkingDirectoryPath` validation lives

### ADRs

- None constraining

### Domain terms

- None - no glossary terms materially bind this ticket

### Ledger records

- `DECISIONS-CliInvoke-process-flaw-mitigations.md#D001` - session scope covering the five mitigation items
- `DECISIONS-CliInvoke-process-flaw-mitigations.md#T003` - inherit semantics documented rather than normalization shipped; no normalization at adapter or construction time; `Directory.Exists` validation unchanged

## Acceptance criteria

- [ ] The docs state that empty `WorkingDirectoryPath` means the child inherits the caller's current directory, consistent cross-runtime
- [ ] The everything-wrong article no longer implies CliInvoke normalizes the value
- [ ] The configuration guide's `WorkingDirectoryPath` statements match the code's actual default and validation behavior
- [ ] No code changes - the `Directory.Exists` validation on non-empty values is untouched

## Dependencies

**Blocked by** - [003-encoding-default-documentation.md](003-encoding-default-documentation.md) - both tickets edit `site/docs/` surfaces and may touch `site/docs/guides/configuration.md`; sequencing the doc tickets avoids merge churn. (Automatic same-file rule - override if you want them parallel.)
