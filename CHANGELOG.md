# Changelog

All notable changes to CliInvoke are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/).

> For releases prior to 2.0, see [CHANGELOG-archive.md](CHANGELOG-archive.md).

## [Unreleased]

CliInvoke 3.0.0 is the first stable release of the v3 line (unreleased). It ships the
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

### Added

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

### Changed

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
- **`ProcessConfigurationFactory` collapsed** to two static spec-callback overloads.
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
- `README.md`, `GLOSSARY.md`, and `CONTRIBUTING.md` updated.
- ADR 0001 (IVT-minimization principle) and ADR 0002 (why not CliWrap) added.
- Migration guides for 3.0.0 and v1-to-v2 refreshed.
- Getting-started and architecture documentation refreshed.
- Middleware, configuration, and troubleshooting guides updated.
- CI guard enforces `ConfigureAwait(false)` on all await sites in `src`.
- Release notes and README updated with all breaking changes and behaviour changes.
- External-process config-seam migration document absorbed into `site/docs/migration-guides/3.0.0.md` §9; standalone file removed.
- Removed stale `3.0.0-beta` / `pre-release` labels from README, AGENTS.md, comparison table, and Specializations README.
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
- `Microsoft.Extensions.Caching.Memory` 10.0.11 added to `Directory.Packages.props` for the new `CachingFilePathResolver`.
- Bumped `github/codeql-action/upload-sarif` from 4.37.7 to 4.37.8 in the Scorecard workflow.

### Removed

- **`CliRun` static mutable state removed.** `CliRun.UseExternalProcessFactory`
  and `CliRun.UseFilePathResolver` no longer exist. `CliRun` retains only its
  `Run*`/`FireAndForget` methods, and each call now allocates a fresh
  `ProcessInvocationPipeline` (with a fresh `ExternalProcessFactory` and default
  `FilePathResolver`) per call. There is no process-wide configurable state to
  leak between calls. Callers needing a custom factory or resolver must use
  `IProcessInvoker` (or DI) instead of `CliRun`. No `[Obsolete]` shim or bridge
  method was added (direct cutover).
- **`ExternalProcess` keeps only constructor C.** Constructors
  `(IFilePathResolver, string)` and `(ProcessConfiguration, ProcessExitConfiguration?)`
  were removed. `ExternalProcess` is now constructed with
  `(IFilePathResolver, ProcessConfiguration, ProcessExitConfiguration?)`.
- **`BuilderProcessConfiguration` bridge subclass deleted.**
- **`PipedProcessResult` and the `Piped` invocation path removed.**
- **`CmdProcessInvoker` and `PowershellProcessInvoker` wrappers deleted.**
- **`FilePathResolverBase` class removed.**
- **`InvocationMode` enum removed.**
- **`IDisposable` dropped from `ProcessConfiguration`.**

### Fixed

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

## [2.11.1] - 2026-09-10

### Fixed

- Fixed an equality comparison bug in ``ProcessConfiguration`` ``Equals`` method
- Fixed equality operators so that they handle null comparisons correctly
- Fixed``ShellDetector`` shell version parsing for non bash based shells
- Fixed potentially inaccurate shell detection in ``ShellDetector`` on Unix based operating systems
- Fixed internal ``ProcessWrapper`` class throwing the wrong exception in some cases such as Access being denied or file not being found
- Fixed ``ExternalProcess`` not catching expected exceptions on pipe reads after process exits
- Fixed potetial issues of pipe reads hanging in ``ExternalProcess``
- Fixed null-safety in ProcessExceptionInfo.Equals for its ``UserCredential`` object named ``Credential``.
- Cleaned up ``PowershellProcessConfiguration`` executable lookup

### Changed

- Updated to use CliInvoke.Core 2.10.6 and CliInvoke 2.10.6
- Updated ``Microsoft.Extensions.DependencyInjection.Abstractions`` from 10.0.11 to 10.0.12

## [2.11.0] - 2026-09-02

### Changed

- General Improvements
- Updated to use CliInvoke.Core 2.11.0
- Improved ``FilePathResolver`` path resolution performance
- Improved ``ShellDetector`` detection performance
- Updated to use CliInvoke.Core 2.11.0 and CliInvoke 2.11.0
- Updated to use CliInvoke.Core 2.11.0

### Deprecated

- Deprecated ``PipedProcessResult`` ahead of removal in v3
- Deprecated some extensions for removal in v3
- Deprecated ``PowershellProcessInvoker`` and ``CmdProcessInvoker`` ahead of removal in v3

## [2.10.6] - 2026-09-10

### Fixed

- Fixed an equality comparison bug in ``ProcessConfiguration`` ``Equals`` method
- Fixed equality operators so that they handle null comparisons correctly
- Fixed``ShellDetector`` shell version parsing for non bash based shells
- Fixed potentially inaccurate shell detection in ``ShellDetector`` on Unix based operating systems
- Fixed internal ``ProcessWrapper`` class throwing the wrong exception in some cases such as Access being denied or file not being found
- Fixed ``ExternalProcess`` not catching expected exceptions on pipe reads after process exits
- Fixed potetial issues of pipe reads hanging in ``ExternalProcess``
- Fixed null-safety in ProcessExceptionInfo.Equals for its ``UserCredential`` object named ``Credential``.
- Cleaned up ``PowershellProcessConfiguration`` executable lookup

