---
title: Collapse ProcessConfigurationFactory to two static spec-callback overloads
classification: Independent
blocked_by: ["001-internal-15param-ctor"]
parent: IMPLEMENTATION-config-construction.md
---

## Goal

Reimplement `ProcessConfigurationFactory` as a `static` class that exposes exactly two `Create` overloads, constructs `ProcessConfiguration` directly via the internal 15-parameter ctor from TK001 (no `IProcessConfigurationBuilder` or `ProcessConfigurationBuilder` instantiation), and validates inputs through the factory's own checks plus the existing spec types (`ArgumentsSpec`, `EnvironmentVariablesSpec`, `ProcessResourcePolicySpec`, `UserCredentialSpec`). The factory remains a class (not a method) in the `CliInvoke` main package so its external convenience surface is preserved.

## What to build

In `src/CliInvoke/Extensions/ProcessConfigurationFactory.cs`:

1. Delete the three current `Create` overloads at lines 30–119.
2. Add the following two static overloads (the exact surface from `DECISIONS-CliInvoke-config-construction.md#T008`):

   ```csharp
   public static ProcessConfiguration Create(
       string targetFilePath, string arguments,
       string? workingDirectory = null, bool outputRedirection = true, bool enableWindowCreation = false);

   public static ProcessConfiguration Create(
       string targetFilePath, IEnumerable<string> arguments,
       string? workingDirectory = null, bool outputRedirection = true, bool enableWindowCreation = false,
       Action<EnvironmentVariablesSpec>? configureEnvironmentVariables = null,
       Action<ProcessResourcePolicySpec>? configureResourcePolicy = null,
       Action<UserCredentialSpec>? configureCredential = null);
   ```

