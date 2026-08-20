---
title: Update ProcessWrapper ctor to take resolved FileInfo and override StartInfo.FileName
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-externalprocess-config-seam.md
---

## Goal

Reshape the internal `ProcessWrapper` constructor so it receives the resolved file path explicitly and applies it to `StartInfo.FileName` after `ProcessControlAdapter.ApplyConfiguration` runs. This removes the redundant `ProcessResourcePolicy` parameter and keeps the resolved path authoritative inside the wrapper.

## What to build

In `src/CliInvoke/Processes/Internal/ProcessWrapper.cs`:

1. Change the constructor signature from `internal ProcessWrapper(ProcessConfiguration configuration, ProcessResourcePolicy? resourcePolicy)` (currently at lines 55-57) to `internal ProcessWrapper(ProcessConfiguration configuration, FileInfo resolvedFilePath)`.
2. Drop the `ResourcePolicy = resourcePolicy ?? ProcessResourcePolicy.Default;` line; instead, source the policy from `configuration.ResourcePolicy` (e.g., `ResourcePolicy = configuration.ResourcePolicy;`).
3. After the existing `ProcessControlAdapter.ApplyConfiguration(this, configuration);` call (currently at line 60), add `StartInfo.FileName = resolvedFilePath.FullName;` so the resolved path supersedes the adapter's `processConfiguration.TargetFilePath` write.
4. The internal `ResourcePolicy` property stays (still consumed by `SetResourcePolicy` at line 110).

## Size

- Files: 1

## Recommended Workflow

### Step 1 — Update ctor signature

Where: `src/CliInvoke/Processes/Internal/ProcessWrapper.cs`

- Replace `internal ProcessWrapper(ProcessConfiguration configuration, ProcessResourcePolicy? resourcePolicy)` with `internal ProcessWrapper(ProcessConfiguration configuration, FileInfo resolvedFilePath)`.
- Replace the `ResourcePolicy = resourcePolicy ?? ProcessResourcePolicy.Default;` assignment with a sourcing-from-configuration assignment.
- Verify: The signature compiles.

### Step 2 — Apply resolved FileInfo to StartInfo.FileName

Where: `src/CliInvoke/Processes/Internal/ProcessWrapper.cs`

- After `ProcessControlAdapter.ApplyConfiguration(this, configuration);` (which writes `StartInfo.FileName = processConfiguration.TargetFilePath` from `BaseProcessControlAdapter.cs:30`), add `StartInfo.FileName = resolvedFilePath.FullName;`.
- Verify: The override lands after `ApplyConfiguration` (order matters — do not reorder).

### Step 3 — Build the wrapper and observe the expected bridge

Where: `src/CliInvoke/CliInvoke.csproj`

- Run `dotnet build src/CliInvoke/CliInvoke.csproj`. The build will FAIL because `ExternalProcess` and other call sites still pass the old `resourcePolicy` argument — that's expected and is the bridge to TK004.
- Verify: Build errors are limited to the expected call sites passing the old ctor signature.

## Context pointers

- Files:
  - `src/CliInvoke/Processes/Internal/ProcessWrapper.cs` (target of change)
  - `src/CliInvoke/Processes/Internal/ControlAdapters/BaseProcessControlAdapter.cs` (the adapter that writes `StartInfo.FileName` from `processConfiguration.TargetFilePath` — informs why T007 needs the override)
- Domain terms: "Resource-Owning Type" (GLOSSARY.md — `ProcessWrapper` is internal and inherits from `System.Diagnostics.Process`, which owns native handles)
- Ledger records:
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#T002` — `ProcessWrapper` ctor takes resolved path
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#T003` — ctor signature `(ProcessConfiguration, FileInfo)`; drop policy param
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#T007` — override `StartInfo.FileName` after `ApplyConfiguration`

## Acceptance criteria

- [ ] `ProcessWrapper` ctor signature is `(ProcessConfiguration, FileInfo)`.
- [ ] No separate `ProcessResourcePolicy` parameter is required by the ctor.
- [ ] `StartInfo.FileName = resolvedFilePath.FullName;` runs after `ApplyConfiguration`.
- [ ] `ResourcePolicy` property remains populated from `configuration.ResourcePolicy`.
- [ ] Build errors are limited to the expected `ExternalProcess` call sites (TK004 will resolve).

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** — None
