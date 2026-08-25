# Changelog

All notable changes to CliInvoke are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/).

## [3.0.0-alpha] - 3.0.0 pre-release line

The 3.0.0-alpha line ships the design-smell triage as one coherent breaking
change set. Themes:

- `CliRun` is now a stateless, batteries-included defaults facade.
- The public surface of `ExternalProcess`, `ProcessInvoker`, and
  `ProcessConfigurationBuilder` is tightened and sealed where appropriate.
- `ProcessResult` equality is made symmetric across the result hierarchy.

> **Migration target for removed APIs:** callers of the removed `CliRun.Use*`
> methods, the removed `ProcessInvoker`/`ExternalProcess` constructors, and the
> `ExitConfiguration` setter should move to `IProcessInvoker` (or resolve one
> from the DI container). A consolidated guide is available in
> [Migrating to 3.0.0](site/docs/migration-guides/3.0.0.md).

### Breaking changes

- **`CliRun` static mutable state removed.** `CliRun.UseExternalProcessFactory`
  and `CliRun.UseFilePathResolver` no longer exist. `CliRun` retains only its
  `Run*`/`FireAndForget` methods, and each call now allocates a fresh
  `ProcessInvocationPipeline` (with a fresh `ExternalProcessFactory` and default
  `FilePathResolver`) per call. There is no process-wide configurable state to
  leak between calls. Callers needing a custom factory or resolver must use
  `IProcessInvoker` (or DI) instead of `CliRun`. No `[Obsolete]` shim or bridge
  method was added (direct cutover).
- **`InvocationContext.Result` / `.Middleware` ownership documented (no API change).**
  These properties are now documented as owned by specific middleware: the only
  legitimate mutators are the `MiddlewareChain` walker, the terminal delegate
  that bridges the chain to the pipeline, and any propagating middleware that
  short-circuits the chain. Caller code must not read or write them outside
  those mutators. This is a documentation-only change; the setters are retained.
- **`ExitConfiguration` is now read-only.** `IExternalProcess.ExitConfiguration`
  and `ExternalProcess.ExitConfiguration` are read-only `{ get; }` properties
  supplied at construction time. The `ExitConfiguration` setter and any
  `WithExitConfiguration(...)` method do not exist. Construct `ExternalProcess`
  with the exit configuration via its constructor.
- **`ProcessInvoker` reduced to two constructors.** The partial overloads
  `(IExternalProcessFactory, MiddlewareItems?)` and
  `(IExternalProcessFactory, IEnumerable<IProcessMiddleware>)` were removed.
  The surviving constructors are `(IExternalProcessFactory)` and
  `(IExternalProcessFactory, IEnumerable<IProcessMiddleware>, MiddlewareItems?)`.
- **`ExternalProcess` keeps only constructor C.** Constructors
  `(IFilePathResolver, string)` and `(ProcessConfiguration, ProcessExitConfiguration?)`
  were removed. `ExternalProcess` is now constructed with
  `(IFilePathResolver, ProcessConfiguration, ProcessExitConfiguration?)`.
- **`ExternalProcess` is sealed.** The class is now `sealed`; no
  public or protected extension points remain.
- **`ProcessConfigurationBuilder` is sealed.** The concrete builder is now
  `sealed`. Fluent chaining via the `IProcessConfigurationBuilder` interface is
  unaffected.
- **`ProcessResult` equality is symmetric.** `ProcessResult.Equals(object?)`
  now uses exact runtime-type matching so that `a.Equals(b) == b.Equals(a)` holds
  across the `ProcessResult` hierarchy. `BufferedProcessResult` and
  `PipedProcessResult` were audited to satisfy the same symmetry contract.
  `ProcessResult` is intentionally **not** sealed in this release (sealing is
  deferred).

[3.0.0-alpha]: https://github.com/alastairlundy/CliInvoke/releases

## Changes since 3.0.0-alpha.9

### All Packages

#### Additions

- Added localization resources to `CliInvoke` (`Resources.resx` / `Resources.Designer.cs`).
- Added a unit test asserting the no-mutation contract on `ExternalProcess`.
- Added a test for the `ExternalProcess.StartAsync(ProcessConfiguration, CancellationToken)` overload.

#### Modifications

