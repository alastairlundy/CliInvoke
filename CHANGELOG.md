# Changelog

All notable changes to CliInvoke are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/).

## [3.0.0-alpha] — 3.0.0 pre-release line

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

- **D002 — `CliRun` static mutable state removed.** `CliRun.UseExternalProcessFactory`
  and `CliRun.UseFilePathResolver` no longer exist. `CliRun` retains only its
  `Run*`/`FireAndForget` methods, and each call now allocates a fresh
  `ProcessInvocationPipeline` (with a fresh `ExternalProcessFactory` and default
  `FilePathResolver`) per call. There is no process-wide configurable state to
  leak between calls. Callers needing a custom factory or resolver must use
  `IProcessInvoker` (or DI) instead of `CliRun`. No `[Obsolete]` shim or bridge
  method was added (direct cutover).
- **D003 — `InvocationContext.Result` / `.Middleware` ownership documented (no API change).**
  These properties are now documented as owned by specific middleware: the only
  legitimate mutators are the `MiddlewareChain` walker, the terminal delegate
  that bridges the chain to the pipeline, and any propagating middleware that
  short-circuits the chain. Caller code must not read or write them outside
  those mutators. This is a documentation-only change; the setters are retained.
- **D004 — `ExitConfiguration` is now read-only.** `IExternalProcess.ExitConfiguration`
  and `ExternalProcess.ExitConfiguration` are read-only `{ get; }` properties
  supplied at construction time. The `ExitConfiguration` setter and any
  `WithExitConfiguration(...)` method do not exist. Construct `ExternalProcess`
  with the exit configuration via its constructor.
- **D005 — `ProcessInvoker` reduced to two constructors.** The partial overloads
  `(IExternalProcessFactory, MiddlewareItems?)` and
  `(IExternalProcessFactory, IEnumerable<IProcessMiddleware>)` were removed.
  The surviving constructors are `(IExternalProcessFactory)` and
  `(IExternalProcessFactory, IEnumerable<IProcessMiddleware>, MiddlewareItems?)`.
- **D006 — `ExternalProcess` keeps only constructor C.** Constructors
  `(IFilePathResolver, string)` and `(ProcessConfiguration, ProcessExitConfiguration?)`
  were removed. `ExternalProcess` is now constructed with
  `(IFilePathResolver, ProcessConfiguration, ProcessExitConfiguration?)`.
- **D008 — `ExternalProcess` is sealed.** The class is now `sealed`; no
  public or protected extension points remain.
- **D010 — `ProcessConfigurationBuilder` is sealed.** The concrete builder is now
  `sealed`. Fluent chaining via the `IProcessConfigurationBuilder` interface is
  unaffected.
- **D012 — `ProcessResult` equality is symmetric.** `ProcessResult.Equals(object?)`
  now uses exact runtime-type matching so that `a.Equals(b) == b.Equals(a)` holds
  across the `ProcessResult` hierarchy. `BufferedProcessResult` and
  `PipedProcessResult` were audited to satisfy the same symmetry contract.
  `ProcessResult` is intentionally **not** sealed in this release (sealing is
  deferred).

[3.0.0-alpha]: https://github.com/alastairlundy/CliInvoke/releases
