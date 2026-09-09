# Changelog

All notable changes to CliInvoke are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/).

## [3.0.0] - 2026-09-09

CliInvoke 3.0.0 is the first stable release of the v3 line. It ships the
design-smell triage as one coherent breaking-change set.

Themes:

- `CliRun` is now a stateless, batteries-included defaults facade.
- The public surface of `ExternalProcess`, `ProcessInvoker`, and
  `ProcessConfigurationBuilder` is tightened and sealed where appropriate.
- `ProcessResult` equality is made symmetric across the result hierarchy.
- Platform-specific process control adapters, output-truncation middleware,
  retry middleware, caching file-path resolver, and exit validation primitives
  are now first-class.

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
- **`null == null` now returns `true` for result, configuration, and
  exception-info types.** `ProcessResult`, `BufferedProcessResult`,
  `ProcessConfiguration`, and `ProcessExceptionInfo<TProcessResult>` equality
  operators now implement standard null-safe semantics: `null == null` is `true`,
  `null == x` is `false`, and the operators never throw. Previously `null == null`
  could throw or return `false` depending on the type.
- **`BufferedProcessResult` equality now includes `ProcessId`.** The
  `Equals` and `GetHashCode` overrides consider `ProcessId`, matching the base
  `ProcessResult` contract. Two results from the same command but different PIDs
  now compare unequal — this is a stricter but correct contract.
- **`UserCredential.GetHashCode` excludes `Password`.** The hash is computed
  from `Domain`, `UserName`, and `LoadUserProfile` only. `Password` still
  participates in `Equals` content comparison but never in the hash. Distinct
  passwords with the same user/domain may collide — this is contract-legal and
  documented. No `SecureString` unwrapping occurs in hash paths.
- **Truncation `0`-cap is now a valid zero-byte cap.** Passing `0` as the
  `maxStandardOutputBytes` or `maxStandardErrorBytes` cap now produces empty text
  with the `WasTruncated` flag set. Previously `0` was treated as "no cap"
  (same as `null`/negative). The three spellings are: `null` = no cap,
  negative = no cap, `0` = zero-byte cap.
- **Narrowed catch blocks surface previously swallowed failures.** Bare
  `catch (Exception)` blocks across `ShellDetector`, `ProcessWrapper`, and
  `ExternalProcess` have been narrowed to catch only their documented expected
  exceptions. Unexpected exceptions (including `OperationCanceledException` in
  the PowerShell-to-cmd fallback path) now propagate to the caller.
- **Unix admin/credential requests now throw `PlatformNotSupportedException`.**
  `UnixProcessControlAdapter.RequireRunningAsAdmin` and
  `UnixProcessControlAdapter.SetUserCredential` (with a non-null credential) now
  throw `PlatformNotSupportedException` instead of silently no-oping. This makes
  the cross-platform contract honest — callers on Unix who relied on the silent
  no-op must handle or guard the exception. Windows behaviour is unchanged.
- **`Configuration` is init-only on `IExternalProcess` and `ExternalProcess`.**
- **`TargetFilePath` is init-only** and the `ProcessWrapper` constructor adjusted
  accordingly.
- **`IFilePathResolver` dropped** from `PowershellProcessConfiguration` constructor,
  `PowershellProcessInvoker`, and `PowerShellMiddleware`.
- **`BuilderProcessConfiguration` bridge subclass deleted.**
- **`ProcessConfigurationFactory` collapsed** to two static spec-callback overloads.
- **`PipedProcessResult` and the `Piped` invocation path removed.**
- **`CmdProcessInvoker` and `PowershellProcessInvoker` wrappers deleted.**
- **`FilePathResolverBase` class removed.**
- **`InvocationMode` enum removed.**
- **`IDisposable` dropped from `ProcessConfiguration`.**

### Additions