### Changed

- Updated to use CliInvoke.Core 2.10.6 and CliInvoke 2.10.6
- Updated ``Microsoft.Extensions.DependencyInjection.Abstractions`` from 10.0.11 to 10.0.12

## [2.10.5] - 2026-08-30

### Fixed

- Fixed shell metacharacter escaping in RunnerProcessFactory. Added tests covering the new escaping behavior.

### Changed

- Updated to use CliInvoke.Core 2.10.5
- Updated to use CliInvoke.Core 2.10.5 and CliInvoke 2.10.5
- Updated to use CliInvoke.Core 2.10.5

## [2.10.4] - 2026-08-26

### Changed

- General Improvements
- Updated to use CliInvoke.Core 2.10.4
- Improved security when running processes creating using ``RunnerProcessFactory``
- Updated to use CliInvoke.Core 2.10.4 and CliInvoke 2.10.4
- Updated to use CliInvoke.Core 2.10.4
- Improved security when running processes via Specialization powered configurations and invokers.

## [2.10.3] - 2026-08-19

### Fixed

- Fixed ``ProcessConfiguration.Equals`` to correctly compare ``RequiresAdministrator`` and ``WindowCreation`` properties instead of comparing ``ResourcePolicy`` twice, and added both properties to ``GetHashCode`` for consistency
- Fixed ``RunnerProcessFactory.CreateRunnerConfiguration`` to chain ``RequireAdministratorPrivileges()`` on the existing builder instead of replacing it with a fresh one, preserving all accumulated configuration when admin privileges are required

### Changed

- Updated to use CliInvoke.Core 2.10.3 and CliInvoke 2.10.3
- Updated to use CliInvoke.Core 2.10.3

## [2.10.2] - 2026-08-15

### Fixed

- Corrected the upper bound validation for processor affinity.
- Centralized the processor affinity upper bound and made the related tests processor-count-aware.
- Replaced `nint`/`IntPtr.MaxValue` with a netstandard2.0-compatible computation in the processor affinity policy.

### Changed

- Updated Polyfill version from 11.0.1 to 11.2.0
- Updated to use CliInvoke.Core 2.10.2
- Updated DotExtensions from 10.5.0 to 10.5.1
- Updated to use CliInvoke.Core 2.10.2 and CliInvoke 2.10.2
- Updated to use CliInvoke.Core 2.10.2

## [2.10.1] - 2026-08-05

### Fixed

- Fixed potential race conditions in ``ProcessWrapper``'s ``Start`` method for fast exiting processes.
- Fixed an issue where ``WaitForExitAsync`` waiting for End Of File would block indefinitely on piped outputs

### Changed

- Updated to use CliInvoke 2.10.1

## [2.10.0] - 2026-07-31

### Added

- Added a constructor overload that accepts ``IEnumerable<string>`` in ``ProcessConfiguration``.

### Changed

- Reduced code duplication in ``ProcessConfiguration`` constructors.
- Improved formatting in ``ProcessConfiguration``.
- Updated ``ProcessConfiguration``.
- Updated DotExtensions from 10.4.0 to 10.5.0.

## [2.9.4] - 2026-08-30

### Security

- Security re-backport of 2.10.5 ArgumentList fix (CWE-78, CWE-88). The 2.9.3 escaping-based backport was proven still vulnerable: a double quote in caller arguments could break OS-level quoting and let the wrapped shell reassemble a second command.

### Changed

- RunnerProcessFactory composes the wrapped command as a list of separate argument tokens, and ToStartInfoExtensions delivers them through `ProcessStartInfo.ArgumentList` on .NET 8+ so the OS quotes each value independently and no caller value can alter how the wrapped command is split.
- Add `PolyUseEmbeddedAttribute=true` in CliInvoke.Specializations so each project's polyfill copy stays private (prevents CS0433/CS0121).
- Guard `[OverloadResolutionPriority(2)]` on the obsolete `FromStartInfo` with `#if NET9_0_OR_GREATER` in CliInvoke.Extensions.

### Fixed

- Port three admin-flag preservation regression tests from 2.10.5.

## [2.9.3] - 2026-08-30

### Fixed

- Fixed shell metacharacter escaping in RunnerProcessFactory. Added tests covering the new escaping behavior.

### Changed

- Updated to use CliInvoke.Core 2.9.3
- Updated to use CliInvoke.Core 2.9.3 and CliInvoke 2.9.3
- Updated to use CliInvoke.Core 2.9.3

## [2.9.2] - 2026-08-19

### Fixed

- Fixed `ProcessConfiguration.Equals` to correctly compare `RequiresAdministrator` and `WindowCreation` properties instead of comparing `ResourcePolicy` twice, and added both properties to `GetHashCode` for consistency
- Fixed `RunnerProcessFactory.CreateRunnerConfiguration` to chain `RequireAdministratorPrivileges()` on the existing builder instead of replacing it with a fresh one, preserving all accumulated configuration when admin privileges are required

### Changed

- Updated to use CliInvoke.Core 2.9.2 and CliInvoke 2.9.2
- Updated to use CliInvoke.Core 2.9.2

