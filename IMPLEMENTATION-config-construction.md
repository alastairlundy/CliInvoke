# Implementation Blueprint: CliInvoke Configuration-Construction Collapse

> Blueprint for the architecture-review candidate 1 collapse of `ProcessConfigurationFactory` + `ProcessConfigurationBuilder` + `BuilderProcessConfiguration` bridge.

## Scope Binding

- **Linked Spec**: architecture-review candidate 1, "Collapse the configuration-construction cluster" (conversation context — no spec file).
- **Decision Ledger**: [docs/decisions/DECISIONS-CliInvoke-config-construction.md](docs/decisions/DECISIONS-CliInvoke-config-construction.md)
- **Notice**: This blueprint is a context pointer valid ONLY for the linked spec and must not be applied to other specifications without explicit authorization.

## Implementation Order

The records form a dependency graph; the order below is the natural execution order.

1. **[T004]** — change the 15-param `ProcessConfiguration` ctor from `protected` to `internal` (foundation).
2. **[T005]–[T008]** — collapse `ProcessConfigurationFactory` to two static spec-callback overloads.
3. **[T009]** — validation surface (factory inputs + spec callbacks + ctor null/empty).
4. **[T014]** — spec callback invocation shape.
5. **[T015]** — default value alignment + XML doc note.
6. **[T010]** — migrate the 21 affected params-overload call sites.
7. **[T020]** — confirm no validation/documentation for the cross-check (no-op).
8. **[T011]** — confirm no new `InternalsVisibleTo` grants (no-op).

## File Changes

### `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`

