---
title: Adapter-layer Process API swaps
classification: Independent
blocked_by: [TK001, TK004]
parent: IMPLEMENTATION-CliInvoke_v3.2.md
---

## Goal

On the `net11.0` leg only, adopt the bounded set of new `Process`/`SafeProcessHandle` APIs at the exact call sites where the process control adapters currently P/Invoke — keeping every mapped site behavior-identical on `net10.0` and leaving pipeline-core launch/wait plumbing untouched.

## What to build

Inside `src/CliInvoke/Processes/Internal/ProcessWrapper.cs` and `src/CliInvoke/Processes/Internal/ControlAdapters/`, all `#if NET11_0`:

**Adopt (bounded to the adapter layer / `ProcessWrapper` only):**

- `Process.TryGetProcessById` — where process lookup by identifier happens through adapters
- `Process.Signal(PosixSignal)` — replaces the `GetTerminatingSignal` read via the `Signal` property (`ProcessWrapper.cs:117`), `SendInterruptSignalAsync` (`ProcessWrapper.cs:989`), and the Unix `kill` P/Invoke (`UnixProcessControlAdapter.cs:224`)
- `ProcessExitStatus` introspection — where exit status is currently derived manually
- handle-based kill/signal/waits — replaces the Windows `GenerateConsoleCtrlEvent`/`AttachConsole` P/Invoke region (`WindowsProcessControlAdapter.cs:222–246`) and other raw-handle P/Invoke sites, confined to interrupt/suspend/resume/kill adapter paths

**Formally declined** (ledger-recorded; do not adopt and do not mirror into user-facing docs):

- `Process.RunAndCaptureTextAsync`/`Run*`, `ReadAll*`, `Process.StartAndForget`, `ProcessStartInfo.StartDetached` — they compete with self (`CliRun`/Buffered own that surface)
- `ProcessStartInfo.InheritedHandles` and Std-handle pre-spawning — default out of scope

**Constraints:**

- Pipeline-core launch/wait plumbing is NOT rewritten — adapter seam stays.
- Every mapped site stays behavior-identical on net10.0 through the existing shared-source path.
- No `LangVersion=preview` surface; both legs keep passing the XML-doc/ConfigureAwait/AOT/trim gates.

## Size

- **Files** - 3-5 (`ProcessWrapper.cs` + per-OS adapter files in ControlAdapters)
- **Large Edits required** - expected (multiple `#if NET11_0` blocks across wrapper and adapters)

## Recommended Workflow

### Step 1 - Inventory the P/Invoke sites

Where: src/CliInvoke/Processes/Internal/ProcessWrapper.cs, src/CliInvoke/Processes/Internal/ControlAdapters/

- Confirm each mapped site from this ticket against current code: `ProcessWrapper.cs:117` (Signal property), `:989` (SendInterruptSignalAsync), `WindowsProcessControlAdapter.cs:222–246` (GenerateConsoleCtrlEvent/AttachConsole), `UnixProcessControlAdapter.cs:224` (kill).
- Look for any additional raw P/Invoke in adapters that fits the adopted categories (handle-based kill/signal/waits, exit-status derivation, process-by-id lookup).

Verify: the site inventory matches the code exactly.

### Step 2 - Swap the Unix sites to Signal(PosixSignal)

Where: src/CliInvoke/Processes/Internal/ControlAdapters/UnixProcessControlAdapter.cs, src/CliInvoke/Processes/Internal/ProcessWrapper.cs

- Under `#if NET11_0`, adopt `Process.Signal(PosixSignal)` for the suspend-resume/kill/interrupt sign paths currently using `kill` P/Invoke.
- Keep the shared source path intact so net10.0 still compiles the original code.

Verify: net10.0 leg compiles and behaves as before; net11.0 leg compiles with the new API call.

### Step 3 - Swap the Windows console-control sites to handle-based APIs

Where: src/CliInvoke/Processes/Internal/ControlAdapters/WindowsProcessControlAdapter.cs

- Under `#if NET11_0`, adopt handle-based kill/signal/waits; drop `GenerateConsoleCtrlEvent`/`AttachConsole` P/Invoke on the net11.0 leg.
- Ensure behavior equivalence on net11.0 for interrupt dispatch (same signal semantics, same error paths).