## [2.9.1] - 2026-08-15

### Fixed

- Corrected the upper bound validation for processor affinity.
- Centralized the processor affinity upper bound and made the related tests processor-count-aware.
- Replaced `nint`/`IntPtr.MaxValue` with a netstandard2.0-compatible computation in the processor affinity policy.

### Changed

- Updated Polyfill version from 11.0.1 to 11.0.2
- Updated to use CliInvoke.Core 2.9.1
- Updated DotExtensions from 10.4.0 to 10.5.1
- Updated to use CliInvoke.Core 2.9.1 and CliInvoke 2.9.1
- Updated to use CliInvoke.Core 2.9.1

## [2.9.0] - 2026-07-19

### Changed

- Updated Polyfill from 10.11.2 to 11.0.1
- Updated DotExtensions from 10.3.3 to 10.4.0
- Scaled the graceful-cancellation wait with the configured timeout, replacing the fixed 30-second `GracefulTimeoutWaitSeconds` constant with `CalculateGracefulTimeoutWaitSeconds(timeout)` (returns `min(10 + floor(timeout * 0.05), 20)`). A 10s timeout now waits 10s (was 30s); a 200s timeout is capped at 20s. The `GracefulCancel_InterruptSignals_Success` test bound is now derived from the same formula (wait + 5s, capped at 60s) so it stays in sync if the formula changes again.
- Updated Microsoft.Extensions.DependencyInjection.Abstractions from 10.0.9 to 10.0.10
- Fixed British English spelling in XML doc comment for AddCustomResultValidatorsExtensions ("customize" → "customise")

### Deprecated

- Deprecated FilePathResolver.Shared static property (marked [Obsolete] ahead of removal in v3)

## [2.8.5] - 2026-08-30

### Changed

- Replace `ArgumentCompositionHelper`-based command composition with direct `ArgumentTokenizer` usage so the wrapped command is built from discrete tokens and `ProcessStartInfo` delivers them through `ArgumentList`.
- Rename `ArgumentParser` to `ArgumentTokenizer` (matches 2.10.x naming).
- `RunnerProcessFactory` now tokenises runner args, the target path, and the caller's arguments separately, letting the OS quote each value independently when it composes the final command line.
- `ProcessConfiguration`'s first constructor initialises `ArgumentsList`; `Equals` covers `RequiresAdministrator` and `WindowCreation`.

### Removed

- Remove `ArgumentCompositionHelper` and its pre-quoting helpers.

## [2.8.4] - 2026-08-30

### Fixed

- Fixed shell metacharacter escaping in RunnerProcessFactory.

### Changed

- Updated to use CliInvoke.Core 2.8.4
- Updated to use CliInvoke.Core 2.8.4 and CliInvoke 2.8.4
- Updated to use CliInvoke.Core 2.8.4

## [2.8.3] - 2026-06-30

### Fixed

- Fixed ``ProcessTimeoutPolicy.Default`` so it uses the parameterless constructor (3 minutes) rather than being hardcoded to 30 minutes, aligning the static ``Default`` value with the documented default-constructor behavior.
- Fixed ``ProcessResourcePolicy`` constructor throwing an exception if ProcessorAffinity was set to 1 or less (minimum supported value is 1 (0x0001))
- Fixed an issue where the ``ProcessResourcePolicy`` constructor would not throw an Exception if ProcessorAffinity was set to a number greater than the 2x the ``Environment.ProcessorCount`` property
- Changed ``UserCredential.LoadUserProfile`` to default to ``null`` instead of ``false``, so that the builder-produced ``UserCredential`` matches the configuration's actual default

### Changed

- Updated ``.gitignore`` to exclude ``/waza-results`` and ``skills-lock.json``.
- Bumped ``Polyfill`` from ``10.8.1`` to ``10.11.2``.
- Updated to use CliInvoke.Core 2.8.3 and CliInvoke 2.8.3
- Updated to use CliInvoke.Core 2.8.3

## [2.8.2] - 2026-06-12

### Changed

- Updated ``.gitignore`` to include agent skills and Lunet build artifacts.
- Updated ``Polyfill`` from 10.7.0 to 10.8.1.
- Updated ``DotExtensions`` from 10.3.2 to 10.3.3.
- Updated ``Microsoft.Extensions.DependencyInjection.Abstractions`` from 10.0.8 to 10.0.9.

## [2.8.1] - 2026-05-31

### Fixed

- Fixed an issue where timeout threshold wasn't Reduced to 3 minutes
- Fixed a Dependency Injection ambiguous constructor issue in ``ProcessInvoker``
- Fixed an issue with ``ExternalProcess`` where ``WaitForBufferedExitOrTimeout`` would cause an exception to be thrown
- Fixed Dependency Injection registration issues with ``ProcessInvoker``
- Fixed a Dependency Injection ambiguous constructor issue in ``ProcessInvoker``

### Changed

- Updated to use CliInvoke.Core 2.8.1 and CliInvoke 2.8.1
- Updated to use CliInvoke.Core 2.8.0

## [2.8.0] - 2026-05-31

### Added

- Added ``WaitForPipedExitOrTimeoutAsync`` to ``IExternalProcess``
- Added ``WaitForPipedExitOrTimeoutAsync`` to ``ExternalProcess``

