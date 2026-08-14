# Decision Ledger — CliInvoke configuration seam stack

### [D001] — session goal

- **Driver**: The user wants the public configuration surface to be smaller — callers should learn one entry point per concept, not two.
- **Resolved Answer**: "Shrink the public surface".
- **Normalized Requirement**: The deepened `IProcessConfigurationBuilder` shall expose one entry point per configuration concept, replacing the dual `Set*` / `Configure*` paths.
- **Constraints**: `None.`

### [D002] — delivery mode

- **Driver**: The library is already at a major version event due to upcoming middleware and other breaking changes, so carrying legacy sub-builder interfaces after the collapse is wasted surface weight.
- **Resolved Answer**: "Option A — Hard break. We're already at a major version event anyways due to middleware and other changes."
- **Normalized Requirement**: The four sub-builder interfaces (`IArgumentsBuilder`, `IEnvironmentVariablesBuilder`, `IProcessResourcePolicyBuilder`, `IUserCredentialBuilder`) shall be removed from `CliInvoke.Core` and replaced with one entry point per concept on `IProcessConfigurationBuilder`.
- **Constraints**: Removal co-occurs with the in-progress major version bump; no deprecation window required.

### [D003] — new entry-point shape

- **Driver**: The new entry point should make the configuration builder the single rich seam and let the consumer-facing API stay declarative.
- **Resolved Answer**: "Option A".
- **Normalized Requirement**: For each removed sub-builder, the new `IProcessConfigurationBuilder` exposes a single entry point of the form `ConfigureXxx(Action<XxxSpec>)` where `XxxSpec` is a small sealed class shipped from `CliInvoke.Core`.
- **Constraints**: Specs must not themselves expose wider API than the former sub-builder they replace.

### [D004] — credential disposal path

- **Driver**: The `SecureString` lifecycle is currently anchored on `UserCredentialBuilder` (which is `IDisposable`) and chained through `ProcessConfigurationBuilder.Dispose()`. Deleting the builder requires anchoring the lifecycle on another cohesive unit.
- **Resolved Answer**: "Option A".
- **Normalized Requirement**: `UserCredentialSpec` shall implement `IDisposable`; `ProcessConfigurationBuilder.Dispose()` shall chain into the disposal of every disposable spec it has built.
- **Constraints**: Avoid double-dispose of the same `SecureString` reference — either the spec's `Dispose` is idempotent, or ownership transfers to the built `UserCredential` and the spec releases its reference without disposing.

### [D005] — test surface

- **Driver**: Per the design vocabulary, the deepened interface is the test surface; tests on the absorbed sub-builder modules would test past the interface and survive internal refactors poorly.
- **Resolved Answer**: "Option A".
- **Normalized Requirement**: The four sub-builder test files (`ArgumentsBuilderTests`, `EnvironmentVariablesBuilderTests`, `ProcessResourcePolicyBuilderTests`, `UserCredentialBuilderTests`) shall be deleted; equivalent coverage shall be added to `ProcessConfigurationBuilderTests` against the deepened `IProcessConfigurationBuilder` interface.
- **Constraints**: Specs may grow internal helpers for assertions but must not expose a public test surface of their own.

### [D006] — spec type naming

- **Driver**: The user wants the rename from removed sub-builder interfaces to be discoverable by name, so callers can find the new spec by the same lookup pattern as the removed type.
- **Resolved Answer**: "Option A".
- **Normalized Requirement**: The four new spec classes shall be named `ArgumentsSpec`, `EnvironmentVariablesSpec`, `ProcessResourcePolicySpec`, `UserCredentialSpec`, preserving a 1:1 mapping from the removed sub-builder interfaces.
- **Constraints**: The `Spec` suffix is fixed by `D003`; API surface and file placement are deferred to later branches.

### [D007] — file placement

