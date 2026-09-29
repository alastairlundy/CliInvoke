---
title: Suspend-resume via StartSuspended in ProcessWrapper
classification: Independent
blocked_by: [TK001]
parent: IMPLEMENTATION-CliInvoke_v3.2.md
---

## Goal

On the `net11.0` leg only, replace the suspend → apply → always-resume cycle in `ProcessWrapper` with the .NET 11 `ProcessStartInfo.StartSuspended` + `SafeProcessHandle.Resume()` flow on Windows and macOS, so `ProcessResourcePolicy` applies through the process control adapter before the process can run — without exposing any new public API.

## What to build

Inside `src/CliInvoke/Processes/Internal/ProcessWrapper.cs`, all `#if NET11_0`:

- Start processes **suspended** on Windows and macOS via `ProcessStartInfo.StartSuspended`, apply `ProcessResourcePolicy` through the process control adapter, then resume via `SafeProcessHandle.Resume()` — **always, including when `SetResourcePolicy` throws** (the existing always-resume guard comments at lines ~135–159 encode this rule).
- OS scoping: the `StartSuspended` + resume replacement applies on **Windows and macOS** only (see the in-code TODO at `ProcessWrapper.cs:131–132`). Linux/macOS/FreeBSD adapters keep the `kill(SIGSTOP)`/`kill(SIGCONT)` P/Invoke path (`UnixProcessControlAdapter.cs:224`) on both legs; .NET 11 signal adoption there (arriving via `Process.Signal(PosixSignal)` rather than a raw `kill` swap) belongs to TK005.
- Call sites being replaced (`#if NET11_0`): the suspend → apply → always-resume guard cycle in `OnStarted` (`ProcessWrapper.cs:120–165`); `SuspendProcess`/`ResumeProcess` and per-OS adapter routing at `:355`/`:377`.
- **No new public API** on `ExternalProcess` or `IExternalProcess` — suspend/resume stays a private mechanism; the `IExternalProcess` surface is unchanged.
- **No net10.0 behavior change**: the net10.0 leg keeps today's code exactly (shared-source path).

## Size

- **Files** - 1-4 (ProcessWrapper.cs plus per-OS control adapters on the Windows/macOS paths)
- **Large Edits required** - expected near the threshold (the OnStarted resume-guard restructuring plus per-leg #if blocks)

## Recommended Workflow

### Step 1 - Map the current suspend/apply/resume flow

Where: src/CliInvoke/Processes/Internal/ProcessWrapper.cs

- Read `OnStarted` (lines ~120–165), the always-resume guard comments (~135–152), and `SuspendProcess`/`ResumeProcess` (~355/~377) including the per-OS adapter routing.
- Read the Windows and macOS process control adapters (`src/CliInvoke/Processes/Internal/ControlAdapters/`) to confirm which sites the `#if NET11_0` swap replaces.

Verify: the call-site inventory in this ticket matches the current code.

### Step 2 - Add the start-suspended path for Windows and macOS

Where: src/CliInvoke/Processes/Internal/ProcessWrapper.cs (site: src/CliInvoke/Processes/Internal/ControlAdapters/WindowsProcessControlAdapter.cs and the macOS adapter as needed)

- Under `#if NET11_0`, start processes with `ProcessStartInfo.StartSuspended` on Windows and macOS.
- Keep the process-control adapter seam intact: the policy still applies through `ProcessControlAdapter.SetResourcePolicy`, not inline.

Verify: build the net11.0 leg; no net10.0-leg source drift.

### Step 3 - Convert resume to SafeProcessHandle.Resume with the always-resume guard

Where: src/CliInvoke/Processes/Internal/ProcessWrapper.cs

- Resume via `SafeProcessHandle.Resume()` on the net11.0 leg for Windows/macOS.
- Preserve the guard semantics: resume runs even when `SetResourcePolicy` throws, and handles a process that already exited during policy application (exit-race comment at line ~163).
- Keep Linux/macOS/FreeBSD `kill(SIGSTOP)`/`kill(SIGCONT)` P/Invoke adapters untouched on both legs.

Verify: a review pass confirms the exception-path resume and exit-race handling remain as before.

### Step 4 - Verify public-surface freeze and both-leg behavior

Where: src/CliInvoke.Core (IExternalProcess) and src/CliInvoke (ExternalProcess)

- Confirm zero public API diff on `ExternalProcess`/`IExternalProcess`.
- Run the dual-leg test suite (TK002) focusing on process-lifecycle and resource-policy tests.

Verify: net10.0 leg builds and tests identical to before; net11.0 leg tests green; public API diff empty.

## Context pointers

##### Files

- `src/CliInvoke/Processes/Internal/ProcessWrapper.cs` — the `OnStarted` flow, `SuspendProcess`/`ResumeProcess`, TODO at ~131–132
- `src/CliInvoke/Processes/Internal/ControlAdapters/WindowsProcessControlAdapter.cs` — Windows adapter (its P/Invoke region swap itself is TK005)
- Unix adapters (`UnixProcessControlAdapter.cs`) — stay untouched here; signal adoption for them lands in TK005

##### ADRs

- None — process-control internals are not covered by an existing ADR.

##### Domain terms

- Resource-Owning Type (GLOSSARY.md) — `SafeProcessHandle` is an OS-resource-bearing type; resume-on-throw paths must not leak the handle

##### Ledger records

- `DECISIONS-CliInvoke-dotnet11.md#D008` — suspended start, adapter-applied policy, always-resume (including on `SetResourcePolicy` throw); no public suspend/resume API
- `DECISIONS-CliInvoke-dotnet11.md#T003` — net10.0 behavior-identical constraint; adapter-layer-only adoption; verified suspend/resume call sites
- `DECISIONS-CliInvoke-dotnet11.md#D002` — dual-leg parity for AOT/trimmable behavior
- `DECISIONS-CliInvoke-dotnet11.md#D006` — no preview-gated language surface; both legs satisfy the XML-doc/ConfigureAwait gates

## Acceptance criteria

- [ ] net11.0 leg starts processes suspended via `ProcessStartInfo.StartSuspended` on Windows and macOS [`DECISIONS-CliInvoke-dotnet11.md#D008`]
- [ ] `ProcessResourcePolicy` applies through the process control adapter before resume, on the net11.0 leg [`DECISIONS-CliInvoke-dotnet11.md#D008`]
- [ ] Resume runs unconditionally — including when `SetResourcePolicy` throws — via `SafeProcessHandle.Resume()` [`DECISIONS-CliInvoke-dotnet11.md#D008`]
- [ ] Linux/macOS/FreeBSD `kill(SIGSTOP)`/`kill(SIGCONT)` adapter paths are unchanged on both legs [`DECISIONS-CliInvoke-dotnet11.md#T003`]
- [ ] No new public API appears on `ExternalProcess` or `IExternalProcess` (public API diff is empty) [`DECISIONS-CliInvoke-dotnet11.md#D008`]
- [ ] net10.0-leg behavior and code paths are unchanged [`DECISIONS-CliInvoke-dotnet11.md#T003`]

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** - TK001 (the net11.0 TFM and `#if NET11_0` compile surface must exist; published as `001-work-branch-dual-targeting-and-language-levels`)