Verify: net11.0 interrupt tests pass; net10.0 unchanged.

### Step 4 - Adopt TryGetProcessById and ProcessExitStatus introspection

Where: adapter/wrapper sites performing id lookup and exit-status derivation

- Swap lookup-by-id sites to `Process.TryGetProcessById` on the net11.0 leg (no exception-throwing lookup).
- Swap exit-status derivation to `ProcessExitStatus` introspection where the adapters currently compute status manually.

Verify: leg-specific unit tests cover both code paths.

### Step 5 - Verify parity and the decline list

Where: tests/CliInvoke.Tests, public API surfaces

- Run the dual-leg suite (TK002) covering interrupt/suspend/resume/kill/exit-status paths.
- Confirm no new public APIs surfaced by these swaps outside the adapter layer; the one-shot capture and detach family is absent from the codebase.
- Confirm AOT/trim checks hold on the net11.0 leg.

Verify: both legs green; public API diff empty; adopt/decline set matches this ticket exactly.

## Context pointers

##### Files

- `src/CliInvoke/Processes/Internal/ProcessWrapper.cs` — Signal/interrupt/interrupt-signal call sites
- `src/CliInvoke/Processes/Internal/ControlAdapters/WindowsProcessControlAdapter.cs` — console-control P/Invoke region
- `src/CliInvoke/Processes/Internal/ControlAdapters/UnixProcessControlAdapter.cs` — `kill` P/Invoke site
- `DESIGN_PATTERNS.md` — confirms `CliRun`/Buffered ownership of the capture surface the declined set competes with

##### ADRs

- None directly constrain this ticket.

##### Domain terms

- Result Model Signal — the POSIX signal that terminated a process; surfaced on Unix only; absent on Windows
- Try* catch discipline (GLOSSARY.md) — `TryGetProcessById` on net11.0 aligns with the repo's `Try*` never-propagate convention

##### Ledger records

- `DECISIONS-CliInvoke-dotnet11.md#T003` — exact adopted set, behavior-identical net10.0 parity, handle-based swaps confined to interrupt/suspend/resume/kill paths, decline list
- `DECISIONS-CliInvoke-dotnet11.md#D007` — bounded per-pain-point adoption; no pipeline-core rewrite; no blanket decline
- `DECISIONS-CliInvoke-dotnet11.md#D008` — suspend/resume API mapping inside adapters (the flow implemented in TK004)
- `DECISIONS-CliInvoke-dotnet11.md#T002` — per-leg verification rides the dual-leg test runs

## Acceptance criteria

- [ ] net11.0 leg uses `Process.Signal(PosixSignal)` at the current Unix `kill` P/Invoke site and `SendInterruptSignalAsync` [`DECISIONS-CliInvoke-dotnet11.md#T003`]
- [ ] net11.0 leg uses handle-based kill/signal/waits in the Windows adapter instead of `GenerateConsoleCtrlEvent`/`AttachConsole` P/Invoke [`DECISIONS-CliInvoke-dotnet11.md#T003`]
- [ ] net11.0 leg adopts `Process.TryGetProcessById` and `ProcessExitStatus` introspection at the mapped sites [`DECISIONS-CliInvoke-dotnet11.md#T003`]
- [ ] Every mapped site is behavior-identical on net10.0 through the shared-source path [`DECISIONS-CliInvoke-dotnet11.md#T003`]
- [ ] Pipeline-core launch/wait plumbing is not rewritten [`DECISIONS-CliInvoke-dotnet11.md#D007`]
- [ ] The one-shot capture and detach family (`RunAndCaptureTextAsync`/`Run*`, `ReadAll*`, `StartAndForget`, `StartDetached`) and `InheritedHandles`/Std-handle pre-spawning are not adopted anywhere [`DECISIONS-CliInvoke-dotnet11.md#T003`]
- [ ] No user-facing adopt/decline documentation is added (the record stays ledger-only per `DECISIONS-CliInvoke-dotnet11.md#T004`)

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** - TK001 (net11.0 compile surface must exist), TK004 (same-file rule: both tickets modify `src/CliInvoke/Processes/Internal/ProcessWrapper.cs`; TK004 is listed first in the proposal, so its suspend/resume restructuring lands before these adapter swaps; published as `004-suspend-resume-via-startsuspended`)