- Stripped `CliRun` of static mutable state; `CliRun` is now a stateless defaults facade (breaking change).
- Reduced `ProcessInvoker` to two constructors (breaking change).
- Tightened the `ExternalProcess` public API (breaking change).
- Sealed `ProcessConfigurationBuilder` (breaking change).
- Made `Configuration` init-only on `IExternalProcess` and `ExternalProcess` (breaking change).
- Made `TargetFilePath` init-only and adjusted the `ProcessWrapper` constructor accordingly (breaking change).
- Dropped `IFilePathResolver` from the `PowershellProcessConfiguration` constructor (breaking change).
- Dropped `IFilePathResolver` from `PowershellProcessInvoker` and `PowerShellMiddleware` (breaking change).
- Changed the 15-parameter `ProcessConfiguration` constructor visibility from `protected` to `protected internal`.
- Collapsed `ProcessConfigurationFactory` to two static spec-callback overloads and migrated 21 params-overload call sites accordingly.
- Reworked `MiddlewareItems` internals for faster lookups.
- Removed LINQ usage from `ProcessConfiguration` to reduce allocations and improved `ProcessConfiguration.Equals` performance.
- Reduced constructor code duplication in `ExternalProcess` and removed unnecessary casting code from `ProcessConfigurationBuilder` and `RunnerConfigurationFactory`.
- Extracted duplicated `CliRun` code into a shared helper.
- Made `MiddlewareChain` non-nullable in `ProcessInvoker` and tightened null checks in `ProcessConfigurationFactory`.
- Added XML doc comments to `ProcessConfiguration` and corrected XML documentation/remarks in `ProcessTimeoutPolicy`, `TargetFilePath`, `BufferedProcessResult`, and `PipedProcessResult`.
- Documented the `InvocationContext.Result` / `.Middleware` ownership contract and replaced stale TODOs in `ProcessInvocationPipeline` and `CliRun.GetPipeline` with design notes.
- Resolved stale TODOs in `PipedProcessResult`; refreshed the `WCountLib.Providers.wc` example, `GlobalUsings.cs`, and `CliInvokeExamples.slnx`.
- Modernized the `WCountLib.Providers.wc` example for CliInvoke v3 (relicensed to MIT).
- Updated mutation tests to use unresolved executable names.

#### Removals

- Deleted the `BuilderProcessConfiguration` bridge subclass (breaking change).

#### Bug fixes

- Fixed `ProcessResult` equality asymmetry and audited the result subclasses (`BufferedProcessResult`, `PipedProcessResult`).
- Fixed an issue with the `BufferedProcessResult.Equals` method.
- Fixed `ExternalProcess` to resolve the file path at `Start`/`StartAsync` without mutating the provided `Configuration`.
- Fixed `ExternalProcess.StartAsync(config, ct)` to dispose the old wrapper and reattach event handlers.
- Fixed code smells: null safety, an inverted condition, null equality, a dead override, and dictionary equality.
- Sorted environment variables by key in `GetHashCode` for ordering independence.
- Fixed the expected path in the `Resolve_CrossPlatform_PathEnv_Executable` test.

#### Non-source code

- Moved the external-process config-seam migration document to `docs/`.
- Added missing license notices to example source files.
- Updated stale documentation (benchmarks README, supported OS, building guide, architecture and configuration guides).
- Fixed the getting-started docs: bumped the package version to 3.0.0 and corrected a non-compiling `WorkingDirectoryPath` example.
- Updated `AGENTS.md`.
- Fixed the `ProcessInvoker` constructor signature in the middleware docs.
- Fixed stale v1 API references across documentation and READMEs.
- Rewrote agent skill descriptions for consistency.
- Removed `Dxxx` decision-ledger citations from documentation and tests, and from `GLOSSARY.md`.
- Added the 3.0.0 breaking-changes changelog and migration guide.
- Fixed a code sample in Section 6 of the migration guide.
- Added a migration guide for the `ExternalProcess` no-mutation contract.
- Made agent skills consistent for the 3.0 targeting note and reconciled the credential API.
- Added agent skills targeting the 3.0 API (with evaluation task sets).

##### Runtime Dependencies

- Bumped the `CliInvoke` / `CliInvoke.Core` package references in `examples/Directory.Packages.props` from `3.0.0-alpha.7` to `3.0.0-alpha.9`.

##### CI Dependencies

- Bumped `github/codeql-action/upload-sarif` from 4.37.7 to 4.37.8 in the Scorecard workflow.