- Output truncation middleware with configurable size limits and custom handlers.
- `CachingFilePathResolver` and DI registration extensions for file-path caching.
- Retry middleware (`UseRetryPolicy`) with configurable policies, linear backoff, and delay clamping.
- `IRetryClassifier` abstraction for custom retry decisions.
- `ValidationRule` primitive for post-exit process validation.
- `RetryBackoffStrategy.Linear` option; retry delay clamped to `Task.Delay` maximum.
- `Canceled` and `Signal` properties on `ProcessResult` for cancellation and signal handling.
- `GetTerminatingSignal` control-adapter heuristic.
- Platform-specific process control adapters (`UnixProcessControlAdapter`, `WindowsProcessControlAdapter`).
- `ProcessValidationException` type.
- `IProcessResultValidator` and `CommonValidationRules` for exit validation.
- `ProcessExitConfiguration` for exit-code and signal validation.
- Localization resources in `CliInvoke` (`Resources.resx` / `Resources.Designer.cs`).
- `maxBufferedOutputBytes` on `CliRun` string-args buffered path for capping buffered output size.

### Modifications

- `CliInvoke.Extensions` folded into the main `CliInvoke` package.
- Dead `InternalsVisibleTo` grants across `Core` and `Specializations` stripped out.
- Solution layout cleaned: `Middleware` folder removed, `Extensions.Tests` wired.
- Process launch and logging paths made more robust.
- `AddEnumerable` now fails fast on null entries.
- `BufferedProcessResult.WasTruncated` made immutable and included in equality.
- Shell argument escaping tightened; `ShellArgumentEscaper` relocated to `Specializations`.
- `PathEnvironmentVariable` moved to `CliInvoke`; `FilePathResolverBase` dropped.
- `ArgumentsSpec` internals reworked.
- `UseRetryPolicy` DI registration fixed to decorate the active (last) `IFilePathResolver` registration.
- Default `IProcessResultValidator.ShouldRetry` inverted to `!Validate(result)`.
- Retry delay clamped to `Task.Delay` maximum; negative `RetryOptions.BaseDelay` and `MaxAttempts` below 1 now rejected.
- Per-call allocations eliminated in result parsing and argument building.
- LINQ usage removed from `ProcessConfiguration` to cut allocations.
- Extension types relocated into `src/CliInvoke/Extensions` tree.
- `PipedProcessResult` references scrubbed from docs and skills.
- `IFilePathResolver`, `IProcessMiddleware`, and `MiddlewareChain` interfaces updated; `MiddlewareChain` now non-nullable.
- `ConfigureAwait(false)` on every await site in `src`. A package-free CI guard enforces this policy on every build.
- Cancellation-reason race eliminated: `WaitForExitOrForcefulTimeoutAsync` and `CancelWithInterrupt` no longer use `CancellationToken.Register` callbacks.
- `ExternalProcess` lifecycle synchronization: start gate and `_processWrapper` swap serialized under a private lock.
- `ShellDetector` Unix flow uses cancellation checkpoints.
- `ForcefulExit` self-guards on exited processes.
- UTF-8 boundary truncation uses incremental decoding.
- Dead-code removal across `UnixProcessControlAdapter`, `WindowsProcessControlAdapter`, and `ProcessWrapper`.
- Started event null-guard on `ProcessWrapper`.
- `$HOME` stale-index fix in `CachingFilePathResolver.EnumerateDirectories`.
- `CachingFilePathResolver` key normalization per-OS casing rules.
- `OutputTruncationMiddleware` reads truncation cap from exit configuration; `MaxSize` renamed to `MaxBytes`.
- Retry middleware reworked around the classifier.
- `MiddlewareItems` internals reworked for faster lookups.
- Reduced constructor code duplication in `ExternalProcess`.
- Extracted duplicated `CliRun` code into a shared helper.

### Bug fixes

- Fixed `ProcessResult` equality asymmetry and audited the result subclasses (`BufferedProcessResult`, `PipedProcessResult`).
- Fixed an issue with the `BufferedProcessResult.Equals` method.
- Fixed `ExternalProcess` to resolve the file path at `Start`/`StartAsync` without mutating the provided `Configuration`.
- Fixed `ExternalProcess.StartAsync(config, ct)` to dispose the old wrapper and reattach event handlers.
- Fixed code smells: null safety, an inverted condition, null equality, a dead override, and dictionary equality.
- Sorted environment variables by key in `GetHashCode` for ordering independence.
- `ShellInformation.GetHashCode` now hashes the target file path
  case-insensitively, matching its `Equals` comparison. Instances whose paths
  differ only in letter casing are equal and no longer land in different hash
  buckets when used as dictionary or set keys.
