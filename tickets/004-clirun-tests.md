---
title: Add `CliRunTests.cs` covering the public surface with parallel-safe, `IDisposable`-where-needed fakes
classification: Independent
blocked_by: [001-build-string-args-config-helper, 002-clirun-funnel-method, 003-per-call-default-factory]
parent: docs/decisions/DECISIONS-CliInvoke-clirun-shape.md
---

## Goal

Add a new test file `tests/CliInvoke.Tests/CliRunTests.cs` that exercises the public `CliRun` surface with custom fakes for `IExternalProcessFactory` and `IFilePathResolver`, verifying (a) the custom factory is invoked once per `Run*Async` call, (b) the custom factory's resolver is the one used, and (c) the throw from an unresolvable path is the resolver's throw type, propagated through the factory's `StartAsync` (no pre-resolution per T006).

## What to build

Create a new file `tests/CliInvoke.Tests/CliRunTests.cs` with a single TUnit test class `CliRunTests`:

- Annotate the class with `[NotInParallel]` so tests that mutate `CliRun`'s static state (`_externalProcessFactory`, `_filePathResolver`) do not race with each other.
- Define a `CountingExternalProcessFactory` test fake that implements `IExternalProcessFactory` and `IDisposable`; the fake records the number of `CreateExternalProcess` invocations and the `ProcessExitConfiguration` passed in, and disposes any captured state in its `Dispose` method.
- Define a `CapturingFilePathResolver` test fake that implements `IFilePathResolver` and `IDisposable`; the fake records the paths it was asked to resolve and throws a sentinel exception (e.g., a custom `UnresolvablePathException`) from `ResolveFilePath` when the path is the sentinel, so the throw-propagation test has a known type to assert on.
- Define an `[AfterEach]` hook that disposes the active fakes and resets `CliRun`'s static state via `CliRun.UseExternalProcessFactory(new ExternalProcessFactory())` and `CliRun.UseFilePathResolver(new FilePathResolver())`.

Tests to add (TUnit `[Test]` methods):

- For each of the six public `Run*Async` methods (3 string-argument overloads at the public surface, 3 config-argument overloads at the public surface): assert the custom factory's `CreateExternalProcess` is invoked exactly once per call, and assert the custom factory's resolver is the one that received the path.
- A throw-propagation test: register a custom factory and a custom resolver that throws the sentinel from `ResolveFilePath`; invoke `CliRun.RunAsync(...)` with the sentinel path; assert the exception type is the resolver's sentinel exception (not a `TargetInvocationException` wrapper or a `FileNotFoundException` from somewhere else).
- A per-call-factory test: register no custom factory (rely on the default); invoke `CliRun.RunAsync(...)` twice; assert the default factory is constructed twice (one `ExternalProcessFactory` instance per call, not a cached singleton) per T005.

Use `ProcessTestHelper.GetTargetFilePath()` from `tests/CliInvoke.Tests/Internal/Helpers/ProcessTestHelper.cs` for the cross-platform test executable path. Follow the existing TUnit test conventions in `tests/CliInvoke.Tests/` (e.g., `tests/CliInvoke.Tests/Invokers/ProcessInvokerTests.cs`) for namespace, using directives, and assertion style.

## Size

- **Files** - 1

## Recommended Workflow

### Step 1 — Create the test class skeleton with `[NotInParallel]` and the `[AfterEach]` reset hook

Where: `tests/CliInvoke.Tests/CliRunTests.cs` (new file)

- Add the file header, namespace, and using directives consistent with existing TUnit test files in `tests/CliInvoke.Tests/`.
- Declare the `CliRunTests` class with the `[NotInParallel]` attribute.
- Add an `[AfterEach]` method that disposes the fakes (if any are held in instance fields) and resets `CliRun`'s statics via `CliRun.UseExternalProcessFactory(new ExternalProcessFactory())` and `CliRun.UseFilePathResolver(new FilePathResolver())`.

Verify: The file compiles; the class is discoverable by TUnit's test runner; no test is added yet so the test list is empty.

### Step 2 — Add the `CountingExternalProcessFactory` and `CapturingFilePathResolver` fakes

Where: `tests/CliInvoke.Tests/CliRunTests.cs`

- Add `CountingExternalProcessFactory` as a `private sealed class` nested in `CliRunTests`, implementing `IExternalProcessFactory` and `IDisposable`; expose a `CreateExternalProcessCallCount` property and a list of captured `(ProcessConfiguration, ProcessExitConfiguration)` tuples.
- Add `CapturingFilePathResolver` as a `private sealed class` nested in `CliRunTests`, implementing `IFilePathResolver` and `IDisposable`; expose a list of captured paths and a sentinel-path-based throw mechanism for the throw-propagation test.

Verify: Both fakes implement their respective interfaces correctly; both implement `IDisposable` (no-op `Dispose` is acceptable for fakes that hold only managed state, but the `IDisposable` contract is mandatory per T007).

### Step 3 — Add the six public-method tests asserting the factory call count and resolver usage

Where: `tests/CliInvoke.Tests/CliRunTests.cs`

