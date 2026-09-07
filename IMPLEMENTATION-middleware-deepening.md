# Implementation Blueprint — CliInvoke middleware deepening

Session output for `docs/decisions/DECISIONS-CliInvoke-middleware-deepening.md` (the Decision Ledger). This blueprint is a context pointer valid only for that linked ledger. Statement shorthand `#Dxxx` / `#Txxx` cites records in it; file paths are repo-relative.

## Scope Binding

| Ledger record | Surface | Status |
|---|---|---|
| #D002, #T002 | Truncation cap on `ProcessExitConfiguration` | Shipped; recorded final below |
| #T005 | `UseOutputTruncation` thin sugar; `TruncationOptions` | Shipped; `MaxSize` → `MaxBytes` rename applied |
| #T004, #D003 | Single validation path; pre-next merge middleware | Shipped |
| #T003, #D004 | Retry classification surface (`IRetryClassifier`) | Shipped; attempt-context overload pending decision |
| #T006 | Test coverage | Shipped; see the amended T006 record in the ledger |

## Cap Surface — final

Per #D002 and #T002, the truncation cap is an Invocation Capability (GLOSSARY.md) stated on the invocation configuration and read by the pipeline from configuration.

- `long? MaxBufferedOutputBytes` — init-only property on `ProcessExitConfiguration` (`src/CliInvoke.Core/Primitives/ProcessExitConfiguration.cs`); participates in the type's equality. (#T002)
- `ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(ProcessExitConfiguration, long?)` (`src/CliInvoke.Core/Extensions/ProcessExitConfigurationCreationExtensions.cs`) — overwrite semantics; preserves `ValidationRules` by reference. (#T002)
- `ProcessInvocationPipeline` reads `ctx.ExitConfiguration?.MaxBufferedOutputBytes` and forwards the same cap to both standard output and standard error capture (`CaptureBufferedResultAsync(ct, cap, cap)`) (`src/CliInvoke/ProcessInvocationPipeline.cs`). (#T002)
- Per-stream caps stay out of scope (#T002); `IExternalProcess.CaptureBufferedResultAsync` exposes per-stream parameters for direct callers, which the configuration surface does not replicate.
- No `MiddlewareItems` string-key handoff exists for the cap (#D002); `MiddlewareItems` remains for ad-hoc per-invocation state only.
- `CliRun` sets the cap during its config construction (`BuildStringArgsConfig` → `WithMaxBufferedOutputBytes`) on the string-args buffered path; the `ProcessConfiguration` overload passes the caller's exit configuration through untouched. (#T002)

Test evidence (#T006): `tests/CliInvoke.Tests/Processes/TruncationTests.cs`, `tests/CliInvoke.Tests/Invokers/OutputTruncationHandoffTests.cs`, `tests/CliInvoke.Tests/Fuzzing/TruncationCapFuzzTests.cs`.

## Truncation sugar — final

Per #T005:

- `UseOutputTruncation()` / `UseOutputTruncation(TruncationOptions)` remain the public entrypoints (`src/CliInvoke/Extensions/Middleware/Truncation/OutputTruncationMiddlewareExtensions.cs`); the middleware writes the cap onto the invocation's exit configuration before `next` and performs no truncation itself.
- `TruncationOptions` (`src/CliInvoke/Extensions/Middleware/Truncation/TruncationOptions.cs`), final shape after the approved `MaxSize` → `MaxBytes` rename:

```csharp
public sealed class TruncationOptions
{
    public static TruncationOptions Default { get; } = new();
    public long MaxBytes { get; set; } = 1_048_576;
}
```

- Proposed, not applied: `MaxBytes >= 1` validation in the `OutputTruncationMiddleware` constructor, mirroring the `RetryOptions` constructor validation (#D004's constraints establish the options-validation precedent). Pending owner decision.

## Pending

- Retry classification attempt context (#T003): `IRetryClassifier.ShouldRetry(ProcessResult)` is the shipped v3 signature; an additive `ShouldRetry(ProcessResult, RetryAttemptContext)` overload is reserved for a future minor if a need emerges. Not decided.
- Rule/exception API changes (#D003, #T004): out of scope per ledger constraints; the current `IProcessResultValidator` surface (`ValidationRules`, `Validate`, `GetValidationFailures`) stands per the T004-behavior decision.

## Ledger Reference

- Decision ledger: `docs/decisions/DECISIONS-CliInvoke-middleware-deepening.md`
- Tickets: `tickets/001-truncation-handoff-test-coverage.md`, `tickets/002-fscheck-property-tests.md` (both list this file as `parent`)
- Glossary: `GLOSSARY.md` — Invocation Capability
