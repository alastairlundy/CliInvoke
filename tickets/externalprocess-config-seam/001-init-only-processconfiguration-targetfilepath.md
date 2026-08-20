---
title: Init-only ProcessConfiguration.TargetFilePath with no-mutation XML doc
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-externalprocess-config-seam.md
---

## Goal

Make `ProcessConfiguration.TargetFilePath` setter init-only so callers cannot reassign it post-construction, and document the no-mutation contract at the call site so the resolved path surface is discoverable from IntelliSense.

## What to build

In `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`, change `public string TargetFilePath { get; set; }` (currently at line 107) to `public string TargetFilePath { get; init; }`. Above the property, add a `<remarks>` block stating "Not mutated after Start; for the resolved file path, see the result." plus a `<see cref="ProcessResult.ExecutedFilePath"/>` cross-reference.

This is the type-system enforcement of the no-mutation contract. Post-construction assignment (`config.TargetFilePath = "..."`) will no longer compile; ctor-parameter and object-initializer assignment still work.

## Size

- Files: 1

## Recommended Workflow

### Step 1 — Flip TargetFilePath accessor

Where: `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`

- Change `public string TargetFilePath { get; set; }` to `{ get; init; }`.
- Verify: A grep for `TargetFilePath { get; set; }` returns no matches in `src/CliInvoke.Core/`.

### Step 2 — Add no-mutation remarks

Where: `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`

- Above the `TargetFilePath` property, add a `<remarks>` block with the one-line text "Not mutated after Start; for the resolved file path, see the result." plus `<see cref="ProcessResult.ExecutedFilePath"/>`.
- Verify: The XML doc builds cleanly (`dotnet build src/CliInvoke.Core/CliInvoke.Core.csproj`) and IntelliSense shows the remarks for the property.

### Step 3 — Verify build and downstream reachability

Where: `tests/CliInvoke.Tests/Primitives/ProcessConfigurationTests.cs` and `tests/CliInvoke.Tests/Builders/ProcessConfigurationBuilderTests.cs`

- Run `dotnet build src/CliInvoke.sln` to confirm the init-only change compiles across the repo.
- Run `dotnet test tests/CliInvoke.Tests/` to confirm existing tests pass (no test assigns `TargetFilePath` post-construction).
- Verify: Build clean; existing tests green.

## Context pointers

- Files: `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs` (target of change)
- Domain terms: "Process Invocation Pipeline", "Resource-Owning Type" (from GLOSSARY.md — relevant only as background; do not reproduce)
- Ledger records:
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#T004` — TargetFilePath setter visibility is init-only
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#T012` — XML doc wording on TargetFilePath (brief remarks plus cref)
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#D003` — call-site documentation requirement

## Acceptance criteria

- [ ] `ProcessConfiguration.TargetFilePath` is declared `{ get; init; }`.
- [ ] Assigning `TargetFilePath` post-construction does not compile.
- [ ] The `<remarks>` block is present above the property and includes `<see cref="ProcessResult.ExecutedFilePath"/>`.
- [ ] `dotnet build src/CliInvoke.sln` succeeds with the change.
- [ ] Existing tests in `tests/CliInvoke.Tests/Primitives/ProcessConfigurationTests.cs` and `tests/CliInvoke.Tests/Builders/ProcessConfigurationBuilderTests.cs` pass without modification.

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** — None