3. Each overload constructs `ProcessConfiguration` directly via `new ProcessConfiguration(...)` — the internal 15-param ctor from TK001. Argument order and field mapping match the internal ctor (`targetFilePath`, `arguments`, `redirectStandardInput: false`, `outputRedirection`, `workingDirectoryPath`, `requiresAdministrator: false`, `environmentVariables`, `credential`, `standardInput: StreamWriter.Null`, `standardInputEncoding: Encoding.Default`, `standardOutputEncoding: Encoding.Default`, `standardErrorEncoding: Encoding.Default`, `processResourcePolicy`, `windowCreation: enableWindowCreation`, `useShellExecution: false`). For the string overload, `arguments` is passed through directly. For the `IEnumerable<string>` overload, the arguments are joined via `ArgumentsSpec.Build()` (mirroring the builder's pattern).
4. **Spec callback shape** (per `DECISIONS-CliInvoke-config-construction.md#T014`): for each non-null spec callback (`configureEnvironmentVariables`, `configureResourcePolicy`, `configureCredential`), instantiate a fresh spec, invoke the user callback against it, read `spec.Build()`, and pass the result to the internal ctor. If a callback throws, the exception propagates unchanged — no wrapping, no logging, no swallowing.
5. **Validation surface** (per `DECISIONS-CliInvoke-config-construction.md#T009`):
   - Factory performs `Directory.Exists(workingDirectory)` if non-null.
   - Spec callbacks run validation via each spec type's `Add`/`Set` methods.
   - The internal 15-param ctor handles null/empty `targetFilePath` / `arguments` validation.
   - **Do not replicate** the builder's cross-constraint check (`_useShellExecution && (_redirectStandardInput || _standardInput != StreamWriter.Null)`); the factory's surface does not expose those inputs (per `DECISIONS-CliInvoke-config-construction.md#T009` and `DECISIONS-CliInvoke-config-construction.md#T020`).
6. **Defaults retained** (per `DECISIONS-CliInvoke-config-construction.md#T015`): `outputRedirection = true`, `enableWindowCreation = false`.
7. **XML doc note** (per `DECISIONS-CliInvoke-config-construction.md#T015`): on each parameter where applicable, add an XML `<remarks>` block flagging that `ProcessConfigurationBuilder`'s `_outputRedirection` field defaults to `false` (per `ProcessConfigurationBuilder.cs:70`), surfacing the divergence between the two construction paths. Do not change the builder's field initializer in this ticket.
8. **No `params`** on either overload. **No `configureBuilder` parameter**. **No instance state** — the class is `static`. **No new `InternalsVisibleTo`** — the existing Core → `CliInvoke` grant covers the internal ctor call (per `DECISIONS-CliInvoke-config-construction.md#T011`).

The class declaration `public static class ProcessConfigurationFactory` (currently at line 19) stays in the `CliInvoke` namespace (per `DECISIONS-CliInvoke-config-construction.md#T007`).

## Size

- Files: 1

## Recommended Workflow

### Step 1 — Delete the three current `Create` overloads

Where: `src/CliInvoke/Extensions/ProcessConfigurationFactory.cs`

- Delete lines 30–119 entirely (the three `Create` overloads: the expression-bodied OL1 at 30–33, OL2 at 50–75, OL3 at 92–119).
- Leave the class declaration at line 19 (`public static class ProcessConfigurationFactory`) and the namespace declaration at line 13 unchanged.
- Verify: The file now contains only the class declaration + namespace; the build of `CliInvoke` reports missing `Create` references from `CliRun.cs:106` and from the 21 affected test sites — those will be addressed by TK004 after this ticket lands.

### Step 2 — Add the two static spec-callback overloads

Where: `src/CliInvoke/Extensions/ProcessConfigurationFactory.cs`

- Inside the `ProcessConfigurationFactory` class body, add OL1 (`Create(string targetFilePath, string arguments, string? workingDirectory = null, bool outputRedirection = true, bool enableWindowCreation = false)`) and OL2 (`Create(string targetFilePath, IEnumerable<string> arguments, string? workingDirectory = null, bool outputRedirection = true, bool enableWindowCreation = false, Action<EnvironmentVariablesSpec>? configureEnvironmentVariables = null, Action<ProcessResourcePolicySpec>? configureResourcePolicy = null, Action<UserCredentialSpec>? configureCredential = null)`).
- Each overload calls `new ProcessConfiguration(...)` (the internal 15-param ctor from TK001) with field mapping per the "What to build" section above.
- For the `IEnumerable<string>` overload: instantiate an `ArgumentsSpec`, add the arguments via the spec's `AddEnumerable` (mirroring `ProcessConfigurationBuilder.cs:99–110`), read `spec.Build()` for the joined `arguments` string, pass that to the ctor.
- Add the spec-callback invocation block to OL2: for each non-null callback, instantiate the fresh spec, invoke the callback, read `spec.Build()`, pass to the ctor. Exceptions propagate unchanged.
- Add `Directory.Exists(workingDirectory)` validation if `workingDirectory` is non-null.
- Verify: `dotnet build src/CliInvoke.sln` succeeds against the new overloads; the production caller `CliRun.cs:106` (uses OL2 signature) compiles without modification.

### Step 3 — Add the XML doc note flagging the builder's `_outputRedirection` default

Where: `src/CliInvoke/Extensions/ProcessConfigurationFactory.cs`

- On the `outputRedirection` parameter of each overload, add an XML `<remarks>` block: "Note: `ProcessConfigurationBuilder._outputRedirection` defaults to `false`; the factory's default is `true`. Use the builder when you need explicit output redirection control."
- Do not modify `ProcessConfigurationBuilder.cs:70` in this ticket.
- Verify: IntelliSense on the factory's `outputRedirection` parameter shows the remarks; XML doc build is clean (`dotnet build src/CliInvoke.sln` produces no doc warnings).

### Step 4 — Confirm no `InternalsVisibleTo` or cross-constraint changes are needed

Where: `src/CliInvoke.Core/CliInvoke.Core.csproj`; `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs:424–426`

- Run a grep for `InternalsVisibleTo` in `src/CliInvoke.Core/CliInvoke.Core.csproj` — must show only the existing two grants (CliInvoke, CliInvoke.Tests).
- Confirm `ProcessConfigurationBuilder.Build()` cross-constraint check at lines 424–426 is unchanged from `git diff`.
- Verify: No new `InternalsVisibleTo` was added (per `DECISIONS-CliInvoke-config-construction.md#T011`); the builder's cross-constraint check is preserved (per `DECISIONS-CliInvoke-config-construction.md#T009` and `DECISIONS-CliInvoke-config-construction.md#T020`).

## Context pointers

- Files:
  - `src/CliInvoke/Extensions/ProcessConfigurationFactory.cs` — target of the collapse (delete lines 30–119; add the two new overloads)
  - `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs:54–69` — the internal 15-param ctor from TK001 that the new overloads call directly
  - `src/CliInvoke.Core/CliInvoke.Core.csproj:57–58` — the existing `InternalsVisibleTo` grants that authorise the cross-assembly call from the `CliInvoke` factory
  - `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs:70` — `_outputRedirection = false` field initializer that the XML doc note flags (per `DECISIONS-CliInvoke-config-construction.md#T015`); do not modify in this ticket
  - `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs:99–110` — `ArgumentsSpec.AddEnumerable` pattern that the `IEnumerable<string>` overload mirrors for joining arguments
  - `src/CliInvoke/Extensions/Configuration/ConfigurationExtensions.cs:67–100` — pre-existing spec-callback mutation pattern in the Extensions assembly (the model the new factory's spec callbacks follow, per `DECISIONS-CliInvoke-config-construction.md#T014`)
- Domain terms:
  - "Resource-Owning Type" (from `GLOSSARY.md`) — `ProcessConfiguration` is a resource-owning type; the factory returns one and the caller owns its `IDisposable` lifecycle at `ProcessConfiguration.cs:197–202`; do not reproduce the glossary entry
- Ledger records:
  - `DECISIONS-CliInvoke-config-construction.md#T005` — factory kept as a class; collapsed from three to two overloads
  - `DECISIONS-CliInvoke-config-construction.md#T006` — static factory, spec-based, no `IProcessConfigurationBuilder`/`ProcessConfigurationBuilder` instantiation; directly constructs `ProcessConfiguration` via the internal ctor
  - `DECISIONS-CliInvoke-config-construction.md#T007` — factory remains in the `CliInvoke` (main package); namespace `CliInvoke`
  - `DECISIONS-CliInvoke-config-construction.md#T008` — exact two-overload surface (string, string, …) and (string, IEnumerable<string>, …, with three spec callbacks); no `params`, no `configureBuilder`
  - `DECISIONS-CliInvoke-config-construction.md#T009` — validation surface: factory inputs (`Directory.Exists`) + spec callbacks (`Add`/`Set`) + ctor null/empty; cross-constraint stays in `Build()`
  - `DECISIONS-CliInvoke-config-construction.md#T011` — no new `InternalsVisibleTo`; existing Core → CliInvoke + Core → Tests suffice
  - `DECISIONS-CliInvoke-config-construction.md#T014` — spec callback shape: fresh spec per callback; read `spec.Build()`; user-callback exceptions propagate unchanged
  - `DECISIONS-CliInvoke-config-construction.md#T015` — defaults retained (`outputRedirection = true`, `enableWindowCreation = false`); XML doc note flagging the builder's separate `_outputRedirection = false` default
  - `DECISIONS-CliInvoke-config-construction.md#T020` — no validation or XML doc warning for the `enableWindowCreation = true` + `outputRedirection = true` combination; runtime is permissive at `BaseProcessControlAdapter.cs:36, 40–41`

## Acceptance criteria

- [ ] `ProcessConfigurationFactory` exposes exactly two static `Create` overloads with the signatures documented in `DECISIONS-CliInvoke-config-construction.md#T008` (string + string overload; string + IEnumerable<string> overload with the three spec callbacks).
- [ ] The class is declared `public static class ProcessConfigurationFactory`; no instance state (per `DECISIONS-CliInvoke-config-construction.md#T006`).
- [ ] The class remains in the `CliInvoke` namespace and the `CliInvoke` main package (per `DECISIONS-CliInvoke-config-construction.md#T007`).
- [ ] Neither overload takes a `params string[] arguments` parameter (per `DECISIONS-CliInvoke-config-construction.md#T008`).
- [ ] Neither overload accepts an `Action<IProcessConfigurationBuilder>? configureBuilder` parameter (per `DECISIONS-CliInvoke-config-construction.md#T008`).
- [ ] Both overloads construct `ProcessConfiguration` directly via the internal 15-param ctor from TK001; no `new ProcessConfigurationBuilder(...)` or `IProcessConfigurationBuilder` instantiation occurs in the factory (per `DECISIONS-CliInvoke-config-construction.md#T006`).
- [ ] For each non-null spec callback (`configureEnvironmentVariables`, `configureResourcePolicy`, `configureCredential`), the factory instantiates a fresh spec, invokes the user callback against it, reads `spec.Build()`, and passes the result to the internal ctor (per `DECISIONS-CliInvoke-config-construction.md#T014`).
- [ ] User-callback exceptions propagate unchanged (no wrapping, no logging, no swallowing) (per `DECISIONS-CliInvoke-config-construction.md#T014`).
- [ ] The factory validates `Directory.Exists(workingDirectory)` when `workingDirectory` is non-null; spec types validate spec fields via their `Add`/`Set` methods; the internal 15-param ctor handles null/empty `targetFilePath` / `arguments` (per `DECISIONS-CliInvoke-config-construction.md#T009`).
- [ ] The factory does NOT replicate the builder's cross-constraint check (`_useShellExecution && (_redirectStandardInput || _standardInput != StreamWriter.Null)`); that check stays in `ProcessConfigurationBuilder.Build():424–426` (per `DECISIONS-CliInvoke-config-construction.md#T009` and `DECISIONS-CliInvoke-config-construction.md#T020`).
- [ ] Defaults retained: `outputRedirection = true`, `enableWindowCreation = false` (per `DECISIONS-CliInvoke-config-construction.md#T015`).
- [ ] An XML `<remarks>` block on each overload's `outputRedirection` parameter flags that `ProcessConfigurationBuilder._outputRedirection` defaults to `false`, surfacing the divergence (per `DECISIONS-CliInvoke-config-construction.md#T015`).
- [ ] No new `InternalsVisibleTo` declarations are added in `src/CliInvoke.Core/CliInvoke.Core.csproj`; the existing Core → `CliInvoke` and Core → `CliInvoke.Tests` grants suffice (per `DECISIONS-CliInvoke-config-construction.md#T011`).
- [ ] No `[Obsolete]` attribute or deprecation shim is added on either overload (per `DECISIONS-CliInvoke-config-construction.md#T010` — the migration strategy is wrap, not deprecate).
- [ ] `dotnet build src/CliInvoke.sln` succeeds; the production caller `src/CliInvoke/Extensions/CliRun.cs:106` (uses OL2 signature) still compiles against the new OL1; the AOT test `tests/CliInvoke.AotProgram.Test/Program.cs:42` (uses OL2) still compiles.

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** — `001-internal-15param-ctor` (semantic coupling: this ticket constructs `ProcessConfiguration` directly via the internal 15-param ctor; TK001 must land first or the factory cannot reach the ctor from `CliInvoke` without the bridge).