- Fixed the expected path in the `Resolve_CrossPlatform_PathEnv_Executable` test.
- `Canceled` no longer reports `false` after graceful timeout cancellation.
- Fixed the `AllowExceptionsIfUnexpected` cancellation window to measure actual
  exit skew: the expected exit time is now captured when the wait begins instead
  of being re-derived inside the exception handlers. Timeout cancellations that
  resolve on time are swallowed, and genuine failures that resolve more than
  1 second from the expected exit time are re-thrown. Previously the difference
  was computed against a re-derived expected exit time, so it matched the
  configured timeout threshold and exceptions were re-thrown for any timeout
  longer than 1 second.
- Deadlock resolved: buffered/piped capture now starts without awaiting process exit.
- `LocateFileFromDirectory` rechecks resolved `FileInfo` existence before returning.
- POSIX argument escaping fixed: `EscapeInner` double-backslashes before quotes and emits POSIX backslashes literally for correct round-tripping.
- Stale escaping expectations in `ProcessConfigurationBuilderTests` corrected.
- Argument-escaping assertions now platform-aware.
- `ProcessValidationException` constructors fixed.
- `UserCredential` constructor and validation rules fixed.
- Retry delay overflow is caught and clamped to `Task.Delay` maximum.
- `RetryMiddleware` XML docs now honestly state that retries apply to classifier-approved results and that pipeline exceptions propagate without retry.
- `FromProcessStartInfo` XML docs document the per-stream redirect-flag collapse via OR into the single `OutputRedirection` flag.
- Validator registration methods use `Add{Lifetime}` after `RemoveAll` with XML docs stating the single-threaded registration convention.
- `FilePathResolver` Unix filename match now compares `f.Name` (extracted via `Path.GetFileName`) on both platforms with documented inferred-directory semantics for relative subpaths.
- A race in `ExternalProcess` where the start gate and wrapper swap could interleave is fixed (both serialized under a private lock).
- Fixed flaky resource policy application on fast-exiting processes.
- Fixed validator registration extensions to use `Add` instead of `TryAdd` so re-registration works.
- Fixed `CachingFilePathResolver` cache keys to respect per-OS path casing rules.
- Fixed a Unix `FilePathResolver` filename match issue.
- Fixed `PathEnvironmentVariable` to expand `~` and `C:\Users` paths in a single pass.
- Fixed null-unsafe equality operators on primitives.
- Fixed a duplicate in `GetHashCode` in `ProcessExitConfiguration`.

### Non-source code

- `README.md`, `GLOSSARY.md`, and `CONTRIBUTING.md` updated.
- ADR 0001 (IVT-minimization principle) and ADR 0002 (why not CliWrap) added.
- Migration guides for 3.0.0 and v1-to-v2 refreshed.
- Getting-started and architecture documentation refreshed.
- Middleware, configuration, and troubleshooting guides updated.
- CI guard enforces `ConfigureAwait(false)` on all await sites in `src`.
- Release notes and README updated with all breaking changes and behaviour changes.
- External-process config-seam migration document moved to `docs/`.
- Added missing license notices to example source files.
- Updated stale documentation (benchmarks README, supported OS, building guide, architecture and configuration guides).
- Fixed the getting-started docs: bumped the package version to 3.0.0 and corrected a non-compiling `WorkingDirectoryPath` example.
- Fixed the `ProcessInvoker` constructor signature in the middleware docs.
- Fixed stale v1 API references across documentation and READMEs.
- Rewrote agent skill descriptions for consistency.
- Removed `Dxxx` decision-ledger citations from documentation and tests, and from `GLOSSARY.md`.
- Added the 3.0.0 breaking-changes changelog and migration guide.
- Fixed a code sample in Section 6 of the migration guide.
- Added a migration guide for the `ExternalProcess` no-mutation contract.
- Made agent skills consistent for the 3.0 targeting note and reconciled the credential API.
- Added agent skills targeting the 3.0 API (with evaluation task sets).
- Modernized the `WCountLib.Providers.wc` example for CliInvoke v3 (relicensed to MIT).

##### Runtime Dependencies

- `Microsoft.Extensions.Caching.Memory` 10.0.11 added to `Directory.Packages.props` for the new `CachingFilePathResolver`.

##### CI Dependencies

- Bumped `github/codeql-action/upload-sarif` from 4.37.7 to 4.37.8 in the Scorecard workflow.

[3.0.0]: https://github.com/alastairlundy/CliInvoke/releases