- **Driver**: The user wants each spec to be easy to find and to match the existing file-per-sub-builder pattern that callers already know.
- **Resolved Answer**: "Option A".
- **Normalized Requirement**: The four new spec classes shall live in `CliInvoke.Core/Configuration/` with one file per spec, file names matching the class names (e.g., `ArgumentsSpec.cs`).
- **Constraints**: Namespace is `CliInvoke.Core.Configuration`; future helpers may live in sibling files but no cross-spec sharing is required.

### [D008] — DI / Extensions impact

- **Driver**: The user wants `FromProcessStartInfo` to flow through the single `IProcessConfigurationBuilder` seam, honoring `D003`/`D004` once the sub-builder interfaces are removed.
- **Resolved Answer**: "Option A".
- **Normalized Requirement**: `FromProcessStartInfo(ProcessStartInfo)` in `src/CliInvoke.Extensions/Configuration/ConfigurationExtensions.cs` shall be rewritten to apply environment variables and credentials via `ConfigureEnvironmentVariables` and `ConfigureUserCredential` lambdas on `IProcessConfigurationBuilder`; the sub-builder pre-step shall be removed.
- **Constraints**: The `UserCredentialSpec` lifecycle established by `D004` must be respected inside the `ConfigureUserCredential` lambda; no other `CliInvoke.Extensions` helper uses the sub-builder interfaces and remains unchanged.

### [D009] — migration impact

- **Driver**: The user wants the hard break to land coherently — every reference migrated in lockstep so the new pattern is the only visible surface — and a written migration trail for external consumers.
- **Resolved Answer**: "A + D hybrid".
- **Normalized Requirement**: The migration shall (1) update in-place every in-repo reference — source (`D002`), tests (`D005`), `FromProcessStartInfo` (`D008`), `README.md`, `src/CliInvoke.Core/README.md`, `site/docs/guides/configuration.md`, `site/docs/guides/architecture.md`, `site/docs/guides/troubleshooting.md`, `site/docs/guides/resource-disposal.md`, and `AGENTS.md` — so no sub-builder references remain; and (2) add a new "Migrating from sub-builder interfaces" section under `site/docs/guides/` plus a `CHANGELOG.md` entry that documents the old-vs-new pattern.
- **Constraints**: The migration guide is additive documentation of the API diff; the in-place migration is the actual removal — both ship together.

### [T001] — ArgumentsSpec API surface

- **Driver**: The user wants the spec to be smaller than the sub-builder, drop rarely-used overloads, and hide `EscapeCharacters` as an implementation detail rather than expose it as public API.
- **Resolved Answer**: "Option B but with `EscapeCharacters` private/internal".
- **Normalized Requirement**: `ArgumentsSpec` shall expose only `Add(string, bool)`, `Add(IFormattable, bool)`, `AddEnumerable(IEnumerable<string>, bool)`, and `AddEnumerable(IEnumerable<IFormattable>, bool)`. `EscapeCharacters` shall be `private` (not `public`) on the spec.
- **Constraints**: API surface must not exceed the sub-builder envelope established by `D003`; the `IFormatProvider` overloads are dropped; `EscapeCharacters` is an implementation detail, not a public API.
- **Cites**: `D003`, `D006`, `D007`.

### [T002] — EnvironmentVariablesSpec API surface

- **Driver**: The user wants the spec to mirror the sub-builder API exactly, so callers migrate by changing the variable type from `IEnvironmentVariablesBuilder` to `EnvironmentVariablesSpec`, not by learning a new API.
- **Resolved Answer**: "Option A".
- **Normalized Requirement**: `EnvironmentVariablesSpec` shall expose `SetPair(string, string)`, `SetEnumerable(IEnumerable<KeyValuePair<string, string>>)`, `SetDictionary(IDictionary<string, string>)`, `SetReadOnlyDictionary(IReadOnlyDictionary<string, string>)`, `Build()`, and `Clear()`.
- **Constraints**: API surface must not exceed the sub-builder envelope established by `D003`; all six methods are mirrored directly.
- **Cites**: `D003`, `D006`, `D007`.

### [T003] — ProcessResourcePolicySpec API surface

