# Implementation Blueprint — ExternalProcess configuration seam

## Scope Binding

- **Linked Spec:** `docs/decisions/DECISIONS-CliInvoke-externalprocess-config-seam.md`
- **Decision Ledger:** `docs/decisions/DECISIONS-CliInvoke-externalprocess-config-seam.md`
- **Notice:** This blueprint is a context pointer valid ONLY for the linked spec and ledger above. It must not be applied to other specifications without explicit authorization.

This blueprint operationalises the resolved decisions in the Decision Ledger for the `ExternalProcess` configuration-mutation fix: stopping `ExternalProcess` from mutating the caller's `ProcessConfiguration` in place [`DECISIONS-CliInvoke-externalprocess-config-seam.md#D002`] while keeping the resolved file path accurate in `ProcessResult.ExecutedFilePath` [`DECISIONS-CliInvoke-externalprocess-config-seam.md#D001`/`#D002`/`#T007`], within a simple, intuitive surface [`#D001`/`#D003`/`#T009`/`#T012`].

## Per-file changes

### `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`

- Change `public string TargetFilePath { get; set; }` to `public string TargetFilePath { get; init; }` [`#T004`]. Post-construction assignment shall no longer compile.
- Add a `<remarks>` block above `TargetFilePath`: "Not mutated after Start; for the resolved file path, see the result." plus `<see cref="ProcessResult.ExecutedFilePath"/>` [`#T012`].

### `src/CliInvoke.Core/Processes/IExternalProcess.cs`

- Change `ProcessConfiguration Configuration { get; set; }` to `ProcessConfiguration Configuration { get; init; }` [`#T010`]. The interface change ripples to all implementations.
- Verify `ISuspendableExternalProcess` (and any other `IExternalProcess`-implementing types) align; no setter overrides should exist after the change [`#T010`].

### `src/CliInvoke/Processes/ExternalProcess.cs`

- Change `public ProcessConfiguration Configuration { get; set; }` to `public ProcessConfiguration Configuration { get; init; }` [`#T010`].
- Update all three ctors to pass `Configuration` as-is (init-only); drop the `configuration.ResourcePolicy` separate-arg pattern to `ProcessWrapper` [`#T003`].
- In `Start()`: remove the `Configuration.TargetFilePath = filePath.FullName;` write-back (line 127) [`#T001`/`#D002`/`#T004`]. Resolve via `_filePathResolver.ResolveFilePath(Configuration.TargetFilePath)` and pass to a fresh `ProcessWrapper(Configuration, resolvedFilePath)` [`#T001`/`#T002`/`#T003`/`#T007`].
- In `StartAsync(CancellationToken)`: replace the `await StartAsync(Configuration, cancellationToken);` redirect with direct field-based resolution; the redirect still resolves the field but creates indirection that obscures intent [`#T008`].
- In `StartAsync(ProcessConfiguration, CancellationToken)`: remove the `Configuration.TargetFilePath = filePath.FullName;` write-back (line 185) [`#T001`/`#T004`/`#T008`]. Resolve `configuration.TargetFilePath` (the parameter) and pass to a fresh `ProcessWrapper(configuration, resolvedFilePath)` [`#T002`/`#T003`/`#T008`].
- Add `<remarks>` blocks above `Start`, `StartAsync(CancellationToken)`, and `StartAsync(ProcessConfiguration, CancellationToken)`: "Configuration is not mutated; the resolved file path is returned via the result." plus `<see cref="ProcessResult.ExecutedFilePath"/>` [`#T012`].

### `src/CliInvoke/Processes/Internal/ProcessWrapper.cs`

- Change ctor signature from `ProcessWrapper(ProcessConfiguration configuration, ProcessResourcePolicy? resourcePolicy)` to `ProcessWrapper(ProcessConfiguration configuration, FileInfo resolvedFilePath)` [`#T002`/`#T003`]. Drop the `resourcePolicy` parameter; the policy is sourced via `configuration.ResourcePolicy` only [`#T003`].
- After `ProcessControlAdapter.ApplyConfiguration(this, configuration);` (line 60), add `StartInfo.FileName = resolvedFilePath.FullName;` to override the adapter's `processConfiguration.TargetFilePath` write [`#T007`].
- The internal `ResourcePolicy` property stays (still consumed by `SetResourcePolicy`); verify no other call site needs the redundant parameter [`#T003`].

### `src/CliInvoke.Specializations/Configurations/PowershellProcessConfiguration.cs`

- Drop the `IFilePathResolver filePathResolver` parameter from the ctor [`#T006`].
- Drop the ctor body that calls `filePathResolver.ResolveFilePath("pwsh.exe"/"pwsh")` at construction time; resolution moves to `ExternalProcess` [`#T001`/`#T006`].
- Pass `OperatingSystem.IsWindows() ? "pwsh.exe" : "pwsh"` as the `targetFilePath` arg to the base ctor — just the executable name [`#D004`].
- The `GetInstallLocationOnWindows()` static helper is no longer reachable from the ctor; either delete it or leave as a public extension for advanced Windows lookup [`#T006`]. Default: delete; behaviour for pwsh-not-in-PATH surfaces as `FileNotFoundException` at `Start()` time.

### `src/CliInvoke.Specializations/Invokers/PowershellProcessInvoker.cs`

- Drop the `IFilePathResolver filePathResolver` parameter from the ctor [`#T009`].
- Update the inner `new PowerShellMiddleware(filePathResolver)` to `new PowerShellMiddleware()` [`#T009`].