### Changed

- Reduced timeout threshold to 3 minutes
- Refactored ``ProcessInvoker`` class to internally use ``IExternalProcessFactory`` and ``IExternalProcess`` to reduce code duplication
- Updated to use CliInvoke.Core 2.8.0 and CliInvoke 2.8.0
- Updated to use CliInvoke.Core 2.8.0

## [2.7.1] - 2026-05-31

### Changed

- Update internal Polyfill version from 10.5.1 to 10.7.0
- Reduced private helper method code usage in ``ProcessInvoker``
- Updated to use CliInvoke.Core 2.7.1 and CliInvoke 2.7.1
- Updated to use CliInvoke.Core 2.7.1

## [2.7.0] - 2026-05-16

### Added

- Added a simpler overload to ``ProcessConfiguration``
- Added a simpler overload to `ProcessExitConfiguration`.
- Ported `CliRun` facade to `CliInvoke` 2.7 — CliRun is a new beginner-friendly entry point providing static RunAsync / Run*Async methods for simple process invocation without boilerplate (zero DI setup required, but allows optional DI injection of some services)

### Changed

- Backported README improvements and PATTERNS.md from v3.
- Updated test dependencies.
- General project cleanup and .gitignore updates.
- Reduced timeout threshold to 10 minutes - A future minor version may reduce this further
- Improve ProcessConfiguration ``ToString`` method performance
- Updated `ProcessWrapper` for improved handling.
- Addressed potential Resource Policy setting race condition.
- Updated to use CliInvoke.Core 2.7.0 and CliInvoke 2.7.0
- Updated to use CliInvoke.Core 2.7.0
- Minor xml doc comment tweaks

## [2.6.0] - 2026-04-26

### Added

- Added a new ProcessConfiguration constructor - This is intended to replace the original constructor

### Changed

- Reduced compiler warnings and disabled obsolete warnings
- Updated DotExtensions to version 10.3.0
- Switched to using DotExtensions' PATH Environment variable resolving code from DotPrimitives and removed dependency on DotPrimitives
- Added a static ``Shared`` instance to ``FilePathResolver``
- General formatting improvements and spelling corrections
- Updated to use CliInvoke.Core 2.6.0 and CliInvoke 2.6.0
- Updated to use CliInvoke.Core 2.6.0
- Added a test for ``PowershellProcessInvoker``

### Deprecated

- Deprecated ``IProcessConfigurationFactory`` - This will be removed in CliInvoke v3
- Deprecated the original ProcessConfiguration constructor - This will be removed in CliInvoke v3
- Deprecated ``ProcessConfigurationFactory`` - This is replaced by a static Factory class in CliInvoke v3

### Fixed

- Fixed an issue with ``PowershellProcessInvoker`` that would cause it to throw an ArgumentException upon instantiation - This will be backported to 2.5.x

## [2.5.4] - 2026-04-26

### Changed

- Updated internal Polyfill version from 10.1.1 to 10.3.0
- Updated DotExtensions version from 10.2.0 to 10.2.3
- Updated Microsoft.Extensions.DependencyInjection.Abstractions version from 10.0.5 to 10.0.7
- Updated to use CliInvoke.Core 2.5.4 and CliInvoke 2.5.4
- Updated to use CliInvoke.Core 2.5.4

## [2.5.3] - 2026-04-12

### Changed

- Updated internal Polyfill version from 9.23.0 to 10.1.1
- Removed deprecation of ``IFilePathResolver`` interface - The ``ResolveFilePath`` method signature in the interface is deprecated and will be replaced with a new method signature in CliInvoke v3
- Updated DotPrimitives version from 4.3.3 to 4.4.0
- Updated DotExtensions version from 10.1.1 to 10.2.0
- README fixes
- Removed deprecation of ``FilePathResolver`` class- The ``ResolveFilePath`` method signature in the class is deprecated and will be replaced with a new method signature in CliInvoke v3
- Updated to use CliInvoke.Core 2.5.3 and CliInvoke 2.5.3
- Updated to use CliInvoke.Core 2.5.3

## [2.5.2] - 2026-04-02

### Changed

- Updated internal Polyfill version from 9.22.0 to 9.23.0
- Updated DotExtensions version from 10.0.0 to 10.1.1
- Updated to use CliInvoke.Core 2.5.2 and CliInvoke 2.5.2
- Updated to use CliInvoke.Core 2.5.2

### Deprecated

- Deprecated ``IProcessPipeHandler`` for removal in CliInvoke v3
- Deprecated ``RunnerProcessInvokerBase`` for removal in CliInvoke v3
- Deprecated ``DefaultRunnerProcessInvoker`` for removal in CliInvoke v3
- Deprecated ``ProcessPipeHandler`` for removal in CliInvoke v3
- Deprecated ``AddDerivedRunnerProcessInvoker`` and  ``AddDefaultRunnerProcessInvoker`` methods for removal in CliInvoke v3

### Fixed

- Fixed a potential issue where Graceful cancellation on Windows would not free an allocated console leading to leaking memory
- Fixed possible cancellation related race conditions
- Fixed an issue where the wrong exception type was attempted to be caught in cancellation methods

