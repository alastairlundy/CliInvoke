---
title: Core — Truncation contract surface
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-CliInvoke-middleware.md
---

## Goal

Define the Core API surface that output truncation requires, so the `CliInvoke` capture layer and the `CliInvoke.Extensions` middleware can build on it. Adds a truncation flag to `BufferedProcessResult` and optional per-stream cap parameters to `IExternalProcess.CaptureBufferedResultAsync`.

## What to build

Two small, non-breaking Core changes:

1. Add `public bool WasTruncated { get; set; }` to `BufferedProcessResult` only (not the base `ProcessResult`). The capture layer sets it after construction.
2. Extend `IExternalProcess.CaptureBufferedResultAsync(CancellationToken)` with optional `long? maxStandardOutputBytes = null` and `long? maxStandardErrorBytes = null` parameters (non-breaking — existing callers omit them).

These are contract-only; no behavior change until the capture layer (TK002) and middleware (TK003) consume them.

## Size

- **Files**: 2 (both edits)
- **Large Files to be created**: omit
- **Large Edits required**: omit

## Recommended Workflow

### Step 1 - Add WasTruncated to BufferedProcessResult

Where: src/CliInvoke.Core/Primitives/Results/BufferedProcessResult.cs

- Add `public bool WasTruncated { get; set; }` to the class (alongside StandardOutput/StandardError).
- Keep the existing get-only properties unchanged.

Verify: Project compiles; existing get-only properties unchanged.

### Step 2 - Add optional cap parameters to IExternalProcess

Where: src/CliInvoke.Core/Processes/IExternalProcess.cs

- Change `Task<BufferedProcessResult> CaptureBufferedResultAsync(CancellationToken cancellationToken);` to include `long? maxStandardOutputBytes = null, long? maxStandardErrorBytes = null` (optional, with defaults).

Verify: Interface compiles; existing implementers (ExternalProcess) still satisfy it via optional params.

## Context pointers

##### Files

- src/CliInvoke.Core/Primitives/Results/BufferedProcessResult.cs — add the flag here.
- src/CliInvoke.Core/Primitives/Results/ProcessResult.cs — base class; do NOT add the flag here (T003 constraint).
- src/CliInvoke.Core/Processes/IExternalProcess.cs — extend the capture method signature.

##### ADRs

- None directly; follows existing Core conventions.

##### Domain terms

- BufferedProcessResult — the result type produced by buffered invocation mode; truncation flag applies only here (D003).
- MiddlewareItems — the per-invocation dictionary the truncation cap travels through (see TK002/TK003).

##### Ledger records

- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T003 — WasTruncated on BufferedProcessResult only, not base.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T002 — CaptureBufferedResultAsync gains optional cap parameters (non-breaking).
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D003 — lossy cap + truncation flag semantics.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D001 — session goal (whole set).

## Acceptance criteria

- [ ] `BufferedProcessResult` has a public `bool WasTruncated { get; set; }` and `ProcessResult` does not.
- [ ] `IExternalProcess.CaptureBufferedResultAsync` accepts optional `long? maxStandardOutputBytes` and `long? maxStandardErrorBytes` with `null` defaults; existing callers compile unchanged.
- [ ] `CliInvoke.Core` builds with no new dependencies.

## Dependencies

Blocked by: None - can start immediately
