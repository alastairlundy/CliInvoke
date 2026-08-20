---
title: Migrate 21 params-overload call sites in tests to the new two-overload factory surface
classification: Independent
blocked_by: ["003-collapse-processconfigurationfactory"]
parent: IMPLEMENTATION-config-construction.md
---

## Goal

Wrap the 21 test call sites that depend on the removed `params string[] arguments` overload of `ProcessConfigurationFactory.Create` to the new two-overload surface from TK003. No `[Obsolete]` shim is introduced; the 12 unaffected sites (`CliRun.cs:106` + 1 AOT + 10 other OL2/OL3 sites) are not modified.

## What to build

The migration is purely a wrapping change at the call sites — no behavioural change. Each affected site is a one-line edit.

### `tests/CliInvoke.Tests/CliRunTests.cs`

- **8 zero-arg sites** — wrap `Create(_targetFilePath)` to `Create(_targetFilePath, "")`:
  - lines 69, 146, 161, 169, 178, 194, 222, 247.
- **2 named-bool sites** — wrap `Create(_targetFilePath, outputRedirection: true)` to `Create(_targetFilePath, "", outputRedirection: true)`:
  - lines 83, 97.

### `tests/CliInvoke.Tests/Invokers/ProcessInvokerTests.cs`

- **3 zero-arg sites** — wrap to `Create(targetFilePath, "")`:
  - lines 58, 71, 84.
- Line 29 (`Create("dotnet", "--version")`) is unaffected — it already matches the new OL1 signature and does not need to change.

### `tests/CliInvoke.Tests/Invokers/ProcessInvokerIntegrationTests.cs`

- **6 zero-arg sites** — wrap `Create(_targetFilePath)` to `Create(_targetFilePath, "")`:
  - lines 34, 51, 68, 85, 101, 117.

### `tests/CliInvoke.Tests/PipelineDispatchTests.cs`

- **1 zero-arg site** — wrap `Create(_targetFilePath)` to `Create(_targetFilePath, "")`:
  - line 39.

### `tests/CliInvoke.Tests.Trimming/Program.cs`

- **1 collection-expression site** — wrap `Create("echo", [randomNumber.ToString()])` to `Create("echo", new[] { randomNumber.ToString() })`:
  - line 29. The collection-expression `[...]` syntax is not accepted by the new `string` overload's second parameter; it must be materialised into a `string[]`.

### Sites that DO NOT change (12 unaffected)

- `src/CliInvoke/Extensions/CliRun.cs:106` — production caller uses the new OL1 (`string, string`) signature; unchanged.
- `tests/CliInvoke.AotProgram.Test/Program.cs:42` — AOT test, uses the new OL1; unchanged.
- `tests/CliInvoke.Extensions.Tests/DependencyInjection/DependencyInjectionExtensionsTests.cs:158` — `Create(filePath, arguments)`; already matches new OL1.
- `tests/CliInvoke.Tests/DependencyInjection/DependencyInjectionExtensionTests.cs:158` — same.
- `tests/CliInvoke.Specializations.Tests/Middleware/PowerShellMiddlewareIntegrationTests.cs:68` — `Create("dotnet", "--version")`; already matches new OL1.
- `tests/CliInvoke.Tests/Resolvers/FilePathResolverTests.cs:37, 49` — `Create("where", "dotnet.exe")` and `Create("which", "dotnet")`; already match new OL1.
- `tests/CliInvoke.Tests/Middleware/Integration/LoggingMiddlewareIntegrationTests.cs:93` — `Create(filePath, arguments)`; already matches new OL1.
- `tests/CliInvoke.Tests/Middleware/Integration/PostExitValidationMiddlewareIntegrationTests.cs:30, 49, 69` — `Create("dotnet", "--version")` and similar; already match new OL1.

## Size

- Files: 5

## Recommended Workflow

### Step 1 — Wrap the 10 sites in `CliRunTests.cs`

Where: `tests/CliInvoke.Tests/CliRunTests.cs`

- For lines 69, 146, 161, 169, 178, 194, 222, 247: replace `ProcessConfigurationFactory.Create(_targetFilePath)` with `ProcessConfigurationFactory.Create(_targetFilePath, "")`.
- For lines 83, 97: replace `ProcessConfigurationFactory.Create(_targetFilePath, outputRedirection: true)` with `ProcessConfigurationFactory.Create(_targetFilePath, "", outputRedirection: true)`.
- Verify: A grep for `ProcessConfigurationFactory\.Create\(_targetFilePath\)$` (no second arg) in `tests/CliInvoke.Tests/CliRunTests.cs` returns no matches.

### Step 2 — Wrap the 3 sites in `ProcessInvokerTests.cs`

Where: `tests/CliInvoke.Tests/Invokers/ProcessInvokerTests.cs`

- For lines 58, 71, 84: replace each `ProcessConfigurationFactory.Create(<single-arg>)` with `ProcessConfigurationFactory.Create(<single-arg>, "")`. Line 29 stays unchanged.
- Verify: A grep for `ProcessConfigurationFactory\.Create\("[^"]*"\)$` (single-string, no second arg) in this file returns no matches.

### Step 3 — Wrap the 6 sites in `ProcessInvokerIntegrationTests.cs`

Where: `tests/CliInvoke.Tests/Invokers/ProcessInvokerIntegrationTests.cs`

