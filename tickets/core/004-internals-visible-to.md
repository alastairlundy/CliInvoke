---
title: Grant InternalsVisibleTo for Specializations and Tests
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CliInvoke-process-invocation-pipeline.md
---

## Goal

Grant `InternalsVisibleTo` from the `CliInvoke` assembly to `CliInvoke.Specializations` and `CliInvoke.Tests` so the pipeline (which is `internal` after TK003) is reachable by the two consumers the deepening pass requires.

## What to build

Modify `src/CliInvoke/AssemblyInfo.cs` to add two new `InternalsVisibleTo` attributes at the assembly level -

- `[assembly: InternalsVisibleTo("CliInvoke.Specializations")]` - lets `CmdProcessInvoker` and `PowershellProcessInvoker` construct the pipeline in their constructors (TK006).
- `[assembly: InternalsVisibleTo("CliInvoke.Tests")]` - lets the pipeline dispatch tests construct the pipeline with a mock factory (TK008).

No other assembly receives `InternalsVisibleTo`. The pipeline is the only internal type these attributes expose.

## Size

- **Files** - 1

## Recommended Workflow

### Step 1 — Add the InternalsVisibleTo attributes

Where: `src/CliInvoke/AssemblyInfo.cs` (existing file - may not exist yet; create if absent)

- Add `using System.Runtime.CompilerServices;` if not present.
- Append two assembly-level attributes: `[assembly: InternalsVisibleTo("CliInvoke.Specializations")]` and `[assembly: InternalsVisibleTo("CliInvoke.Tests")]`.
- Keep the file alphabetised with other attributes if the file already has them.

Verify: `dotnet build src/CliInvoke.sln` succeeds. From `CliInvoke.Specializations` and `CliInvoke.Tests`, the `CliInvoke.ProcessInvocationPipeline` type is now reachable (TK003, TK006, TK008 confirm this in their own work).

## Context pointers

**Files** - `src/CliInvoke/AssemblyInfo.cs` (modify) - the deliverable. `src/CliInvoke/ProcessInvocationPipeline.cs` - the internal type these attributes expose (created in TK003).

**Ledger records** - `DECISIONS-CliInvoke-process-invocation-pipeline.md#T007` (both `InternalsVisibleTo` grants are scoped - no other assemblies receive the grant). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D004` (the grant to `CliInvoke.Specializations` is required so the specialisation wrappers can reach the pipeline). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D007` (the grant to `CliInvoke.Tests` is required for the dispatch tests; it is not granted to `CliInvoke.Specializations.Tests`).

## Acceptance criteria

- [ ] `[assembly: InternalsVisibleTo("CliInvoke.Specializations")]` is present in `src/CliInvoke/AssemblyInfo.cs`.
- [ ] `[assembly: InternalsVisibleTo("CliInvoke.Tests")]` is present in `src/CliInvoke/AssemblyInfo.cs`.
- [ ] No other assembly receives `InternalsVisibleTo` to `CliInvoke`.
- [ ] `CliInvoke.Specializations` can reference `CliInvoke.ProcessInvocationPipeline` from a public type after the attribute is in place (verified in TK006).
- [ ] `CliInvoke.Tests` can reference `CliInvoke.ProcessInvocationPipeline` from an internal test class after the attribute is in place (verified in TK008).

## Dependencies

**Blocked by** - None - can start immediately. Although the pipeline (TK003) must exist for the grant to be useful, the attribute itself is independent of any new type.
