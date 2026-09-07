---
title: CliInvoke — FsCheck property tests for the new deepening surfaces
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-middleware-deepening.md
---

## Goal

Add FsCheck property tests for the three new surfaces introduced by the middleware deepening — cap plumbing, retry-policy dispatch, and validation rule merging — per T006's Option C decision (migrate plus property tests). Per T006 constraints, property tests target the new surfaces, **not** parsers; individual assertions are blueprint detail, so the properties below are the required shape, not an exhaustive list.

Existing FsCheck infrastructure lives in `tests/CliInvoke.Tests/Fuzzing/` (see `ArgumentTokenizerFuzzTests.cs` for conventions). The retry-dispatch surface lives in `CliInvoke.Extensions` behind an internal middleware, so its properties go in `tests/CliInvoke.Extensions.Tests/`, which requires adding the FsCheck package reference to that test project (version is centrally managed).

## What to build

1. `tests/CliInvoke.Tests/Fuzzing/TruncationCapFuzzTests.cs` — properties over arbitrary `long?` caps and arbitrary exit configurations:
   - `ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(config, cap)` round-trips the cap (`result.MaxBufferedOutputBytes == cap`, including `null` → `null`).
   - The copy preserves the other exit-configuration fields (exception behaviour, validation rules).
   - `ProcessExitConfiguration` equality reflects the cap value.
2. `tests/CliInvoke.Tests/Fuzzing/ValidationRuleMergeFuzzTests.cs` — properties over arbitrary rule arrays:
   - `WithValidationRules(config, rules)` produces a configuration whose rules are exactly the supplied rules (order-preserving) without mutating the source configuration.
   - The copy preserves `MaxBufferedOutputBytes` (merging rules must not disturb the cap seam).
3. `tests/CliInvoke.Extensions.Tests/Middleware/Retry/RetryDispatchFuzzTests.cs` — properties over arbitrary classifiers and retry options:
   - `RetryMiddleware` retries a failed result iff `IRetryClassifier.ShouldRetry(result)` is `true` for that result.
   - Total invocation attempts never exceed `RetryOptions.MaxAttempts` (the budget includes the initial attempt; values below 1 are rejected by the middleware).
   - A result the classifier accepts is never retried.
4. `tests/CliInvoke.Extensions.Tests/CliInvoke.Extensions.Tests.csproj` — add `<PackageReference Include="FsCheck" />` (version resolves via Central Package Management; do not pin a version locally).

## Size

- **Files**: 4 (3 new test files; 1 csproj edit)
- **Large Files to be created**: omit
- **Large Edits required**: omit

## Recommended Workflow

### Step 1 - Add FsCheck to CliInvoke.Extensions.Tests

Where: tests/CliInvoke.Extensions.Tests/CliInvoke.Extensions.Tests.csproj

- Add the `FsCheck` PackageReference (no version attribute; CPM supplies it).

Verify: `dotnet restore` succeeds; `tests/Directory.Packages.props` already defines the FsCheck version (3.4.0) used by `tests/CliInvoke.Tests/CliInvoke.Tests.csproj`.

### Step 2 - Cap plumbing properties

Where: tests/CliInvoke.Tests/Fuzzing/TruncationCapFuzzTests.cs (new)

- Follow the `Prop.ForAll<T>(...)` + TUnit `[Test]` conventions from `ArgumentTokenizerFuzzTests.cs`.

Verify: `dotnet test` from `tests/CliInvoke.Tests/` passes the new properties.

### Step 3 - Rule merging properties

Where: tests/CliInvoke.Tests/Fuzzing/ValidationRuleMergeFuzzTests.cs (new)

- Generate small arrays of `ValidationRule<ProcessResult>` instances (delegates are fine).

Verify: Same test run passes.

### Step 4 - Retry dispatch properties

Where: tests/CliInvoke.Extensions.Tests/Middleware/Retry/RetryDispatchFuzzTests.cs (new)

- Drive `RetryMiddleware` with generated classifiers (map a generated `bool` per distinct result) and generated `RetryOptions.MaxAttempts` values; count invocations of the inner invocation delegate.
- Construct the middleware directly (the test project has internal access, mirroring `OutputTruncationMiddlewareTests`).

Verify: `dotnet test` from `tests/CliInvoke.Extensions.Tests/` passes; generated classifiers must be deterministic per result so the iff-property is well-defined.

## Context pointers

##### Files

- src/CliInvoke.Core/Extensions/ProcessExitConfigurationCreationExtensions.cs — `WithMaxBufferedOutputBytes`, `WithValidationRules` (the copy functions under test).
- src/CliInvoke.Core/Primitives/ProcessExitConfiguration.cs — `MaxBufferedOutputBytes` participates in equality.
- src/CliInvoke.Core/Middleware/IRetryClassifier.cs — `ShouldRetry(ProcessResult)`; the classification hook from T003 (implemented as `IRetryClassifier`, the ledger's `IRetryPolicy` placeholder name).
- src/CliInvoke/Extensions/Middleware/Retry/RetryMiddleware.cs — consumes the classifier; `RetryMiddleware(IRetryClassifier, RetryOptions)`.
- src/CliInvoke/Extensions/Middleware/Retry/RetryOptions.cs — `MaxAttempts` (includes the initial attempt), `BaseDelay`, `Strategy`.
- tests/CliInvoke.Tests/Fuzzing/ArgumentTokenizerFuzzTests.cs — FsCheck + TUnit conventions to follow.
- tests/CliInvoke.Extensions.Tests/Middleware/Truncation/OutputTruncationMiddlewareTests.cs — precedent for constructing internal Extensions middleware directly in tests.

##### ADRs

- None new; honors D002/D003 (capability vs. contract ownership) and D004 (retryability owned by the retry middleware's own surface).

##### Domain terms

- Invocation Capability — cap and validation rules are parameters of the invocation contract (GLOSSARY.md).
- Middleware concern — retry is cross-cutting composition; the classifier decides, the middleware enforces.

##### Ledger records

- DECISIONS-CliInvoke-middleware-deepening.md#T006 — Option C: migrate plus property tests; new coverage includes FsCheck property tests for the new surfaces; property tests target the new surfaces (cap plumbing, retry policy dispatch, rule merging), not parsers. Primary source for this ticket.
- DECISIONS-CliInvoke-middleware-deepening.md#T002 — cap plumbing surface.
- DECISIONS-CliInvoke-middleware-deepening.md#T003 — retry classification surface (named `IRetryClassifier` in code).
- DECISIONS-CliInvoke-middleware-deepening.md#T004 — rule-merging surface (thin pre-next validation middleware; the pipeline is the only evaluator).

Note: the blueprint file `IMPLEMENTATION-middleware-deepening.md` is absent from the repo; the Decision Ledger is authoritative.

## Acceptance criteria

- [ ] FsCheck properties exist for all three surfaces: cap plumbing, retry dispatch, rule merging.
- [ ] No property test targets a parser (those already exist in `tests/CliInvoke.Tests/Fuzzing/` and are out of scope).
- [ ] `tests/CliInvoke.Extensions.Tests/CliInvoke.Extensions.Tests.csproj` references FsCheck via CPM (no local version pin).
- [ ] All new properties pass from both test project working directories.

## Dependencies

Blocked by: none