## [2.5.1] - 2026-03-21

### Changed

- Updated AoT Compatibility property in ``CliInvoke.Core.csproj`` to only be set to true on .NET 8+
- Reduced warnings regarding supported and unsupported OSes in ``IProcessResourcePolicyBuilder``
- Updated AoT Compatibility property in ``CliInvoke.csproj`` to only be set to true on .NET 8+
- Reduced warnings regarding supported and unsupported OSes in ``ProcessResourcePolicyBuilder``
- Updated to use CliInvoke.Core 2.5.1 and CliInvoke 2.5.1
- Updated AoT Compatibility property in ``CliInvoke.Extensions.csproj`` to only be set to true on .NET 8+
- Updated AoT Compatibility property in ``CliInvoke.Specializations.csproj`` to only be set to true on .NET 8+

### Fixed

- Fixed a potential issue where CliInvoke Graceful cancellation on Unix and Windows may not have been Trimming safe or AoT compatible despite CliInvoke advertising itself as Trimming Safe and AoT compatible. It is now fixed to be Trimming safe and AoT compatible on .NET 8+ - This fix is also backported to CliInvoke 2.4 via version 2.4.4

## [2.5.0] - 2026-03-18

### Added

- Added ``IExternalProcessFactory``, an interface for easily creating ``IExternalProcess`` instances.
- Added ``ExternalProcessFactory``, the implementation of ``IExternalProcessFactory`` for easily creating ``IExternalProcess`` instances.
- Added support for ``IExternalProcessFactory`` and ``ExternalProcessFactory``

### Changed

- Updated DotExtensions version from 9.7.4 to 10.0.0
- Updated DotPrimitives version from 4.3.2 to 4.3.3
- Updated to use CliInvoke.Core 2.5.0 and CliInvoke 2.5.0
- Updated CliInvoke.Core 2.5.0

### Deprecated

- Deprecated ``IProcessConfigurationBuilder``'s ``SetStandardOutputPipe`` and ``SetStandardErrorPipe`` methods for removal in CliInvoke.Core v3
- Deprecated ``ProcessConfigurationBuilder``'s ``SetStandardOutputPipe`` and ``SetStandardErrorPipe`` methods for removal in CliInvoke v3
- Deprecated ``ProcessConfiguration``'s ``FromStartInfo`` static extension method as deprecated for removal in CliInvoke v3 - This is being replaced with a replacement method called ``FromProcessStartInfo``

### Fixed

- Fixed an issue where ProcessPipeHandler didn't make use of the CancellationToken passed to its methods (Backported to 2.4.3)

## [2.4.5] - 2026-04-02

### Changed

- Updated internal Polyfill version from 9.22.0 to 9.23.0
- Updated DotExtensions version from 9.7.4 to 9.7.5
- Updated to use CliInvoke.Core 2.4.5 and CliInvoke 2.4.5
- Updated to use CliInvoke.Core 2.4.5

### Fixed

- Fixed a potential issue where Graceful cancellation on Windows would not free an allocated console leading to leaking memory
- Fixed possible cancellation related race conditions
- Fixed an issue where the wrong exception type was attempted to be caught in cancellation methods

## [2.4.4] - 2026-03-21

### Changed

- Updated AoT Compatibility property in ``CliInvoke.Core.csproj`` to only be set to true on .NET 8+
- Updated AoT Compatibility property in ``CliInvoke.csproj`` to only be set to true on .NET 8+
- Updated to use CliInvoke.Core 2.4.4 and CliInvoke 2.4.4
- Updated AoT Compatibility property in ``CliInvoke.Extensions.csproj`` to only be set to true on .NET 8+
- Updated AoT Compatibility property in ``CliInvoke.Specializations.csproj`` to only be set to true on .NET 8+

### Fixed

- Fixed a potential issue where CliInvoke Graceful cancellation on Unix and Windows may not have been Trimming safe or AoT compatible despite CliInvoke advertising itself as Trimming Safe and AoT compatible. It is now fixed to be Trimming safe and AoT compatible on .NET 8+

## [2.4.3] - 2026-03-18

### Changed

- Updated internal Polyfill version from 9.20.0 to 9.22.0
- Updated to use CliInvoke.Core 2.4.3 and CliInvoke 2.4.3
- Added FreeBSD as a supported OS attribute for ``PowershellProcessInvoker``

### Fixed

- Fixed an issue where ``ProcessPipeHandler`` didn't make use of the CancellationToken passed to its methods
- Removed nuisance OS Support warnings for ``ProcessPipeHandler``

## [2.4.2] - 2026-03-14

### Changed

- Updated internal Polyfill version from 9.18.0 to 9.20.0
- Updated to use CliInvoke.Core 2.4.2 and CliInvoke 2.4.2

### Fixed

- Fixed an issue where ``RunnerProcessFactory`` failed to adequately apply runner configuration arguments and runner process arguments to a runner process.
- Fixed an issue where ``ProcessInvoker``'s ``ExecuteAsync`` method would attempt Standard Input Redirection without checking first if it was requested
- Fixed an issue with ``CmdProcessConfiguration`` that would cause an Argument exception to be thrown if instantiated due to an empty or whitespace string parameter value in the constructor
- Fixed an issue with ``CmdProcessConfiguration`` that caused cmd to stay open after being executed