- **Driver**: The user wants the spec to mirror the sub-builder API exactly, so callers migrate by changing the variable type from `IProcessResourcePolicyBuilder` to `ProcessResourcePolicySpec`, not by learning a new API. The spec is a sealed class — no interface stays — per the broader "Interface + Impl → Sealed Impl" collapse.
- **Resolved Answer**: "Option A".
- **Normalized Requirement**: `ProcessResourcePolicySpec` shall expose `SetProcessorAffinity(nint)`, `SetMinWorkingSet(nint)`, `SetMaxWorkingSet(nint)`, `SetPriorityClass(ProcessPriorityClass)`, `ConfigurePriorityBoost(bool)`, and `Build()`.
- **Constraints**: API surface must not exceed the sub-builder envelope established by `D003`; all six methods are mirrored directly; the spec is sealed with no interface.
- **Cites**: `D003`, `D006`, `D007`.

### [T005] — EnvironmentVariablesSpec API surface (revisit)

- **Driver**: The user wants the four `Set*` methods to collapse to `SetEnumerable` per the new "Interface + Impl → Sealed Impl, streamline call sites" lens, with `Clear()` retained for state-reset semantics.
- **Resolved Answer**: "Option C but keep Clear method".
- **Normalized Requirement**: `EnvironmentVariablesSpec` shall expose `SetEnumerable(IEnumerable<KeyValuePair<string, string>>)`, `Build()`, and `Clear()`. `SetPair`, `SetDictionary`, and `SetReadOnlyDictionary` are dropped. Existing callers of the dropped methods migrate to `SetEnumerable` (interface implementation for the dictionary overloads, array wrapping for the single-pair case).
- **Constraints**: `Supersedes: T002`. `Clear()` is retained for state-reset semantics.
- **Cites**: `D003`, `D006`, `D007`.

### [T006] — ProcessResourcePolicySpec API surface (revisit)

- **Driver**: The user wants the spec to keep the original method names for the unchanged methods, drop the `Configure` prefix from methods that didn't have it, and combine the working-set pair into a single `SetWorkingSet(nint min, nint max)` method. The "fully configured" path is `SetProcessResourcePolicy(ProcessResourcePolicy)` on `IProcessConfigurationBuilder`, which is preserved.
- **Resolved Answer**: "Option B with previous method names preserved and `SetWorkingSet` for the combined working set".
- **Normalized Requirement**: `ProcessResourcePolicySpec` shall expose `SetProcessorAffinity(nint)`, `SetWorkingSet(nint minWorkingSet, nint maxWorkingSet)`, `SetPriorityClass(ProcessPriorityClass)`, `ConfigurePriorityBoost(bool)`, and `Build()`. `SetMinWorkingSet` and `SetMaxWorkingSet` are dropped (replaced by `SetWorkingSet`).
- **Constraints**: `Supersedes: T003`. `SetProcessResourcePolicy(ProcessResourcePolicy)` on `IProcessConfigurationBuilder` is the path for handing a fully configured `ProcessResourcePolicy`; the spec is the gradual `ConfigureProcessResourcePolicy(Action<ProcessResourcePolicySpec>)` path.
- **Cites**: `D003`, `D006`, `D007`.

### [T004] — UserCredentialSpec API surface

- **Driver**: The user wants the spec to mirror the sub-builder API exactly, but rejects `LoadUserProfile` as the method name because `Set` + `Load` (verb-on-verb) reads awkwardly. The spec is a sealed class — no interface stays — per the broader "Interface + Impl → Sealed Impl" collapse.
- **Resolved Answer**: "Option A with `LoadUserProfile` renamed to `SetUserProfileLoading`".
- **Normalized Requirement**: `UserCredentialSpec` shall expose `SetDomain(string)`, `SetUsername(string)`, `SetPassword(SecureString)`, `SetUserProfileLoading(bool)`, `Build()`, and `Dispose()`. The `LoadUserProfile` method from the sub-builder is renamed to `SetUserProfileLoading` to follow the `Set*` cadence without the verb-on-verb collision.
- **Constraints**: API surface must not exceed the sub-builder envelope established by `D003`; the spec is sealed with no interface; the `IDisposable` lifecycle is governed by `D004`.
- **Cites**: `D003`, `D004`, `D006`, `D007`.

