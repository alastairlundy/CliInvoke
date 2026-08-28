---
title: CliInvoke — Truncation capture mechanism
classification: Independent
blocked_by: [001-core-truncation-contract]
parent: IMPLEMENTATION-CliInvoke-middleware.md
---

## Goal

Implement lossy, per-stream output truncation during buffered capture, and wire the cap from the invocation pipeline into the capture call. Also defines the shared `TruncationDefaults.MaxBytesPerStreamKey` constant in `CliInvoke` (per I001) so the pipeline and the Extensions middleware can share it without a circular dependency.

## What to build

1. Add `TruncationDefaults` (static class) in `CliInvoke` exposing `public const string MaxBytesPerStreamKey = "CliInvoke.Truncation.MaxBytesPerStream"`. (Per I001, this lives in CliInvoke, not Extensions.)
2. Add a NEW overload `ReadAllTextAsync(CancellationToken, long? maxStandardOutputBytes, long? maxStandardErrorBytes)` to `ProcessWrapper` that reads each stream, discards the remainder beyond the limit, and returns `(string StandardOutput, string StandardError, bool WasTruncated)`. NOTE: the existing `ReadAllTextAsync(CancellationToken)` is inherited from `System.Diagnostics.Process` and must NOT be modified/overridden — add a distinct overload.
3. Update `ExternalProcess.CaptureBufferedResultAsync` to accept the optional caps, forward them to the new `ProcessWrapper` overload, and set `result.WasTruncated` from the returned flag.
4. Update `ProcessInvocationPipeline.InvokeAsync` (Buffered branch) to read the cap via `ctx.Middleware?.Items.TryGet<long>(TruncationDefaults.MaxBytesPerStreamKey, out var cap)` and pass the same value to both stdout and stderr caps (or `null` when absent).

## Size

- **Files**: 4 (1 new: TruncationDefaults.cs; 3 edits: ProcessWrapper.cs, ExternalProcess.cs, ProcessInvocationPipeline.cs)
- **Large Files to be created**: omit
- **Large Edits required**: omit

## Recommended Workflow

### Step 1 - Add TruncationDefaults key constant

Where: src/CliInvoke/TruncationDefaults.cs (new)

- Create `internal static class TruncationDefaults { public const string MaxBytesPerStreamKey = "CliInvoke.Truncation.MaxBytesPerStream"; }`

Verify: CliInvoke compiles; constant accessible from pipeline and (via project reference) from Extensions.

### Step 2 - Add truncation overload to ProcessWrapper

Where: src/CliInvoke/Processes/Internal/ProcessWrapper.cs

- Add `public async Task<(string StandardOutput, string StandardError, bool WasTruncated)> ReadAllTextAsync(CancellationToken ct, long? maxStandardOutputBytes = null, long? maxStandardErrorBytes = null)`.
- Read stdout/stderr, truncating each at its cap (discard remainder), set WasTruncated if either exceeded.

Verify: Overload resolves distinctly from the base `Process.ReadAllTextAsync(CancellationToken)`; no base-method override.

### Step 3 - Forward caps in ExternalProcess

Where: src/CliInvoke/Processes/ExternalProcess.cs

- Change `CaptureBufferedResultAsync` signature to accept the two optional `long?` caps; call the new `ProcessWrapper` overload; set `result.WasTruncated` from the returned flag.

Verify: Buffered result carries correct WasTruncated; existing callers (no caps) behave as before.

### Step 4 - Read cap in pipeline

Where: src/CliInvoke/ProcessInvocationPipeline.cs

- In the Buffered branch, read `ctx.Middleware?.Items.TryGet<long>(TruncationDefaults.MaxBytesPerStreamKey, out var cap)`; pass `cap` to both stdout and stderr caps of `CaptureBufferedResultAsync`.

Verify: When the middleware sets the key, capture is capped; when absent, caps are null (unbounded, prior behavior).

## Context pointers

##### Files

- src/CliInvoke/Processes/Internal/ProcessWrapper.cs — add the truncation overload (base method is inherited, do not touch).
- src/CliInvoke/Processes/ExternalProcess.cs — forward caps, set flag.
- src/CliInvoke/ProcessInvocationPipeline.cs — read cap from MiddlewareItems.
- src/CliInvoke.Core/Middleware/MiddlewareItems.cs — TryGet<T>/Set<T> API used by pipeline and middleware.
- src/CliInvoke.Core/Processes/IExternalProcess.cs — the interface whose optional params were added in TK001.

##### ADRs

- None new; honors D002 (middleware pattern) and D004 (pipeline reads capped buffers).

##### Domain terms

- MiddlewareItems — per-invocation shared dictionary; the cap travels here from the middleware (TK003) to the pipeline.
- Buffered invocation mode — the only mode truncation applies to (D003).

##### Ledger records

- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T002 — cap in MiddlewareItems; CaptureBufferedResultAsync optional params; truncate as read; set WasTruncated.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D002 — truncation is a pipeline middleware concern; does not cover IExternalProcess.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D003 — lossy cap semantics.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#I001 — TruncationDefaults key constant placed in CliInvoke (this ticket).

## Acceptance criteria

- [ ] `TruncationDefaults.MaxBytesPerStreamKey` exists in `CliInvoke` and is referenced by the pipeline.
- [ ] New `ProcessWrapper.ReadAllTextAsync(CancellationToken, long?, long?)` overload truncates each stream at its cap and reports `WasTruncated`; the inherited base method is unchanged.
- [ ] `ExternalProcess.CaptureBufferedResultAsync` forwards caps and sets `WasTruncated`.
- [ ] `ProcessInvocationPipeline` reads the cap from `MiddlewareItems` and passes it to both streams; absent key → null caps (prior behavior).
- [ ] `CliInvoke` builds; no circular dependency introduced.

## Dependencies

Blocked by: 001-core-truncation-contract