## [2.4.1] - 2026-03-12

### Changed

- Switched to using Central Package Management
- Updated internal Polyfill version from 9.12.0 to 9.18.0
- Updated internal Polyfill version from 9.12.0 to 9.18.0
- Updated DotExtensions version from 9.7.0 to 9.7.3
- Updated ``Microsoft.Extensions.DependencyInjection.Abstractions`` version from 10.0.3 to 10.0.4
- Updated to use CliInvoke.Core 2.4.1 and CliInvoke 2.4,1
- Updated internal Polyfill version from 9.12.0 to 9.18.0
- Updated to use CliInvoke.Core 2.4.1

## [2.4.0] - 2026-02-24

### Added

- Added ``IShellDetector`` interface for detecting default system Shell
- Added ``IExternalProcess`` (the interface for ``ExternalProcess``) - This is not an injectable service and exists to facilitate a possible future ``ExternalProcess`` Factory pattern, and/or allow alternate implementations.
- Added ``ShellDetector`` implementation of ``IShellDetector`` for detecting default system Shell
- Added ``ExternalProcess`` - An alternative to the Process Invokation design pattern. This version makes use of CliInvoke v2's file path resolver and process pipe handling as well as internal CliInvoke helper code.
- Added registration of ``IShellDetector`` and ``ShellDetector`` in ``AddCliInvoke`` method

### Changed

- Updated to use CliInvoke.Core 2.4.0

## [2.3.4] - 2026-02-24

### Changed

- Updated internal Polyfill version from 9.9.0 to 9.10.0
- Updated internal Polyfill version from 9.9.0 to 9.10.0
- Updated DotExtensions version from 9.6.2 to 9.7.0
- Updated to use CliInvoke.Core 2.3.4 and CliInvoke 2.3.4
- Updated internal Polyfill version from 9.9.0 to 9.10.0
- Updated to use CliInvoke.Core 2.3.4

## [2.3.3] - 2026-02-15

### Changed

- Updated internal Polyfill version from 9.8.1 to 9.9.0
- Updated internal Polyfill version from 9.8.1 to 9.9.0
- Updated DotPrimitives version from 4.3.1 to 4.3.2
- Updated DotExtensions version from 9.6.1 to 9.6.2
- Updated to use CliInvoke.Core 2.3.3 and CliInvoke 2.3.3
- Updated internal Polyfill version from 9.8.1 to 9.9.0
- Updated to use CliInvoke.Core 2.3.3

## [2.3.2] - 2026-02-14

### Changed

- Updated DotExtensions version from 9.5.1 to 9.6.1
- Backported readme improvements
- Updated to use Microsoft.Extensions.DependencyInjection.Abstractions from 10.0.2 to 10.0.3
- Updated to use CliInvoke.Core 2.3.2 and CliInvoke 2.3.2
- Updated to use CliInvoke.Core 2.3.2

### Fixed

- Fixed xml doc comment grammar issues

## [2.3.1] - 2026-02-04

### Changed

- Update dependencies.
- Refactor `WaitForExitNoTimeoutAsync` to use `WaitForExitOrGracefulTimeoutAsync` with fallback to forceful.
- Extract `GracefulInterruptCancellation` method in `GracefulCancellation`.

## [2.3.0] - 2026-01-23

### Added

- Added CancellationToken support to ``IProcessPipeHandler``
- Added ``IProcessResultValidator<TProcessResult>`` This is the replacement for ``ProcessResultValidation``.
- Added ``CommonValidationRules`` static class for common Validation Rules
- Added Graceful Cancellation via Signals on Windows and Unix
- Added ``ProcessResultValidator<TProcessResult>`` implementation
- Added CancellationToken support to ``ProcessPipeHandler``
- Added DI registration of ``IProcessResultValidator<ProcessResult>``, ``IProcessResultValidator<BufferedProcessResult>``, and ``IProcessResultValidator<PipedProcessResult>`` in ``AddCliInvoke`` method
- Added ``AddValidationRules<TProcessResult>Func<TProcessResult, bool>[] validationRules, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)`` DI registration method
- Added ``AddCustomResultValidators<TProcessResult, TProcessResultValidator>(TProcessResultValidator validator, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)`` DI registration method

### Changed

- Replaced manual ``ArgumentOutOfRangeException`` instantiation and throwing with cleaner Throw method calls
- Updated ``ProcessExceptionInfo`` class
- Updated ``ProcessConfigurationFactory`` to use default ``ArgumentsBuilder`` validation
- Updated ``ArgumentsBuilder`` logic to allow setting empty arguments
- Removed unnecessary Process ``HasStarted`` internal helper method - ``ProcessWrapper``'s ``HasStarted`` property is used instead
- Replaced manual ``ArgumentOutOfRangeException`` instantiation and throwing with cleaner Throw method calls
- Reduced code duplication in ``ProcessInvoker`` implementation class
- Updated to use CliInvoke.Core 2.3.0 and CliInvoke 2.3.0
- Updated to use CliInvoke.Core 2.3.0

### Deprecated