### [D010] — ArgumentValidationLogic reuse

- **Driver**: The user wants `ArgumentsSpec` to carry the `ArgumentValidationLogic` capability forward, mirroring `ArgumentsBuilder`'s dual-constructor pattern: a parameterless constructor (default null check) and an optional constructor that takes a `Func<string, bool>`.
- **Resolved Answer**: "Option A with optional constructor matching the existing `ArgumentsBuilder` pattern".
- **Normalized Requirement**: `ArgumentsSpec` shall expose two constructors — a parameterless `ArgumentsSpec()` and `ArgumentsSpec(Func<string, bool> argumentValidationLogic)`. The default case (no validation logic) shall behave as a null check on the argument value, matching the existing `ArgumentsBuilder.IsValidArgument` behavior in `src/CliInvoke/Builders/ArgumentsBuilder.cs:296-318`.
- **Constraints**: The default constructor's validation behavior is a null check via `ArgumentNullException.ThrowIfNull`; the optional constructor accepts user-provided validation logic that is invoked for each argument.
- **Cites**: `D003`, `T001`.

### [T007] — EnvironmentVariablesSpec constructors

- **Driver**: The user wants the spec to carry the `StringComparer` knob forward but drop the redundant bool-only and protected constructors, matching the `D010` optional-constructor pattern.
- **Resolved Answer**: "Option A".
- **Normalized Requirement**: `EnvironmentVariablesSpec` shall expose a parameterless `()` constructor and a `(StringComparer stringComparer, bool throwExceptionIfDuplicateKeyFound)` constructor. The bool-only constructor and the protected `(IDictionary<string, string>, StringComparer, bool)` constructor are dropped.
- **Constraints**: The spec is sealed; no subclasses exist; `StringComparer` is the only niche knob worth preserving.
- **Cites**: `D003`, `D006`, `D007`, `D010`.

### [T008] — ProcessResourcePolicySpec constructors

- **Driver**: The user wants the spec to match the existing `ProcessResourcePolicyBuilder` (parameterless) since no niche knob survives the collapse.
- **Resolved Answer**: "Option A".
- **Normalized Requirement**: `ProcessResourcePolicySpec` shall expose a parameterless constructor only.
- **Constraints**: No niche knob survives the collapse; the spec is the gradual `ConfigureProcessResourcePolicy(Action<ProcessResourcePolicySpec>)` path.
- **Cites**: `D003`, `D006`, `D007`.

### [T009] — UserCredentialSpec constructors

- **Driver**: The user wants the spec to match the `D010` pattern (parameterless + optional) but the all-fields constructor duplicates the `Set*` API, so it is dropped.
- **Resolved Answer**: "Option A".
- **Normalized Requirement**: `UserCredentialSpec` shall expose a parameterless constructor only.
- **Constraints**: The all-fields constructor is dropped; callers use the `Set*` methods inside the `ConfigureUserCredential` lambda.
- **Cites**: `D003`, `D006`, `D007`, `D010`.

### [T010] — spec lifecycle / ownership

- **Driver**: The user wants the parent builder to hold the specs as shared fields, matching the existing sub-builder pattern and satisfying `D004`'s disposal chain.
- **Resolved Answer**: "Option A".
- **Normalized Requirement**: `ProcessConfigurationBuilder` shall hold `_argumentsSpec`, `_environmentVariablesSpec`, `_processResourcePolicySpec`, `_userCredentialSpec` as fields; `ConfigureXxx(Action<XxxSpec>)` invokes the action on the shared field; `Build()` extracts via `spec.Build()`; `Dispose()` disposes every disposable spec (per `D004`).
- **Constraints**: The spec is mutable and shared; a second `ConfigureXxx` call re-mutates the same spec; the `SecureString` lifecycle in `UserCredentialSpec` is handled by the parent's `Dispose()` chain per `D004`.
- **Cites**: `D003`, `D004`, `T004`, `T007`, `T008`, `T009`.

