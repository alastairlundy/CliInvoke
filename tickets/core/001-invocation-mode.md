---
title: Add InvocationMode enum in Core
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CliInvoke-process-invocation-pipeline.md
---

## Goal

Introduce the public `InvocationMode` enum in `CliInvoke.Core` that the process invocation pipeline uses as the dispatch key for switching between execution modes.

## What to build

Create a new public enum `InvocationMode` in the `CliInvoke.Core` namespace with exactly four values - `Raw`, `Buffered`, `Piped`, `FireAndForget`. The enum is the single source of truth for which `IExternalProcess` capture path the pipeline invokes and is shared with the future middleware system. No attributes, no wrapper struct, no extra members. The switch inside the pipeline uses these values directly so a future fifth value will produce a compile-time warning under exhaustiveness checks.

## Size

- **Files** - 1

## Recommended Workflow

### Step 1 — Create the InvocationMode enum

Where: `src/CliInvoke.Core/InvocationMode.cs` (new file)

- Add the file header license block matching the surrounding Core files.
- Declare a public enum named `InvocationMode` in the `CliInvoke.Core` namespace.
- Declare the four values in this order - `Raw`, `Buffered`, `Piped`, `FireAndForget`.
- Set the underlying type to `int` explicitly so the IL (Intermediate Language) layout is stable.

Verify: `dotnet build src/CliInvoke.sln` succeeds and the new file is picked up by `CliInvoke.Core.csproj` (no manual project edits required if the SDK style uses implicit glob).

## Context pointers

**Files** - `src/CliInvoke.Core/InvocationMode.cs` (new) - the deliverable. `src/CliInvoke.Core/IProcessInvoker.cs` - the contract that the enum's `Raw`, `Buffered`, `Piped` values must match (the existing three `Execute*Async` methods).

**Domain terms** - Resource-Owning Type (a future enum value added here will be the dispatch key for capture paths on resource-owning `IExternalProcess` objects).

**Ledger records** - `DECISIONS-CliInvoke-process-invocation-pipeline.md#T001` (enum shape with four values, no attributes, lives in Core). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D002` (enum values are the dispatch key the pipeline switch reads). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D004` (the type lives in Core).

## Acceptance criteria

- [ ] A new public enum `InvocationMode` exists in `CliInvoke.Core` with the four values `Raw`, `Buffered`, `Piped`, `FireAndForget` in that order.
- [ ] No attributes decorate the enum or its values.
- [ ] The enum compiles standalone in `CliInvoke.Core` without any other new types.
- [ ] The enum is referenced by name (not value) from at least one other ticket's follow-up work - TK002 in this set.

## Dependencies

**Blocked by** - None - can start immediately.