- Deprecated ``ProcessConfiguration``'s ``StandardOutput`` and ``StandardError`` properties - These will be removed in CliInvoke.Core v3
- Deprecated ``ProcessResultValidation`` enum - This will be replaced with a new Result Validation system in CliInvoke.Core v3
- Deprecated ``ResultValidation`` property in ``ProcessExitConfiguration`` - This will be replaced in CliInvoke.Core v3
- Deprecated ``ProcessResult``'s ``WasSuccessful`` property
- Deprecated ``IFilePathResolver`` interface - This will be removed in version 3. CliInvoke uses File Path Resolving as an implementation detail and CliInvoke main package will migrate to use an alternative file path resolving interface

### Fixed

- Fixed xml doc comment issues

## [2.2.1] - 2026-01-21

### Changed

- Updated internal Polyfill version from 9.6.0 to 9.7.4
- Updated internal Polyfill version from 9.6.0 to 9.7.4
- Updated DotPrimitives version from 4.1.0 to 4.2.0
- Updated DotExtensions version from 9.3.1 to 9.4.1
- Updated Microsoft.Extensions.DependencyInjection.Abstractions from 10.0.1 to 10.0.2
- Updated to use CliInvoke.Core 2.2.1 and CliInvoke 2.2.1
- Updated internal Polyfill version from 9.6.0 to 9.7.4
- Updated to use CliInvoke.Core 2.2.1

### Fixed

- Fixed an issue where an empty StandardOutput in ``BufferedProcessResult`` constructor would throw an ArgumentException
- Fixed an issue with ``ProcessInvoker`` exit code validation logic if exit code validation fails

## [2.2.0] - 2025-12-29

### Added

- Add `ProcessExceptionInfo` type for detailed process failure information.
- Add initial `Configuration.FromStartInfo` extension method.
- Add File Path Resolve unit tests.
- Add XML doc comments across the codebase.
- Add initial reworked string escape code.

### Changed

- Rework `FilePathResolver` to use DotExtensions and DotPrimitives.
- Rework Powershell Unix detection to resolve file path and remove reliance on `which`.
- Improve file path resolver performance.
- Compute target file path once in Process Configuration.
- Deprecate Classic Powershell Configuration.
- Switch to DotExtensions package.
- Switch to DotPrimitives 4.1.0.
- Clean up Argument Exceptions.
- Polyfill Argument Exceptions.
- Remove unnecessary new instantiation.

### Fixed

- Fix Cancellation Token ignored in Timeout methods.
- Fix potential issue with Process wrongly reporting starting.
- Fix standard input issue.
- Fix issues with File Path Resolver.
- Fix duplicate argument escaping.
- Fix typos in exception strings.
- Fix copyright header issue.
- Fix incorrect license notice.
- Fix issues with old package id and namespaces.

### Removed

- Remove remote process running detection and process disposal detection.
- Remove no longer needed localized phrase.

## [2.1.5] - 2026-01-02

### Changed

- Update to use CliInvoke main package 2.1.5

### Fixed

- Fixed an issue where Cancellation Token is ignored in some circumstances
- Fixed an issue where ``ProcessWrapper`` could attempt to apply a Resource Policy to a Process even if the Process hasn't started successfully
- Fixed an issue where ``ProcessInvoker``'s ``ExecutePipedAsync`` method would attempt to redirect Standard Input before the process has started

## [2.1.4] - 2025-12-26

### Changed

- Update internal Polyfill version from 9.3.4 to 9.5.0
- Update internal Polyfill version from 9.3.4 to 9.5.0
- Update to CliInvoke 2.1.4
- Update internal Polyfill version from 9.3.4 to 9.5.0
- Replaced outdated package Ids and namespaces in readmes and some file headers.

### Fixed