### [T011] — internal state representation

- **Driver**: The user wants each spec to hold the same internal state as its sub-builder, keeping the migration mechanical rather than a re-design.
- **Resolved Answer**: "Option A".
- **Normalized Requirement**: Each spec shall hold the same internal state as its sub-builder: `ArgumentsSpec` → `StringBuilder` + validation logic; `EnvironmentVariablesSpec` → `Dictionary<string, string>` + `StringComparer` + throw flag; `ProcessResourcePolicySpec` → 5 nullable fields; `UserCredentialSpec` → 4 fields + `SecureString`.
- **Constraints**: The disposal path in `D004`/`T010` assumes the `SecureString` lives in `UserCredentialSpec`'s internal state; mirroring keeps the migration mechanical.
- **Cites**: `D003`, `D004`, `T001`, `T004`, `T005`, `T006`, `T007`, `T008`, `T009`, `T010`.

### [T012] — spec factory wiring

- **Driver**: The user wants the parent builder to eagerly create the specs in its constructor, matching the existing sub-builder pattern, with `ArgumentValidationLogic` threaded via an optional parent ctor parameter.
- **Resolved Answer**: "Option A".
- **Normalized Requirement**: `ProcessConfigurationBuilder` constructor shall create all four specs eagerly (matching the existing eager sub-builder pattern at `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs:54-57`); `ConfigureXxx` invokes the action on the shared field; `ArgumentValidationLogic` is passed to `ArgumentsSpec` if the parent ctor accepts an optional one.
- **Constraints**: All four specs are allocated even if unused; the eager pattern matches the existing sub-builder creation.
- **Cites**: `D003`, `D010`, `T010`.

## Consolidated Implementation Plan

Source ledger: `DECISIONS-CliInvoke-configuration-seam-stack.md`. Every file change is grouped by path; each change cites the `Dxxx`/`Txxx` record that drives it.

### `src/CliInvoke.Core/Builders/IArgumentsBuilder.cs`
- **DELETE** the interface. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D002`.

### `src/CliInvoke.Core/Builders/IEnvironmentVariablesBuilder.cs`
- **DELETE** the interface. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D002`.

### `src/CliInvoke.Core/Builders/IProcessResourcePolicyBuilder.cs`
- **DELETE** the interface. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D002`.

### `src/CliInvoke.Core/Builders/IUserCredentialBuilder.cs`
- **DELETE** the interface (including its `IDisposable` inheritance). Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D002`, `DECISIONS-CliInvoke-configuration-seam-stack.md#D004`.

### `src/CliInvoke.Core/Configuration/ArgumentsSpec.cs` (CREATE)
- Sealed class `ArgumentsSpec` in namespace `CliInvoke.Core.Configuration`. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D006`, `DECISIONS-CliInvoke-configuration-seam-stack.md#D007`.
- API: `Add(string, bool)`, `Add(IFormattable, bool)`, `AddEnumerable(IEnumerable<string>, bool)`, `AddEnumerable(IEnumerable<IFormattable>, bool)`; `EscapeCharacters` is `private`. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#T001`.
- Constructors: parameterless and `(Func<string, bool> argumentValidationLogic)`; default case is a null check. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D010`.
- Internal state: `StringBuilder` + validation logic. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#T011`.

### `src/CliInvoke.Core/Configuration/EnvironmentVariablesSpec.cs` (CREATE)
- Sealed class `EnvironmentVariablesSpec` in namespace `CliInvoke.Core.Configuration`. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D006`, `DECISIONS-CliInvoke-configuration-seam-stack.md#D007`.
- API: `SetEnumerable(IEnumerable<KeyValuePair<string, string>>)`, `Build()`, `Clear()`. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#T005`.
- Constructors: parameterless and `(StringComparer stringComparer, bool throwExceptionIfDuplicateKeyFound)`. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#T007`.
- Internal state: `Dictionary<string, string>` + `StringComparer` + throw flag. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#T011`.

