---
title: Add ProcessInvocationContext type in Core
classification: Independent
blocked_by: ["001-invocation-mode.md"]
parent: docs/decisions/DECISIONS-CliInvoke-process-invocation-pipeline.md
---

## Goal

Introduce `ProcessInvocationContext` in `CliInvoke.Core` - the single state-bearing object the pipeline accepts and mutates during execution. This is the shared input shape between this deepening pass (F1) and the future middleware system.

## What to build

Create a new public class `ProcessInvocationContext` in the `CliInvoke.Core` namespace with five members -

- `Configuration` of type `ProcessConfiguration`, set through the constructor (required).
- `ExitConfiguration` of type `ProcessExitConfiguration`, set through the constructor (required).
- `Mode` of type `InvocationMode` (from TK001), set through the constructor (required).
- `CancellationToken` of type `CancellationToken`, set through the constructor as an optional fourth parameter with a default value of `default`.
- `Result` of type `ProcessResult?`, exposed as a public read/write property the pipeline mutates after leaf execution.

Use a traditional constructor with three positional parameters (`ProcessConfiguration`, `ProcessExitConfiguration`, `InvocationMode`) plus the optional `CancellationToken`. The codebase uses no primary constructors today, so this stays consistent with the existing style. The `Result` property is not part of the constructor; it is set by the pipeline after the leaf execution step.

## Size

- **Files** - 1

## Recommended Workflow

### Step 1 — Create the ProcessInvocationContext class

Where: `src/CliInvoke.Core/ProcessInvocationContext.cs` (new file)

- Add the file header license block matching the surrounding Core files.
- Declare a public class `ProcessInvocationContext` in the `CliInvoke.Core` namespace.
- Add a public constructor with signature `ProcessInvocationContext(ProcessConfiguration configuration, ProcessExitConfiguration exitConfiguration, InvocationMode mode, CancellationToken cancellationToken = default)`.
- Store the four constructor parameters as public read-only properties.
- Add a public read/write property `Result` of type `ProcessResult?` with default value `null`.

Verify: `dotnet build src/CliInvoke.sln` succeeds. The class compiles against the existing `ProcessConfiguration`, `ProcessExitConfiguration`, and `ProcessResult` types without any changes to those files.

## Context pointers

**Files** - `src/CliInvoke.Core/ProcessInvocationContext.cs` (new) - the deliverable. `src/CliInvoke.Core/InvocationMode.cs` - consumed by the `Mode` property once TK001 lands. `src/CliInvoke.Core/ProcessConfiguration.cs` and `src/CliInvoke.Core/ProcessExitConfiguration.cs` - existing types referenced by the constructor.

**Domain terms** - Process Invocation Context (the state-bearing object that travels through the pipeline; this ticket is the concrete realisation of that glossary term).

**Ledger records** - `DECISIONS-CliInvoke-process-invocation-pipeline.md#T002` (traditional constructor with three positional parameters and an optional `CancellationToken`, `Result` is a read/write property). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D002` (the five-field shape is normative - the pipeline mutates `Result` and reads `CancellationToken` from the same object). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D003` (the pipeline reads `CancellationToken` from the context, not from a separate parameter).

## Acceptance criteria

- [ ] A new public class `ProcessInvocationContext` exists in `CliInvoke.Core` with the five specified members.
- [ ] The constructor accepts `ProcessConfiguration`, `ProcessExitConfiguration`, `InvocationMode` as required positional parameters and `CancellationToken` as an optional fourth parameter defaulting to `default`.
- [ ] The `Result` property is public, read/write, and initialised to `null`.
- [ ] The class compiles after TK001 (`InvocationMode`) is in place.
- [ ] Construction syntax `new ProcessInvocationContext(config, exitConfig, InvocationMode.Raw)` works without a `CancellationToken` argument.

## Dependencies

**Blocked by** - `001-invocation-mode.md` - the `Mode` constructor parameter requires the `InvocationMode` enum to exist.