- Add one `[Test]` method per public `Run*Async` overload (6 total: `RunAsync`, `RunBufferedAsync`, `RunPipedAsync` for both string-argument and config-argument overloads).
- Each test registers the `CountingExternalProcessFactory` and the `CapturingFilePathResolver` via `CliRun.UseExternalProcessFactory(...)` and `CliRun.UseFilePathResolver(...)`.
- Each test invokes the `Run*Async` method with a path obtained from `ProcessTestHelper.GetTargetFilePath()`.
- Each test asserts the `CreateExternalProcessCallCount` is exactly 1 and the `CapturingFilePathResolver` recorded the path it was asked to resolve (the path flows through the funnel via the factory's `StartAsync`, not through the helper's pre-resolution per T006).

Verify: All six tests pass on net8.0, net9.0, and net10.0.

### Step 4 — Add the throw-propagation and per-call-factory tests

Where: `tests/CliInvoke.Tests/CliRunTests.cs`

- Add a throw-propagation `[Test]` that registers a custom factory and a custom resolver that throws the sentinel exception from `ResolveFilePath` for a specific sentinel path; invokes `CliRun.RunAsync(sentinelPath, ...)`; asserts the thrown exception's type is the resolver's sentinel exception type.
- Add a per-call-factory `[Test]` that does not register a custom factory, invokes `CliRun.RunAsync(...)` twice with the same path, and asserts (via a wrapping factory that captures the factory instance) that two distinct `ExternalProcessFactory` instances are observed per T005's per-call construction.

Verify: Both new tests pass; the throw-propagation test fails if pre-resolution is re-introduced to the helper (the helper would throw the sentinel before reaching the factory, but the test asserts the exception originates from the factory's `StartAsync` path — implementers should write the test in a way that distinguishes the helper's throw from the factory's throw, e.g., by checking the exception's stack frame).

### Step 5 — Run the new test file and confirm the full suite passes

Where: `tests/CliInvoke.Tests/`

- Run `dotnet test tests/CliInvoke.Tests/CliInvoke.Tests.csproj --filter "FullyQualifiedName~CliRunTests"` to run the new tests in isolation.
- Run `dotnet test tests/CliInvoke.Tests/CliInvoke.Tests.csproj` to confirm no regressions in existing tests across the full suite.

Verify: All new `CliRunTests` pass; the full test suite passes with no regressions on net8.0, net9.0, and net10.0.

## Context pointers

**Files**
- `tests/CliInvoke.Tests/CliRunTests.cs` — the new test file created by this ticket.
- `tests/CliInvoke.Tests/Internal/Helpers/ProcessTestHelper.cs` — `GetTargetFilePath()` is the cross-platform test executable path helper used by the new tests.
- `tests/CliInvoke.Tests/GlobalUsings.cs` — existing global using directives; the new file should not duplicate these.
- `tests/CliInvoke.Tests/Invokers/ProcessInvokerTests.cs` — reference for the existing TUnit test conventions (namespace, using directives, `[Test]` style, assertion style).
- `src/CliInvoke/Extensions/CliRun.cs` — the production file exercised by the new tests; the new tests assert the behaviour introduced by TK001 (helper), TK002 (funnel), and TK003 (per-call factory default).

**Domain terms**
- Resource-Owning Type (from `CONTEXT.md`) — `IExternalProcess` and `ProcessConfiguration` are Resource-Owning Types; the test fakes should not need to allocate real Resource-Owning Types (the `CountingExternalProcessFactory` can return `null!` from `CreateExternalProcess` if the test path is short-circuited before `StartAsync`, but tests that reach `StartAsync` need a real or fake `IExternalProcess`).

**Ledger records**
- `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#T007` — public-surface tests, parallel-safe via `[NotInParallel]`, fakes implement `IDisposable` where needed, `[AfterEach]` resets `CliRun` statics.
- Cross-cite (superseded): `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#T003` (lazy-default-factory claim and throw-timing claim) — superseded by T007; covered by this ticket via the active successor.

## Acceptance criteria

- [ ] A new test file `tests/CliInvoke.Tests/CliRunTests.cs` exists.
- [ ] The test class is annotated with `[NotInParallel]` per `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md#T007`.
- [ ] The `CountingExternalProcessFactory` and `CapturingFilePathResolver` fakes both implement `IDisposable`.
- [ ] An `[AfterEach]` hook disposes the fakes and resets `CliRun`'s statics via `UseExternalProcessFactory(new ExternalProcessFactory())` and `UseFilePathResolver(new FilePathResolver())`.
- [ ] Tests cover all six public `Run*Async` methods (3 string-argument overloads, 3 config-argument overloads).
- [ ] Tests assert the custom factory's `CreateExternalProcess` is invoked exactly once per `Run*Async` call.
- [ ] Tests assert the custom factory's resolver is the one that received the path (no pre-resolution per T006).
- [ ] A throw-propagation test asserts the resolver's throw type is the exception type observed at the public call site.
- [ ] A per-call-factory test asserts the default `ExternalProcessFactory` is constructed per call (not a cached singleton) per T005.
- [ ] All new tests pass on net8.0, net9.0, and net10.0.
- [ ] The full `tests/CliInvoke.Tests/` suite continues to pass with no regressions.

## Dependencies

**Blocked by** - 001-build-string-args-config-helper, 002-clirun-funnel-method, 003-per-call-default-factory