- For lines 34, 51, 68, 85, 101, 117: replace `ProcessConfigurationFactory.Create(_targetFilePath)` with `ProcessConfigurationFactory.Create(_targetFilePath, "")`.
- Verify: A grep for `ProcessConfigurationFactory\.Create\(_targetFilePath\)$` in this file returns no matches.

### Step 4 — Wrap the site in `PipelineDispatchTests.cs`

Where: `tests/CliInvoke.Tests/PipelineDispatchTests.cs`

- For line 39: replace `ProcessConfigurationFactory.Create(_targetFilePath)` with `ProcessConfigurationFactory.Create(_targetFilePath, "")`.
- Verify: A grep for `ProcessConfigurationFactory\.Create\(_targetFilePath\)$` in this file returns no matches.

### Step 5 — Wrap the collection-expression site in the Trimming Program.cs

Where: `tests/CliInvoke.Tests.Trimming/Program.cs`

- For line 29: replace `ProcessConfigurationFactory.Create("echo", [randomNumber.ToString()])` with `ProcessConfigurationFactory.Create("echo", new[] { randomNumber.ToString() })`.
- Verify: A grep for `ProcessConfigurationFactory\.Create\("echo", \[` in this file returns no matches.

### Step 6 — Build the full solution and run the test suite

Where: `src/CliInvoke.sln` and `tests/CliInvoke.Tests/`

- Run `dotnet build src/CliInvoke.sln`.
- Run `dotnet test tests/CliInvoke.Tests/`.
- Verify: Build clean; all existing tests in `tests/CliInvoke.Tests/` pass with no regressions. The trimming test project also builds cleanly. `CliRun.cs:106` (production caller) still compiles against the new OL1.

## Context pointers

- Files (5 targets):
  - `tests/CliInvoke.Tests/CliRunTests.cs` — 8 zero-arg sites (lines 69, 146, 161, 169, 178, 194, 222, 247) + 2 named-bool sites (lines 83, 97)
  - `tests/CliInvoke.Tests/Invokers/ProcessInvokerTests.cs` — 3 zero-arg sites (lines 58, 71, 84); line 29 unaffected
  - `tests/CliInvoke.Tests/Invokers/ProcessInvokerIntegrationTests.cs` — 6 zero-arg sites (lines 34, 51, 68, 85, 101, 117)
  - `tests/CliInvoke.Tests/PipelineDispatchTests.cs` — 1 zero-arg site (line 39)
  - `tests/CliInvoke.Tests.Trimming/Program.cs` — 1 collection-expression site (line 29)
- Domain terms: (no new ones needed — all call sites are mechanical wraps)
- Ledger records:
  - `DECISIONS-CliInvoke-config-construction.md#T010` — migration strategy: 21 params-overload sites wrap to the new two-overload surface; no `[Obsolete]` shim; the 12 unaffected sites need not change
  - `DECISIONS-CliInvoke-config-construction.md#T008` — the two-overload surface (string, string, …) and (string, IEnumerable<string>, …, with spec callbacks) the new sites must match
  - `DECISIONS-CliInvoke-config-construction.md#T005` — factory kept as a class; call-site migration is in-scope of the collapse

## Acceptance criteria

- [ ] All 8 zero-arg sites in `CliRunTests.cs` (lines 69, 146, 161, 169, 178, 194, 222, 247) are wrapped to `Create(_targetFilePath, "")` (per `DECISIONS-CliInvoke-config-construction.md#T010`).
- [ ] Both named-bool sites in `CliRunTests.cs` (lines 83, 97) are wrapped to `Create(_targetFilePath, "", outputRedirection: true)` (per `DECISIONS-CliInvoke-config-construction.md#T010`).
- [ ] All 3 zero-arg sites in `ProcessInvokerTests.cs` (lines 58, 71, 84) are wrapped to `Create(<arg>, "")`; line 29 (`Create("dotnet", "--version")`) is unchanged (per `DECISIONS-CliInvoke-config-construction.md#T010`).
- [ ] All 6 zero-arg sites in `ProcessInvokerIntegrationTests.cs` (lines 34, 51, 68, 85, 101, 117) are wrapped to `Create(_targetFilePath, "")` (per `DECISIONS-CliInvoke-config-construction.md#T010`).
- [ ] The single zero-arg site in `PipelineDispatchTests.cs` (line 39) is wrapped to `Create(_targetFilePath, "")` (per `DECISIONS-CliInvoke-config-construction.md#T010`).
- [ ] The collection-expression site in `tests/CliInvoke.Tests.Trimming/Program.cs` (line 29) is wrapped to `Create("echo", new[] { randomNumber.ToString() })` (per `DECISIONS-CliInvoke-config-construction.md#T010`).
- [ ] No `[Obsolete]` attribute or deprecation shim is introduced on `ProcessConfigurationFactory` or its overloads in this ticket (per `DECISIONS-CliInvoke-config-construction.md#T010`).
- [ ] The 12 unaffected sites (listed in the "Sites that DO NOT change" section above) are unchanged.
- [ ] `dotnet build src/CliInvoke.sln` succeeds across all 4 test projects (`CliInvoke.Tests`, `CliInvoke.Extensions.Tests`, `CliInvoke.Specializations.Tests`, `CliInvoke.Tests.Trimming`) and the AOT test project.
- [ ] `dotnet test tests/CliInvoke.Tests/` passes with no regressions.

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** — `003-collapse-processconfigurationfactory` (semantic coupling: this ticket wraps 21 call sites to the new two-overload surface from TK003; the new surface must exist before the migration can compile).