### `src/CliInvoke.Core/Configuration/ProcessResourcePolicySpec.cs` (CREATE)
- Sealed class `ProcessResourcePolicySpec` in namespace `CliInvoke.Core.Configuration`. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D006`, `DECISIONS-CliInvoke-configuration-seam-stack.md#D007`.
- API: `SetProcessorAffinity(nint)`, `SetWorkingSet(nint minWorkingSet, nint maxWorkingSet)`, `SetPriorityClass(ProcessPriorityClass)`, `ConfigurePriorityBoost(bool)`, `Build()`. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#T006`.
- Constructor: parameterless only. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#T008`.
- Internal state: 5 nullable fields. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#T011`.

### `src/CliInvoke.Core/Configuration/UserCredentialSpec.cs` (CREATE)
- Sealed class `UserCredentialSpec` in namespace `CliInvoke.Core.Configuration`; implements `IDisposable`. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D006`, `DECISIONS-CliInvoke-configuration-seam-stack.md#D007`, `DECISIONS-CliInvoke-configuration-seam-stack.md#D004`.
- API: `SetDomain(string)`, `SetUsername(string)`, `SetPassword(SecureString)`, `SetUserProfileLoading(bool)`, `Build()`, `Dispose()`. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#T004`.
- Constructor: parameterless only. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#T009`.
- Internal state: 4 fields + `SecureString`; `Dispose` handles the `SecureString` lifecycle per `D004`. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#T011`.

### `src/CliInvoke/Builders/ArgumentsBuilder.cs`
- **DELETE** the class (replaced by `ArgumentsSpec`). Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D002`.

### `src/CliInvoke/Builders/EnvironmentVariablesBuilder.cs`
- **DELETE** the class (replaced by `EnvironmentVariablesSpec`). Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D002`.

### `src/CliInvoke/Builders/ProcessResourcePolicyBuilder.cs`
- **DELETE** the class (replaced by `ProcessResourcePolicySpec`). Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D002`.

### `src/CliInvoke/Builders/UserCredentialBuilder.cs`
- **DELETE** the class (replaced by `UserCredentialSpec`). Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D002`, `DECISIONS-CliInvoke-configuration-seam-stack.md#D004`.

### `src/CliInvoke.Core/Builders/IProcessConfigurationBuilder.cs` (MODIFY)
- Change the four `ConfigureXxx` signatures from `Action<I*Builder>` to `Action<XxxSpec>`: `ConfigureArguments(Action<ArgumentsSpec>)`, `ConfigureEnvironmentVariables(Action<EnvironmentVariablesSpec>)`, `ConfigureProcessResourcePolicy(Action<ProcessResourcePolicySpec>)`, `ConfigureUserCredential(Action<UserCredentialSpec>)`. Keep `SetProcessResourcePolicy(ProcessResourcePolicy)` unchanged. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D003`.

### `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs` (MODIFY)
- Replace the four sub-builder fields with spec fields (`_argumentsSpec`, `_environmentVariablesSpec`, `_processResourcePolicySpec`, `_userCredentialSpec`); create them eagerly in the constructor. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#T012`.
- `ConfigureXxx` invokes the action on the shared spec field. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#T010`.
- `Build()` extracts via `spec.Build()`. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#T010`, `DECISIONS-CliInvoke-configuration-seam-stack.md#T011`.
- `Dispose()` chains into disposal of every disposable spec (notably `UserCredentialSpec`). Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D004`, `DECISIONS-CliInvoke-configuration-seam-stack.md#T010`.

### `src/CliInvoke.Extensions/Configuration/ConfigurationExtensions.cs` (MODIFY)
- Rewrite `FromProcessStartInfo(ProcessStartInfo)` to apply environment variables and credentials via `ConfigureEnvironmentVariables` and `ConfigureUserCredential` lambdas; drop the sub-builder pre-step. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D008`.

### `tests/CliInvoke.Tests/Builders/ArgumentsBuilderTests.cs`
- **DELETE**. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D005`.