- Change the 15-parameter constructor (lines 54–69) from `protected` to `internal` [DECISIONS-CliInvoke-config-construction.md#T004]. Enables `ProcessConfigurationBuilder` (CliInvoke) and the thinned `ProcessConfigurationFactory` (CliInvoke) to call it directly via the existing `InternalsVisibleTo("CliInvoke")` grant [DECISIONS-CliInvoke-config-construction.md#T011].
- The public 3-parameter ctor's `: this(...)` delegation is unaffected (same-class call).

### `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`

- In `Build()` (lines 435–441), replace `new BuilderProcessConfiguration(...)` with `new ProcessConfiguration(...)` [DECISIONS-CliInvoke-config-construction.md#T004].
- Delete the `BuilderProcessConfiguration` class (lines 468–485) and its doc block.
- The cross-constraint check at `Build()`:424–426 (`_useShellExecution && (_redirectStandardInput || _standardInput != StreamWriter.Null)`) stays in place [DECISIONS-CliInvoke-config-construction.md#T009]. The factory does not replicate this check; the factory's T008 surface does not expose those inputs.
- Verified: no other references to `BuilderProcessConfiguration` exist.

### `src/CliInvoke/Extensions/ProcessConfigurationFactory.cs`

- Collapse the three `Create` overloads (lines 30–119) to two [DECISIONS-CliInvoke-config-construction.md#T005], reimplemented as a **static** factory [DECISIONS-CliInvoke-config-construction.md#T006] that directly `new`s a `ProcessConfiguration` via the internal 15-param ctor, assembling fields with the existing spec types; no `IProcessConfigurationBuilder`/`ProcessConfigurationBuilder` instantiation. Keep the class in `CliInvoke` (main package) [DECISIONS-CliInvoke-config-construction.md#T007].
- Exact surface [DECISIONS-CliInvoke-config-construction.md#T008] — two overloads, no `params`, no `configureBuilder`:

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

- Validation [DECISIONS-CliInvoke-config-construction.md#T009]:
  - Factory performs `Directory.Exists(workingDirectory)` if non-null.
  - Spec callbacks (`configureEnvironmentVariables`, `configureResourcePolicy`, `configureCredential`) run validation via each spec type's `Add`/`Set` methods.
  - The internal 15-param `ProcessConfiguration` constructor handles null/empty validation.
  - The cross-constraint check in `Build()` is NOT replicated in the factory.
- Spec callback shape [DECISIONS-CliInvoke-config-construction.md#T014]:
  - For each callback, instantiate a fresh spec, invoke the user callback against it, read `spec.Build()`, pass that to the 15-param ctor.
  - If the user callback throws, the exception propagates unchanged (no wrapping, no logging, no swallowing).
- Defaults [DECISIONS-CliInvoke-config-construction.md#T015]: retain `outputRedirection = true`, `enableWindowCreation = false`. Add an XML doc comment on the factory's parameters noting that `ProcessConfigurationBuilder`'s `_outputRedirection` field defaults to `false`, surfacing the divergence between the two construction paths.
- Cross-check [DECISIONS-CliInvoke-config-construction.md#T020]: do not validate the `enableWindowCreation + outputRedirection` combination; the combination is valid at the .NET `ProcessStartInfo` level (BaseProcessControlAdapter.cs:36, 40–41).
- No new `InternalsVisibleTo` declarations are required [DECISIONS-CliInvoke-config-construction.md#T011].

### Test files [T010]

The 21 call sites using the removed `params` overload shall wrap to the new two-overload surface. No `[Obsolete]` shim shall be retained.

#### `tests/CliInvoke.Tests/CliRunTests.cs`

- **8 zero-arg sites** (`Create(_targetFilePath)`) — wrap to `Create(_targetFilePath, "")`:
  - lines 69, 146, 161, 169, 178, 194, 222, 247.
- **2 named-bool sites** (`Create(_targetFilePath, outputRedirection: true)`) — wrap to `Create(_targetFilePath, "", outputRedirection: true)`:
  - lines 83, 97.

#### `tests/CliInvoke.Tests/Invokers/ProcessInvokerTests.cs`

- **3 zero-arg sites** — wrap to `Create(targetFilePath, "")`:
  - lines 58, 71, 84.
- (Line 29 uses `Create("dotnet", "--version")` which already compiles against `New-OL1`; no change.)

#### `tests/CliInvoke.Tests/Invokers/ProcessInvokerIntegrationTests.cs`

- **6 zero-arg sites** — wrap to `Create(_targetFilePath, "")`:
  - lines 34, 51, 68, 85, 101, 117.

#### `tests/CliInvoke.Tests/PipelineDispatchTests.cs`

- **1 zero-arg site** — wrap to `Create(_targetFilePath, "")`:
  - line 39.

#### `tests/CliInvoke.Tests.Trimming/Program.cs`

- **1 collection-expression site** — wrap to `Create("echo", new[] { randomNumber.ToString() })`:
  - line 29.

### Production and AOT files (no change)

- `src/CliInvoke/Extensions/CliRun.cs:106` — production caller uses OL2 (`string, string`); compiles against `New-OL1`. No change needed.
- `tests/CliInvoke.AotProgram.Test/Program.cs:42` — AOT test, uses OL2. No change needed.

## Ledger Reference

- **D001** — session goal: "Understand trade-offs first."
- **T001** — primary language = C# (repo-locked).
- **T002** — framework/runtime = .NET 10 (repo-locked).
- **T003** — project type = class library (repo-locked).
- **T004** — bridge elimination via internal ctor.
- **T005** — factory kept thin, reduced to 1–2 overloads.
- **T006** — static factory, spec-based, no builder dependency.
- **T007** — factory placement = `CliInvoke` (main package).
- **T008** — two-overload spec-based surface (no `params`, no `configureBuilder`).
- **T009** — validation surface (factory inputs + spec callbacks + ctor null/empty; cross-constraint stays in `Build()`).
- **T010** — migration strategy (21 affected params-overload call sites wrap; no `[Obsolete]` shim).
- **T011** — `InternalsVisibleTo` scope (existing Core → CliInvoke + Core → Tests suffice; no new grants).
- **T014** — spec callback shape (fresh spec per callback; read `spec.Build()`; exceptions propagate unchanged).
- **T015** — default value alignment (factory defaults match the public 3-param ctor; XML doc note surfaces builder's separate `_outputRedirection = false` default).
- **T020** — `enableWindowCreation + outputRedirection` cross-check (no validation, no documentation; runtime is permissive).

## Clarifying Interactions

- **I001** — T004 ctor visibility rationale (user rejected public full ctor; internal ctor via existing `InternalsVisibleTo` is the answer).
- **I002** — T005 factory purpose (factory is a convenience for external library consumers; collapse reduces overload redundancy, not the surface).
- **I003** — middle-ground ctor exploration (assessed as a code smell; rejected).
- **I004** — reorder: grill API shape before placement.
- **I005** — missing option D (static factory, no builder).