### `src/CliInvoke.Specializations/Middleware/PowerShellMiddleware.cs`

- Drop the `IFilePathResolver? filePathResolver = null` parameter from the ctor [`#T009`].
- Replace the `_filePathResolver ?? new CliInvoke.FilePathResolver();` lazy-init with `new CliInvoke.FilePathResolver();` directly; this preserves behaviour for callers that did not previously pass a resolver [`#T009`].
- Update the `new PowershellProcessConfiguration(_filePathResolver, ...)` ctor call (line 90) to `new PowershellProcessConfiguration(...)` (no resolver) [`#T006`/`#T009`].

### `src/CliInvoke.Extensions/DependencyInjection/FilePathResolverRegistration.cs`

- Update any DI extensions that register `IFilePathResolver` so they no longer pass it into `PowershellProcessInvoker` or `PowerShellMiddleware` ctors [`#T009`]. The default `FilePathResolver` allocation now lives inside `PowerShellMiddleware`.

### `tests/CliInvoke.Tests/.../ExternalProcessNoMutationTests.cs` (new file)

- New unit test [`#T005`/`#T011`]: construct an `ExternalProcess` with `"dotnet"` as `Configuration.TargetFilePath`, invoke `Start()` and `StartAsync()`. Assert (a) `Configuration.TargetFilePath` is unchanged from `"dotnet"`, and (b) the result's `ExecutedFilePath` equals the resolved `dotnet` binary path returned by `IFilePathResolver`. Run under TUnit per `AGENTS.md §Testing`. The fast-exiting-process race is handled by the existing `ProcessWrapper` guard [`#T011`].

### `docs/decisions/MIGRATION-externalprocess-config-seam.md` (new file, default location)

- New migration guide [`#T013`]: walks consumers through the no-mutation contract, the init-only setters (`ProcessConfiguration.TargetFilePath`, `ExternalProcess.Configuration` per [`#T004`]/[`#T010`]), and the removed constructor parameters (`PowershellProcessConfiguration` per [`#T006`], `PowershellProcessInvoker`/`PowerShellMiddleware` per [`#T009`]). Includes before/after code samples. Linked from `CHANGELOG.md`.

### `CHANGELOG.md`

- Add a `### BREAKING` entry (or equivalent marker) inside the v3 pre-release section that links to the migration guide [`#T013`]. Update the package version per `AGENTS.md §Versioning`; the major semver bump is automatic on v3 stable per the v3 context [`#T013`].

## Open / deferred

- `<!-- next-d: D005 -->` and `<!-- next-i: I004 -->` — placeholder sentinels; no clarifying interaction or design branch is currently open.
- Migration guide location (`docs/decisions/`, `site/docs/`, or `README.md`) is deferred to implementation per [`#T013`] Constraints.
- Whether to ship the change in v3-pre-release now or wait for v3-stable is deferred (v3 is in pre-release; the user has not yet decided the cut).
- `ExternalProcess.StartAsync(Configuration, cancellationToken)` redirect from the parameterless overload: behaviour under [`#T008`] is now correct-by-construction (the inner overload resolves the field via its parameter), so the redirect stays; verify in tests.

## Ledger Reference

- `DECISIONS-CliInvoke-externalprocess-config-seam.md#D001` — session goal
- `DECISIONS-CliInvoke-externalprocess-config-seam.md#D002` — remove internal `TargetFilePath` setter; stop mutating caller's Configuration
- `DECISIONS-CliInvoke-externalprocess-config-seam.md#D003` — document no-mutation at call site
- `DECISIONS-CliInvoke-externalprocess-config-seam.md#D004` — Specializations provide executable name per platform; `ExternalProcess` resolves
- `DECISIONS-CliInvoke-externalprocess-config-seam.md#T001` — always resolve at Start
- `DECISIONS-CliInvoke-externalprocess-config-seam.md#T002` — `ProcessWrapper` ctor takes resolved path
- `DECISIONS-CliInvoke-externalprocess-config-seam.md#T003` — `ProcessWrapper` ctor signature `(ProcessConfiguration, FileInfo)`; drop policy param
- `DECISIONS-CliInvoke-externalprocess-config-seam.md#T004` — `TargetFilePath` is `init`-only
- `DECISIONS-CliInvoke-externalprocess-config-seam.md#T005` — unit test on `ExternalProcess`
- `DECISIONS-CliInvoke-externalprocess-config-seam.md#T006` — drop `IFilePathResolver` from `PowershellProcessConfiguration` ctor
- `DECISIONS-CliInvoke-externalprocess-config-seam.md#T007` — override `StartInfo.FileName` in `ProcessWrapper` ctor after `ApplyConfiguration`
- `DECISIONS-CliInvoke-externalprocess-config-seam.md#T008` — `StartAsync(ProcessConfiguration, CT)` resolves parameter; field untouched
- `DECISIONS-CliInvoke-externalprocess-config-seam.md#T009` — drop `IFilePathResolver` from `PowershellProcessInvoker` / `PowerShellMiddleware` ctors
- `DECISIONS-CliInvoke-externalprocess-config-seam.md#T010` — `ExternalProcess.Configuration` setter is `init`-only
- `DECISIONS-CliInvoke-externalprocess-config-seam.md#T011` — `dotnet --info` test stub
- `DECISIONS-CliInvoke-externalprocess-config-seam.md#T012` — brief `<remarks>` + cref at call site
- `DECISIONS-CliInvoke-externalprocess-config-seam.md#T013` — migration guide (v3 context)