- Fixed an issue with Standard Input incorrectly being applied to ``ProcessWrapper`` (CliInvoke's internal Process wrapper class) in ``ProcessInvoker``

## [2.1.3] - 2025-12-18

### Changed

- Update to use CliInvoke main package 2.1.3

### Fixed

- Fixed a bug where FilePathResolver's implementation of Path Environment Variable Resolving would lead to incorrect file path evaluation due to an incorrect if statement evaluation.

## [2.1.2] - 2025-12-11

### Changed

- Updated to Polyfill version 9.3.4 from 9.1.0
- Updated to Polyfill version 9.3.4 from 9.1.0
- Updated to Polyfill version 9.3.4 from 9.1.0
- Updated to Microsoft.Extensions.DependencyInjection.Abstractions version 10.0.1 from 10.0.0

## [2.1.1] - 2025-11-18

### Changed

- Added Trimming test project
- Added AoT test project
- Update to internal Polyfill version 9.1.0 from 9.0.3
- Update to internal Polyfill version 9.1.0 from 9.0.3
- Update to internal Polyfill version 9.1.0 from 9.0.3

### Fixed

- Fixed an issue that caused Trimming warning IL2091 when used in a Trimmed Application
- Fixed nullability inconsistency issues

## [2.1.0] - 2025-11-14

### Added

- Added .NET 10  TFM and switched to C# language version 14
- Added Trimming Support and AoT support
- Added ``UseCustomFilePathResolver<TResolver>`` extension member method to register custom File Path Resolver implementations - This uses C# 14's extension members system and thus requires C# 14

### Changed

- Renamed namespaces to start with ``CliInvoke`` instead of ``AlastairLundy.CliInvoke``
- Updated ``IUserCredentialBuilder`` to not accept null strings
- Updated internal Polyfill version from 8.9.1 to 9.0.3
- Improved ``FilePathResolver`` resolving logic
- Updated ``UserCredentialBuilder`` to not accept null strings
- Add .NET Standard 2.0 fallback for null checks using Polyfill's ``Ensure`` static methods
- Updated internal Polyfill version from 8.9.1 to 9.0.3
- Removed dependency on DotExtensions
- Updated to use CliInvoke Core 2.1.0
- Updated internal Polyfill version from 8.9.1 to 9.0.3
- Updated ``Microsoft.Extensions.DependencyInjection.Abstractions`` from 9.0.10 to 10.0.0

## [2.0.1] - 2025-11-18

### Changed

- Update internal Polyfill version from 8.9.1 to 9.1.0
- Updated DotExtensions from 8.6.3 to 9.0.0
- Update internal Polyfill version from 8.9.1 to 9.1.0
- Update Core and Main to 2.0.1
- Update Microsoft.Extensions.DependencyInjection.Abstractions from 9.0.10 to 9.0.11
- Update internal Polyfill version from 8.9.1 to 9.1.0

## [2.0.0] - 2025-10-28

### Added

- Added `IProcessConfigurationFactory`
- Added abstract class `RunnerProcessInvokerBase` that inherits from `IProcessInvoker`
- Added `IRunnerProcessFactory` - An interface for creating Processes that run other processes.
- Added support for Graceful Cancellation after specified Timeout
- Added support for Forceful Cancellation after specified Timeout
- Added `ProcessCancellationExceptionBehaviour` enum to enable configuring Cancellation Exception behaviour (i.e. suppressing the exception, allowing it, or allowing the exception if unexpected) - This has been added as a property to `ProcessExitConfiguration`, with updates to the class to support the addition.
- Added `DefaultProcessRunnerInvoker` - The default implementation of RunnerProcessInvokerBase
- Added `RunnerProcessCreator` - A class implementation of `IRunnerProcessCreator` for creating Processes that run other processes.
- Added `IProcessConfigurationFactory` and `ProcessConfigurationFactory` dependency injection setup to the `AddCliInvoke` extension method
- Added convenience extension methods that make CliInvoke more ergonomic to use.

### Changed

- Updated `ProcessResult` to implement `IEquatable<ProcessResult>`
- Updated `PipedProcessResult` to implement `IDisposable` and dispose of the Stream parameters when Dispose or DisposeAsync is called
- Changed default value for `ProcessResourcePolicy`'s `PriorityBoostEnabled` value from true to false
- Updated `EnvironmentVariablesBuilder` to allow enabling/disabling exceptions if duplicate keys are attempted to be added.
- Updated `IProcessInvoker` to allow specifying ProcessConfiguration disposal after invoker use
- Updated Specialization `ProcessConfiguration` derived classes to work with the updated `ProcessConfiguration` class
- Improved robustness of `ProcessTimeoutPolicy` parameter checks during creation
- Clarified OS support for `IProcessResourcePolicyBuilder` methods
- Annotated `IProcessInvoker` methods to indicate lack of support for IOS and tvOS
- Reworked `ProcessInvoker` implementation to not require `IProcessFactory`
- Moved `ProcessConfiguration`'s `ToProcessStartInfo` method logic to `ApplyProcessConfiguration` internal extension method in `CliInvoke`
- Moved extension methods to `CliInvoke` main package
- Moved `ProcessTimeoutPolicy` and `ProcessResultValidation` properties from `ProcessConfiguration` into a new type `ProcessExitConfiguration`
- Moved Builder implementations from `CliInvoke` to `CliInvoke.Core`
- Moved `FilePathResolver` into CliInvoke.Core directly
- Moved `IFilePathResolver` into CliInvoke.Core directly
- Stability improvements
- Renamed `IEnvironmentVariablesBuilder` methods to avoid ambiguous method usage
- Renamed `EnvironmentVariablesBuilder` methods to avoid ambiguous method usage
- Restructured `IProcessInvoker` interface with methods for `ExecuteAsync`, `ExecuteBufferedAsync`, and `ExecutePipedAsync`

### Removed

- Removed `CliCommandConfiguration` - This has been replaced by `ProcessConfiguration`
- Removed `ICliCommandConfigurationBuilder` and `CliCommandConfigurationBuilder` - These have been replaced by `IProcessConfigurationBuilder` and `ProcessConfigurationBuilder` respectively
- Removed `ICliCommandInvoker` and `CliCommandInvoker` This has been replaced with `IProcessInvoker`
- Removed `StartInfo` property from `ProcessConfiguration`
- Removed Primitives subnamespace
- Removed `TryApplyUserCredential` extension method
- Removed redundant constructor in `ProcessConfiguration`
- Removed `UserCredential` `IsSupportedOnCurrentOs` extension method
- Removed `IProcessFactory`
- Removed `IProcessTimeoutPolicyBuilder` and `ProcessTimeoutPolicyBuilder` - These builders added hardly any benefit for the complexity they added
- Removed deprecated code
- Removed `ProcessFactory`

