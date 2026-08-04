---
title: Add pipeline dispatch tests and invoker integration tests
classification: Independent
blocked_by: ["003-process-invocation-pipeline.md", "004-internals-visible-to.md", "005-process-invoker-wiring.md", "006-specialization-invoker-wiring.md", "007-clirun-pipeline-wiring.md"]
parent: docs/decisions/DECISIONS-CliInvoke-process-invocation-pipeline.md
---

## Goal

Add two test classes that together cover the pipeline - a focused dispatch test using a mock `IExternalProcessFactory` and an integration test driving `IProcessInvoker` against a real process for all three modes.

## What to build

Add two new test files in `tests/CliInvoke.Tests/`:

### `PipelineDispatchTests.cs`

- Class is internal (matches the existing `CliRunTests` access pattern; the pipeline is internal in `CliInvoke` so the test must be internal too, reachable through the `InternalsVisibleTo` grant in TK004).
- Use the existing `CountingExternalProcessFactory.StubExternalProcess` pattern from `CliRunTests.cs` (lines 268-419) to provide a stub `IExternalProcess`. The stub's `WaitForExitOrTimeoutAsync`, `CaptureBufferedResultAsync`, and `CapturePipedResultAsync` already return sentinel results.
- Construct `ProcessInvocationPipeline` directly with the stub factory.
- For each `InvocationMode` value (`Raw`, `Buffered`, `Piped`), call `InvokeAsync<T>(ctx)` with the matching typed result and assert that the stub's matching capture method was called.
- For `FireAndForget`, call `InvokeAsync<ProcessResult>(ctx)` and assert the returned stub `ProcessResult` has the process identifier populated.
- For cancellation, build a context with a pre-cancelled `CancellationToken` and assert that the start-or-capture method observes the cancellation (the stub's `StartAsync` can throw `OperationCanceledException` when the token is already cancelled - mirror the existing `ThrowOnStart` pattern in `CountingExternalProcessFactory`).

### `ProcessInvokerIntegrationTests.cs`

- Drive the public `IProcessInvoker` (the one wired by TK005) against a real process for all three modes.
- For `ExecuteAsync` and `ExecuteBufferedAsync` and `ExecutePipedAsync`, use a real cross-platform command available on CI (`which dotnet` on Linux, `where dotnet` on Windows - or `echo` as a more portable sentinel). The existing `ProcessTestHelper.GetTargetFilePath` pattern in `CliRunTests.cs` (line 47) shows the discovery approach.
- Assert that each returned result is non-null and that the exit code reflects success on the target host.
- For the cancellation path, pass a pre-cancelled `CancellationToken` to each method and assert the call throws `OperationCanceledException` (or a wrapper such as `TaskCanceledException`).

The two test classes share a single `CliRunTests` access pattern: each is marked `[NotInParallel]` only if it touches `CliRun` static state. `PipelineDispatchTests` does not need `[NotInParallel]` (it does not call `CliRun`). `ProcessInvokerIntegrationTests` also does not need `[NotInParallel]` for the same reason.

## Size

- **Files** - 2

## Recommended Workflow

### Step 1 — Add PipelineDispatchTests

Where: `tests/CliInvoke.Tests/PipelineDispatchTests.cs` (new file)

- Add a new internal class `PipelineDispatchTests` (no `[NotInParallel]` needed).
- Reuse the `CountingExternalProcessFactory` (or a new internal stub factory) to provide a controllable `IExternalProcess`.
- Write one `[Test]` method per `InvocationMode` value (four methods).
- Write one `[Test]` method for the cancellation path.
- For each test, construct a `ProcessInvocationContext` and call `_pipeline.InvokeAsync<T>(ctx)`, asserting the expected outcome.

Verify: `dotnet test tests/CliInvoke.Tests/CliInvoke.Tests.csproj --framework net10.0 --filter-class PipelineDispatchTests` runs all five tests and they pass.

### Step 2 — Add ProcessInvokerIntegrationTests

Where: `tests/CliInvoke.Tests/ProcessInvokerIntegrationTests.cs` (new file)

- Add a new class `ProcessInvokerIntegrationTests` (no `[NotInParallel]` needed).
- Use the existing `ProcessTestHelper.GetTargetFilePath` (or a similar portable test executable) to drive a real process.
- Write one `[Test]` method per mode (`ExecuteAsync`, `ExecuteBufferedAsync`, `ExecutePipedAsync`) that asserts a successful result.
- Write one `[Test]` method for the cancellation path that pre-cancels the token and asserts `OperationCanceledException` (or its wrapper).

Verify: `dotnet test tests/CliInvoke.Tests/CliInvoke.Tests.csproj --framework net10.0 --filter-class ProcessInvokerIntegrationTests` runs all four tests and they pass on the current host.

### Step 3 — Run the full test suite

Where: `tests/CliInvoke.Tests/`

- Run `dotnet test tests/CliInvoke.Tests/CliInvoke.Tests.csproj --framework net10.0` from the repository root.
- Confirm the existing eleven `CliRunTests` plus the new dispatch and integration tests all pass.

Verify: All tests in the project pass. The CI gate is green.

## Context pointers

**Files** - `tests/CliInvoke.Tests/PipelineDispatchTests.cs` (new) and `tests/CliInvoke.Tests/ProcessInvokerIntegrationTests.cs` (new) - the deliverables. `tests/CliInvoke.Tests/CliRunTests.cs` - the existing test surface whose `CountingExternalProcessFactory.StubExternalProcess` pattern (lines 319-418) is the model for the new stub. `src/CliInvoke/ProcessInvocationPipeline.cs` - the system under test (TK003). `src/CliInvoke/ProcessInvoker.cs` - the system under test for the integration tests (TK005).

**Domain terms** - Process Invocation Pipeline (the system under test in the dispatch tests).

**Ledger records** - `DECISIONS-CliInvoke-process-invocation-pipeline.md#T008` (one test class per concern: dispatch tests and integration tests). `DECISIONS-CliInvoke-process-invocation-pipeline.md#D007` (mixed test surface; `InternalsVisibleTo("CliInvoke.Tests")` is required for the dispatch tests, scoped to `CliInvoke.Tests` only).

## Acceptance criteria

- [ ] `PipelineDispatchTests` exists as an internal class in `tests/CliInvoke.Tests/`.
- [ ] The class has a `[Test]` method per `InvocationMode` value that asserts the matching capture method is invoked.
- [ ] The class has a `[Test]` method for `FireAndForget` that asserts the returned stub `ProcessResult` has the process identifier populated.
- [ ] The class has a `[Test]` method for cancellation propagation.
- [ ] `ProcessInvokerIntegrationTests` exists in `tests/CliInvoke.Tests/`.
- [ ] The class has `[Test]` methods for `ExecuteAsync`, `ExecuteBufferedAsync`, and `ExecutePipedAsync` that drive a real process and assert success.
- [ ] The class has a `[Test]` method for the cancellation path on each invoker mode.
- [ ] The existing eleven `CliRunTests` continue to pass without modification.
- [ ] All tests pass on the host's primary target framework (`net10.0`).

## Dependencies

**Blocked by** - `003-process-invocation-pipeline.md` (the system under test for the dispatch tests), `004-internals-visible-to.md` (the test assembly must be granted access to the internal pipeline), `005-process-invoker-wiring.md` (the integration tests drive the refactored `ProcessInvoker`), `006-specialization-invoker-wiring.md` (the specialisation tests need the refactored wrappers; this dependency is loose - the dispatch and integration tests in `CliInvoke.Tests` do not directly cover the specialisations, but the `CliInvoke.Specializations.Tests` project depends on the same `InternalsVisibleTo` chain being intact), and `007-clirun-pipeline-wiring.md` (the integration tests rely on `CliRun`'s public static surface continuing to work end-to-end).
