---
title: ExternalProcess lifecycle synchronization
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-bug-audit-fixes.md
---

## Goal

Make `ExternalProcess` safe for cross-thread use by serializing its start gate and internal wrapper swap, since its public surface invites concurrent calls.

## What to build

- A private lock serializes the start gate and the `_processWrapper` swap in `src/CliInvoke/Processes/ExternalProcess.cs` (DECISIONS-CliInvoke-bug-audit-fixes.md#T016).
- Readers capture the wrapper reference under the lock and operate on the snapshot; long awaits stay outside the lock (DECISIONS-CliInvoke-bug-audit-fixes.md#T016).
- An `Interlocked`-based state machine and documented-contract alternatives were considered and not adopted.

## Size

- **Files** - 2 (1 source edit, 1 test edit)

## Recommended Workflow

### Step 1 - Map the lifecycle access points

Where: src/CliInvoke/Processes/ExternalProcess.cs

- Read the file; identify the start gate, every `_processWrapper` read and write, and which paths await.

Verify: a complete list of access points exists before editing.

### Step 2 - Introduce the private lock

Where: src/CliInvoke/Processes/ExternalProcess.cs

- Add a private lock object; serialize the start gate and the `_processWrapper` swap under it.

Verify: the start gate cannot double-execute under concurrent calls.

### Step 3 - Convert readers to snapshot reads

Where: src/CliInvoke/Processes/ExternalProcess.cs

- Each reader captures the wrapper reference under the lock and operates on the snapshot afterwards.
- Keep every await outside the lock — no await may hold the lock.

Verify: code review confirms no await occurs while the lock is held.

### Step 4 - Add concurrency tests

Where: tests/CliInvoke.Tests/Processes/ExternalProcessNoMutationTests.cs

- Add concurrency tests exercising concurrent starts and overlapping reads (DECISIONS-CliInvoke-bug-audit-fixes.md#T027).

Verify: dotnet test passes, including the new concurrency tests.

## Context pointers

##### Files

- `src/CliInvoke/Processes/ExternalProcess.cs` - the only source file this ticket edits
- `tests/CliInvoke.Tests/Processes/ExternalProcessNoMutationTests.cs` - no-mutation and concurrency tests

##### Domain terms

- Resource-Owning Type - `ExternalProcess` manages unmanaged OS (operating system) resources; the lock protects the lifecycle of the underlying process handle wrapper.

##### Ledger records

- `DECISIONS-CliInvoke-bug-audit-fixes.md#T016` - private lock on start gate and wrapper swap; snapshot reads; awaits outside the lock; state-machine and documented-contract alternatives not adopted
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T001`, `DECISIONS-CliInvoke-bug-audit-fixes.md#T027` - existing files only; concurrency tests required

## Acceptance criteria

- [ ] The start gate and the `_processWrapper` swap are serialized under a private lock (T016)
- [ ] Readers capture the wrapper reference under the lock and operate on the snapshot (T016)
- [ ] No await holds the lock (T016)
- [ ] Concurrency tests pass

## Dependencies

**Blocked by** - None - can start immediately
