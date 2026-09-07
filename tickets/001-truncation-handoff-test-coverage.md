---
title: CliInvoke — Truncation handoff test coverage (CliRun and middleware paths)
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-middleware-deepening.md
---

## Goal

Pin the truncation handoff end-to-end on both entry paths the deepening authorized: the CliRun configuration path and the `UseOutputTruncation` middleware sugar path. Tests must prove the cap travels on `ProcessExitConfiguration.MaxBufferedOutputBytes` and is read by the pipeline from `ctx.ExitConfiguration` — the configuration seam from T002/T005 — and must NOT reintroduce or assert the deleted `MiddlewareItems` string-key handoff.

The middleware unit tests in `tests/CliInvoke.Extensions.Tests/Middleware/Truncation/OutputTruncationMiddlewareTests.cs` already cover the middleware writing the cap to the downstream exit configuration. What is missing is evidence that the cap, once on the exit configuration, actually truncates buffered capture through a real invocation on both paths, and that absence of a cap preserves unbounded capture (`WasTruncated == false`).

## What to build

1. New test file `tests/CliInvoke.Tests/Invokers/OutputTruncationHandoffTests.cs` covering:
   - **CliRun path**: `CliRun.RunBufferedAsync(targetFilePath, arguments, ..., maxBufferedOutputBytes: N)` where the child writes more than N bytes → returned `BufferedProcessResult` output is truncated to the cap and `WasTruncated` is `true`.
   - **CliRun path, no cap**: same invocation without `maxBufferedOutputBytes` → full output, `WasTruncated` is `false`.
   - **Middleware path**: `IProcessInvoker` resolved from DI with `builder.UseOutputTruncation(new TruncationOptions { MaxBytes = N })` → buffered invocation → output truncated to N, `WasTruncated` is `true`.
   - **Middleware path, no truncation middleware and no cap** → full output, `WasTruncated` is `false`.
   - **Configuration-only path**: `IProcessInvoker` with a cap set on the exit configuration via `ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes` and no truncation middleware → cap still honored (T005 constraint: the configuration path serves all callers without middleware).
2. Follow the executable-selection and OS-guard patterns already used by `tests/CliInvoke.Tests/Invokers/BufferedCaptureNoDeadlockTests.cs` (which already registers `UseOutputTruncation` but asserts only deadlock-freedom, not truncation correctness).

## Size

- **Files**: 1 new (OutputTruncationHandoffTests.cs); 0 edits
- **Large Files to be created**: omit
- **Large Edits required**: omit

## Recommended Workflow

### Step 1 - Survey existing invocation test patterns

Where: tests/CliInvoke.Tests/Invokers/BufferedCaptureNoDeadlockTests.cs, tests/CliInvoke.Extensions.Tests/Middleware/Truncation/OutputTruncationMiddlewareTests.cs

- Note how DI is set up (`AddCliInvoke`), how a cross-platform child process that emits large output is chosen, and how `TruncationOptions` is sized.

Verify: You can name the command helper and OS guards to reuse.

### Step 2 - Write CliRun-path cases

Where: tests/CliInvoke.Tests/Invokers/OutputTruncationHandoffTests.cs (new)

- Cap case and no-cap case through `CliRun.RunBufferedAsync`, asserting `StandardOutput`/`StandardError` length bounds and `WasTruncated`.

Verify: `dotnet test` from `tests/CliInvoke.Tests/` passes the new cases.

### Step 3 - Write middleware-path and configuration-only cases

Where: tests/CliInvoke.Tests/Invokers/OutputTruncationHandoffTests.cs

- `UseOutputTruncation(TruncationOptions)` case through DI; cap-on-exit-configuration case with no middleware; shared no-cap baseline.

Verify: Same test run passes; both paths exercise the pipeline read at `ProcessInvocationPipeline` (`ctx.ExitConfiguration?.MaxBufferedOutputBytes`).

### Step 4 - Full test run

Where: tests/CliInvoke.Tests/ (working directory, per CI)

- `dotnet test` — all new and existing tests pass.

Verify: 0 failures; no flakiness from child-process output timing (output sizes must exceed the cap deterministically).

## Context pointers

##### Files

- src/CliInvoke/CliRun.cs — `RunBufferedAsync(..., long? maxBufferedOutputBytes = null, ...)`; `BuildStringArgsConfig` applies the cap via `WithMaxBufferedOutputBytes`.
- src/CliInvoke/ProcessInvocationPipeline.cs — reads `ctx.ExitConfiguration?.MaxBufferedOutputBytes` and forwards it to `CaptureBufferedResultAsync(ct, cap, cap)`.
- src/CliInvoke.Core/Extensions/ProcessExitConfigurationCreationExtensions.cs — `WithMaxBufferedOutputBytes(config, cap)`.
- src/CliInvoke/Extensions/Middleware/Truncation/OutputTruncationMiddleware.cs — sugar that writes the cap via `WithExitConfiguration` before `next`.
- src/CliInvoke.Core/Primitives/Results/BufferedProcessResult.cs — `WasTruncated`.
- tests/CliInvoke.Tests/Invokers/BufferedCaptureNoDeadlockTests.cs — existing DI + `UseOutputTruncation` invocation pattern to reuse.

##### ADRs

- None new; honors D002 (truncation is an invocation capability read from configuration) and T005 (middleware is thin sugar over the configuration seam).

##### Domain terms

- Invocation Capability — the truncation cap is stated on the invocation contract, not composed as middleware decoration (GLOSSARY.md).
- Buffered invocation mode — the only mode truncation applies to; Raw and FireAndForget are unaffected.

##### Ledger records

- DECISIONS-CliInvoke-middleware-deepening.md#T006 — new coverage shall include the truncation handoff (CliRun and middleware paths). Primary source for this ticket.
- DECISIONS-CliInvoke-middleware-deepening.md#T002 — cap is a nullable long on ProcessExitConfiguration; pipeline reads it from ctx.ExitConfiguration; the MiddlewareItems string-key handoff is deleted.
- DECISIONS-CliInvoke-middleware-deepening.md#T005 — UseOutputTruncation stays as thin sugar writing the cap via WithExitConfiguration; the configuration path serves all callers.
- DECISIONS-CliInvoke-middleware-deepening.md#D002 — no middleware-to-pipeline string-key handoff may exist for the cap; CliRun invocations honor the cap.

Note: the blueprint file `IMPLEMENTATION-middleware-deepening.md` is absent from the repo; the Decision Ledger is authoritative.

## Acceptance criteria

- [ ] CliRun path: capped invocation truncates output and sets `WasTruncated == true`; uncapped invocation returns full output with `WasTruncated == false`.
- [ ] Middleware path: `UseOutputTruncation` through DI truncates buffered capture identically.
- [ ] Configuration-only path: cap on the exit configuration is honored with no truncation middleware present.
- [ ] No test references `MiddlewareItems` keys for the cap (the string-key handoff stays deleted).
- [ ] Tests live under `tests/CliInvoke.Tests/` and pass when run from that directory (CI parity).

## Dependencies

Blocked by: none