### `tests/CliInvoke.Tests/Builders/EnvironmentVariablesBuilderTests.cs`
- **DELETE**. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D005`.

### `tests/CliInvoke.Tests/Builders/ProcessResourcePolicyBuilderTests.cs`
- **DELETE**. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D005`.

### `tests/CliInvoke.Tests/Builders/UserCredentialBuilderTests.cs`
- **DELETE**. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D005`.

### `tests/CliInvoke.Tests/Builders/ProcessConfigurationBuilderTests.cs` (MODIFY)
- Add equivalent coverage against the deepened `IProcessConfigurationBuilder` interface (the four `ConfigureXxx` entry points and spec APIs). Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D005`.

### `README.md` (MODIFY)
- Update references to removed sub-builder interfaces (e.g., `UserCredentialBuilder` at line 228). Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D009`.

### `src/CliInvoke.Core/README.md` (MODIFY)
- Update the builder enumeration (lines 22–27) to list the four specs instead of the four interfaces. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D009`.

### `site/docs/guides/configuration.md` (MODIFY)
- Update the interface→type mapping table (lines 302–305), the `ConfigureXxx` examples (lines 314–315), and the working-set / credential notes (lines 148–167, 481–482). Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D009`.

### `site/docs/guides/architecture.md` (MODIFY)
- Update the builder descriptions (lines 42–43, 124–128) to reference the specs. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D009`.

### `site/docs/guides/troubleshooting.md` (MODIFY)
- Update `UserCredentialBuilder` references (lines 29, 55) to `UserCredentialSpec`. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D009`.

### `site/docs/guides/resource-disposal.md` (MODIFY)
- Update the disposal table (line 42) to reference `UserCredentialSpec` and its `IDisposable` lifecycle. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D009`, `DECISIONS-CliInvoke-configuration-seam-stack.md#D004`.

### `AGENTS.md` (MODIFY)
- Update the resource-disposal note (line 28) to reference `UserCredentialSpec`. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D009`.

### `site/docs/guides/migrating-from-sub-builder-interfaces.md` (CREATE)
- New migration guide documenting the old-vs-new pattern for all four concepts. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D009`.

### `CHANGELOG.md` (MODIFY)
- Add an entry referencing the migration guide and the hard break. Drives: `DECISIONS-CliInvoke-configuration-seam-stack.md#D009`.

## Ledger Reference

- **Design decisions**: `D001`, `D002`, `D003`, `D004`, `D005`, `D006`, `D007`, `D008`, `D009`, `D010`.
- **Technical decisions**: `T001`, `T004`, `T005`, `T006`, `T007`, `T008`, `T009`, `T010`, `T011`, `T012`.

### [D011] — Versioning & Release Mechanics

- **Driver**: The config-seam collapse removes four public interfaces. This is a hard break (`D002`). How do we communicate that to consumers?
- **Resolved Answer**: "CliInvoke main is where v3 pre-release is being developed. Therefore the changes land as part of the upcoming v3."
- **Normalized Requirement**: The config-seam changes land as part of the upcoming v3.0.0 release. No separate versioning scheme needed.
- **Constraints**: The `main` branch is already the v3 pre-release development branch.
- **Cites**: `D002`.

### [I001] — ticket output target and PR count

- **Prompt**: "Where should the decomposed tickets be published (local markdown files, GitHub Issues, GitLab Issues, Gitea Issues, Codeberg Issues, or a hosted Forgejo Instance's Issues)? And how many pull requests should this ticket set produce (default is 1 PR covering all tickets)?"
- **User Response**: "Local markdown files; 1 PR covering all tickets."
- **Resolution**: "Drove the publish target to local markdown files under tickets/ and grouped all tickets under a single pull request (PR count = 1)."
- **Notes**: "No issue-tracker CLI required; tickets are written as markdown at the repo root."

<!-- next-d: D012 -->
<!-- next-t: T013 -->
<!-- next-i: I002 -->
