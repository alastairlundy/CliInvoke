# Decision Ledger — CliInvoke Middleware: Truncation, Caching, Retry

Source of truth for the domain-grilling session on output truncation / size cap,
retry-with-backoff, and cached `IFilePathResolver`. Promotes the candidate
conclusions from `handoff-CliInvoke-middleware-2026-08-25.md` §2.1 / §2.3 to
formal records. Does NOT reconstruct the missing
`DECISIONS-CliInvoke-middleware-design-surprises.md` ledger.

## Goal

### [D001] - session goal

- **Driver**: The user wants to align the domain vocabulary and key design decisions for three candidate CliInvoke features before implementation, so the conclusions can be promoted to Decision Ledger records and then built per the handoff's next-session focus (§4).
- **Resolved Answer**: "Confirm as stated" — grill all three ideas (output truncation/size cap, retry-with-backoff, cached IFilePathResolver) to resolve their domain vocabulary and key design decisions, recording outcomes as D/T/I records in this new ledger.
- **Normalized Requirement**: The session shall resolve the domain vocabulary, bounded contexts, and key design decisions for output truncation/size cap, retry-with-backoff, and cached IFilePathResolver, producing Decision Ledger records (D/T/I) that are implementable per the handoff's §4 next-session focus.
- **Constraints**: Scope limited to these three candidate ideas. Do not reconstruct the missing `DECISIONS-CliInvoke-middleware-design-surprises.md` ledger. The existing GLOSSARY Design Decisions 1/3/4/5 remain in force and bound any resolution.

### [D002] - truncation placement

- **Driver**: The user wants truncation to fit the existing Process Invocation Pipeline pattern and stay consistent with `LoggingMiddleware` and the deliberate middleware/`IExternalProcess` separation.
- **Resolved Answer**: "Option A — Pipeline middleware" (Truncation placement: Option A).
- **Normalized Requirement**: Output truncation shall be implemented as a middleware in the Process Invocation Pipeline that caps the buffer as output is read; it is not a `BufferedProcessResult` behavior and does not cover `IExternalProcess`.
- **Constraints**: Does not cap output invoked via `IExternalProcess` (middleware does not flow there) — that path remains unbounded by design. Not a result-type contract change.

### [D003] - truncation semantics

- **Driver**: The user wants a simple, implementable decision that directly bounds memory growth.
- **Resolved Answer**: "Option A — Lossy cap" (Truncation Semantics: Option A).
- **Normalized Requirement**: When the configured size cap is exceeded, excess output shall be discarded and the result shall expose a truncation flag/metadata indicating output was truncated.
- **Constraints**: Applies to the buffered execution mode's stdout/stderr. Caller must consult the truncation flag to detect incomplete output. Lossless overflow and per-invocation policy are deferred (out of scope for the initial decision).

### [D004] - truncation ↔ LoggingMiddleware interaction

- **Driver**: The user wants a low-risk decision that keeps `LoggingMiddleware` unchanged and bounds memory earliest, consistent with D002/D003.
- **Resolved Answer**: "Option A — Truncation upstream" (D004: Option A).
- **Normalized Requirement**: The truncation middleware shall be ordered upstream of `LoggingMiddleware` in the Process Invocation Pipeline, so `LoggingMiddleware` observes already-capped buffers and its existing buffer special-casing remains valid.
- **Constraints**: Logs reflect only capped output; the dropped portion is not recorded in logs. No refactor of `LoggingMiddleware` internals.

### [D005] - retry idempotency boundary

- **Driver**: The user prefers the simplest caller experience — retry applies by default to any classified failure rather than requiring an explicit opt-in.
- **Resolved Answer**: "Option B — Assume retry-safe by default" (D005: Option B).
- **Normalized Requirement**: The retry middleware shall retry by default for any failure classified as retryable; idempotency is not asserted by the caller as a precondition to retrying.
- **Constraints**: Risks re-executing side-effecting commands (deploy, file write) with duplicate effects on retry. Callers must avoid retry for non-idempotent invocations by not classifying them as retryable.

### [D006] - retry trigger model

- **Driver**: The user wants a single classifier extension point, but with the concrete rules implementation shared from the `CliInvoke` package so `CliInvoke.Extensions` can reuse it.
- **Resolved Answer**: "Option A — Single `IProcessResultClassifier`, with a concrete implementation in the `CliInvoke` package that `CliInvoke.Extensions` can use. The classifier just does the validation. The configuration/rules are provided in DI or through the classifier ctor." (D006: Option A + elaboration)
- **Normalized Requirement**: Retry triggering shall use a single `IProcessResultClassifier` interface; a concrete classifier implementation shall live in the `CliInvoke` package and be consumable by `CliInvoke.Extensions`; the classifier performs validation, and its rules/configuration are supplied via DI or the classifier constructor.
- **Constraints**: The classifier interface itself does not embed the rule data; rules come from DI or ctor injection. `CliInvoke.Extensions` depends on the `CliInvoke` classifier implementation.

### [D007] - backoff strategy model

- **Driver**: The user wants the simplest backoff model (inline enum) but with an explicit, well-named enum.
- **Resolved Answer**: "Option B — Enum in `RetryOptions`, with the enum named `RetryBackoffStrategy`." (D007: Option B + enum name)
- **Normalized Requirement**: Backoff shall be selected via an enum property on `RetryOptions` named `RetryBackoffStrategy` (e.g., Fixed, Exponential); no separate strategy type is introduced.
- **Constraints**: Custom backoff curves are not expressible via the enum (deferred). `RetryOptions` also carries max attempts and base delay.

### [D008] - cache lifetime vs resolver lifetime

- **Driver**: The user wants to honor GLOSSARY Design Decision 5 while still sharing the cache across scopes.
- **Resolved Answer**: "Option A — Resolver follows global lifetime; cache injected `Singleton`" (D008: Option A).
- **Normalized Requirement**: `CachingFilePathResolver` shall be registered with the global `lifetime` (default Scoped per Design Decision 5) and receive `IMemoryCache` (a `Singleton`) via DI; the cache store is shared across scopes.
- **Constraints**: The resolver wrapper instance is per-scope; only the cache is shared. The resolver is not special-cased to `Singleton`.

### [D009] - cache key & invalidation vs resolution-order

- **Driver**: The user wants to cache the expensive resolution while preserving the PATH-first order on cache miss.
- **Resolved Answer**: "Option A — Key on raw target; cache resolved absolute path" (D009: Option A).
- **Normalized Requirement**: The cache key shall be the raw target path argument; the cached value shall be the resolved absolute path; `AbsoluteTtl` (default ~5 min) bounds staleness, with `PostEvictionCallback` for invalidation hooks.
- **Constraints**: On cache miss, resolution still runs PATH-first per Design Decision 1. A stale entry within TTL may return a now-invalid path; mitigated by TTL + eviction callback, not eliminated.

### [D010] - package placement

- **Driver**: The user wants to match the handoff's placement and keep `CliInvoke.Core` free of caching dependencies.
- **Resolved Answer**: "Option A — `CliInvoke.Extensions`" (D010: Option A).
- **Normalized Requirement**: `CachingFilePathResolver` and `UseCachingFilePathResolver()` shall ship in `CliInvoke.Extensions`; `CliInvoke.Core` shall not take a caching dependency.
- **Constraints**: Caching is unavailable to Core-only consumers. A separate `CliInvoke.Extensions.Caching` package is deferred unless `IDistributedCache` is later required.

### [D011] - truncation default cap size

- **Driver**: The user wants a default that bounds memory out of the box while staying overridable.
- **Resolved Answer**: "Option A — Conservative default (1 MB)" (D011: Option A).
- **Normalized Requirement**: The truncation middleware shall use a default cap of 1 MB when the caller does not specify a cap; the cap shall be overridable via options.
- **Constraints**: 1 MB is a fixed default; callers with larger legitimate outputs must opt in to raise it. Per-invocation configurability is assumed available (not decided here).

### [D012] - retry default max attempts

- **Driver**: The user wants a conventional retry default that tolerates transient failures.
- **Resolved Answer**: "Option A — Default 3 attempts" (D012: Option A).
- **Normalized Requirement**: The retry middleware shall use a default max attempts of 3 (1 initial + 2 retries) when the caller does not specify; retry still applies by default to classified failures per D005.
- **Constraints**: Base delay and the default `RetryBackoffStrategy` value are not set by this record (separate decisions).

### [D013] - cache default SizeLimit

- **Driver**: The user wants a bounded default tighter than 1024 — splitting the difference between Option A (1024) and Option B (no limit).
- **Resolved Answer**: "Hybrid of A and B — default SizeLimit = 512 entries" (D013: split difference → 512).
- **Normalized Requirement**: The `CachingFilePathResolver`'s `IMemoryCache` shall use a default `SizeLimit` of 512 entries when the caller does not specify; overridable via `CachingFilePathResolverOptions`.
- **Constraints**: Bounded (not unbounded); 512 is a midpoint between 1024 (A) and no-limit (B). High-churn resolvers may still evict at 512.

### [D014] - retry default RetryBackoffStrategy value

- **Driver**: The user wants a resilient retry default consistent with D007's strategy enum.
- **Resolved Answer**: "Option A — Default Exponential" (D014: Option A).
- **Normalized Requirement**: The retry middleware shall use `RetryBackoffStrategy.Exponential` as the default when the caller does not specify a strategy in `RetryOptions`.
- **Constraints**: The exponential base delay value is not set by this record (separate decision). `Fixed` remains available via the enum.

### [D015] - cache default expiration policy & TTL

- **Driver**: The user wants a default that bounds staleness to a fixed window, consistent with D009's `AbsoluteTtl` intent.
- **Resolved Answer**: "Option A — Absolute, 5 min default" (D015: Option A).
- **Normalized Requirement**: The `CachingFilePathResolver`'s `IMemoryCache` shall use `AbsoluteExpirationRelativeToNow` of 5 minutes as the default expiration; overridable via `CachingFilePathResolverOptions`.
- **Constraints**: Sliding expiration is not the default. `PostEvictionCallback` (D009) remains the invalidation hook. Default `SizeLimit` is 512 (D013).

### [T001] - truncation middleware placement & registration API

- **Driver**: The user wants truncation to fit the existing Process Invocation Pipeline pattern and stay consistent with `LoggingMiddleware` and the deliberate middleware/`IExternalProcess` separation (D002).
- **Resolved Answer**: "CliInvoke.Extensions + `UseOutputTruncation()`"
- **Normalized Requirement**: Truncation shall be implemented as `internal sealed class OutputTruncationMiddleware : IProcessMiddleware` in `CliInvoke.Extensions` (namespace `CliInvoke.Extensions.Middleware`), registered via a `UseOutputTruncation()` extension method on `IProcessMiddlewareBuilder` (C# 14 `extension` syntax, mirroring `UseLogging()`). `CliInvoke.Core` shall not gain middleware implementation.
- **Constraints**: Opt-in (not registered by default in `AddCliInvoke`). Does not apply to `IExternalProcess` (middleware does not flow there). Registration method name is `UseOutputTruncation()`, not `UseTruncation()`.
- **Cites**: D002, D004

### [T002] - truncation capping mechanism

- **Driver**: The user wants a simple, implementable decision that directly bounds memory growth while keeping `LoggingMiddleware` unchanged and observing already-capped buffers (D003, D004).
- **Resolved Answer**: "Cap stored in `MiddlewareItems`; `CaptureBufferedResultAsync` gains direct optional `maxStandardOutputBytes`/`maxStandardErrorBytes` parameters"
- **Normalized Requirement**: `OutputTruncationMiddleware` shall write the configured per-stream cap (stdout + stderr) into `InvocationContext.Middleware.Items` under a constant key (e.g. `TruncationDefaults.MaxBytesPerStreamKey`) before calling `next`. `ProcessInvocationPipeline` shall read that entry and forward it to `IExternalProcess.CaptureBufferedResultAsync` via added optional parameters `long? maxStandardOutputBytes = null` and `long? maxStandardErrorBytes = null` (non-breaking). The capture shall truncate each stream as it is read and set `WasTruncated` on the result.
- **Constraints**: Cap is per-stream (each stream capped at `MaxSize`); `LoggingMiddleware` observes capped output regardless of registration order. No new `InvocationContext` property is added. `PipedProcessResult` is out of scope for truncation (D003).
- **Cites**: D002, D003, D004

### [T003] - result truncation flag & options class

- **Driver**: The user wants a default that bounds memory out of the box while staying overridable (D011), and a simple flag to detect incomplete output (D003).
- **Resolved Answer**: "Option A — `WasTruncated` on `BufferedProcessResult` + `TruncationOptions`"
- **Normalized Requirement**: Add a `bool WasTruncated { get; set; }` property to `BufferedProcessResult` only (not the base `ProcessResult`). Add a `public sealed class TruncationOptions` POCO with a `long MaxSize` property and a `static TruncationOptions Default { get; }` singleton returning `MaxSize = 1_048_576` (1 MB), following the `PowerShellMiddlewareOptions` pattern. The middleware reads `MaxSize` from `TruncationOptions` (via DI/registration).
- **Constraints**: Applies to buffered execution mode only (D003). Per-invocation configurability is out of scope (D011). `CliInvoke.Core` holds the `BufferedProcessResult` property; the `TruncationOptions` POCO lives with the middleware in `CliInvoke.Extensions`.
- **Cites**: D003, D011

<!-- next-d: D016 -->
<!-- next-i: I001 -->
### [T004] - retry classification reuses `IProcessResultValidator<T>`

- **Driver**: The user wants to avoid duplicating the existing `IProcessResultValidator<T>` rule engine while keeping retry classification retry-oriented rather than overloading `Validate` (which is success-semantics).
- **Resolved Answer**: "Add `bool ShouldRetry(TProcessResult)` to `IProcessResultValidator<TProcessResult>` (default `=> Validate(result)`); retry middleware calls it on a retryable-conditions validator. No new classifier type."
- **Normalized Requirement**: `IProcessResultValidator<TProcessResult>` shall gain a `bool ShouldRetry(TProcessResult result)` member with a default interface implementation returning `Validate(result)`, so existing implementers are unaffected. The retry middleware shall accept an `IProcessResultValidator<ProcessResult>` instance whose configured rules define retryable outcomes and shall decide retry via `ShouldRetry(result)`. No separate `IProcessResultClassifier` type shall be introduced.
- **Constraints**: `ShouldRetry` reuses the existing rule engine; the success validators used by `PostExitValidationMiddleware` are never queried via `ShouldRetry`. Core.Validation gains one retry-named member.
- **Cites**: D005, D006

### [T005] - `RetryOptions` shape & `RetryBackoffStrategy` enum

- **Driver**: The user wants a single options type encoding the retry configuration with the ledger-mandated defaults.
- **Resolved Answer**: "Option A — `RetryOptions` POCO + `RetryBackoffStrategy` enum"
- **Normalized Requirement**: Add `public enum RetryBackoffStrategy { Fixed, Exponential }` and `public sealed class RetryOptions` with `int MaxAttempts` (default 3), `TimeSpan BaseDelay` (default 100ms, convention), `RetryBackoffStrategy Strategy` (default Exponential), plus a `static RetryOptions Default { get; }` singleton, following the `PowerShellMiddlewareOptions` pattern. Lives with the retry middleware in `CliInvoke.Extensions`.
- **Constraints**: `BaseDelay` default (100ms) is a convention, not ledger-bound (D007/D012/D014 set only MaxAttempts and Strategy defaults). `CliInvoke.Core` stays free of retry options.
- **Cites**: D007, D012, D014

### [T006] - `RetryMiddleware` & `UseRetryPolicy()` registration

- **Driver**: The user wants retry to apply by default to classified failures via the standard middleware + `UseXxx` pattern, with an ASP.NET Core-aligned name (Polly `AddPolicyHandler` / `AddResilienceHandler`).
- **Resolved Answer**: "Option A — `RetryMiddleware` in Extensions; `UseRetryPolicy()` (renamed from `UseRetry()`)"
- **Normalized Requirement**: Implement `internal sealed class RetryMiddleware : IProcessMiddleware` in `CliInvoke.Extensions`; its constructor accepts `IProcessResultValidator<ProcessResult>` (retryable conditions) and `RetryOptions` from DI. It shall invoke `await next(context)`, then while `retryableConditions.ShouldRetry(ctx.Result)` is true and attempts remain, re-invoke `next`. Register via `UseRetryPolicy()` (and overloads accepting a custom `IProcessResultValidator<ProcessResult>` and/or `RetryOptions`) on `IProcessMiddlewareBuilder`, mirroring `UseLogging()`/`UsePostExitValidation()`.
- **Constraints**: Retry by default for any failure the validator classifies retryable (D005); re-invoking `next` re-executes the process (intended). Callers avoid retry for non-idempotent invocations by not classifying them retryable (D005). Registration name is `UseRetryPolicy()`, not `UseRetry()`.
- **Cites**: D005, D006

<!-- next-d: D016 -->
<!-- next-i: I001 -->
### [T007] - `CachingFilePathResolver` class shape

- **Driver**: The user wants to cache resolved paths without duplicating the resolution algorithm, while honoring D009's raw-target→absolute-path key/value and D010's Extensions placement.
- **Resolved Answer**: "Option A — `CachingFilePathResolver : IFilePathResolver` decorator in CliInvoke.Extensions"
- **Normalized Requirement**: Implement `public sealed class CachingFilePathResolver : IFilePathResolver` in `CliInvoke.Extensions`. Its constructor accepts an inner `IFilePathResolver` (the resolver being decorated) and an `IMemoryCache` (Singleton). Both `ResolveFilePath` and `TryResolveFilePath` shall check the cache keyed on the raw target argument; on a miss, delegate to the inner resolver (which runs PATH-first per GLOSSARY DD1), cache the resolved absolute `FileInfo` with `AbsoluteExpirationRelativeToNow` (from options) and a `PostEvictionCallback` (D009), then return. `Microsoft.Extensions.Caching.Memory` is added to `CliInvoke.Extensions` only; `CliInvoke.Core` stays free of caching (D010).
- **Constraints**: Cache key is the raw target; cached value is the resolved absolute path. On miss, resolution runs PATH-first per DD1. `IMemoryCache` is the shared Singleton; the resolver wrapper instance follows the global lifetime (D008). Inheritance from `FilePathResolver` was rejected because its virtual extension points are leaf strategies, not the top-level resolve, and `ResolveFilePath` is non-virtual (`new`), so it cannot intercept at D009 granularity without duplication.
- **Cites**: D009, D010, D008

### [T008] - `CachingFilePathResolverOptions` shape

- **Driver**: The user wants bounded, overridable defaults for the cache (D013 SizeLimit 512, D015 5 min) plus the D009 invalidation hook.
- **Resolved Answer**: "Option A — `CachingFilePathResolverOptions` POCO"
- **Normalized Requirement**: Add `public sealed class CachingFilePathResolverOptions` with `int SizeLimit` (default 512), `TimeSpan AbsoluteExpirationRelativeToNow` (default 5 minutes), and an optional `PostEvictionCallback?` delegate, plus a `static CachingFilePathResolverOptions Default { get; }` singleton, following the `PowerShellMiddlewareOptions` pattern. Lives in `CliInvoke.Extensions`.
- **Constraints**: `SizeLimit` is applied to the shared `IMemoryCache` (`MemoryCacheOptions`), not per-resolver instance (D013). Sliding expiration is not the default (D015). `CliInvoke.Core` stays free of caching options.
- **Cites**: D013, D015, D009

### [T009] - caching DI registration & lifetime

- **Driver**: The user wants caching wired via DI honoring D008 (resolver global lifetime, `IMemoryCache` Singleton) and D010 (`UseCachingFilePathResolver()` in Extensions).
- **Resolved Answer**: "Option A — `UseCachingFilePathResolver()` on `IServiceCollection`"
- **Normalized Requirement**: Add `UseCachingFilePathResolver()` (and an overload accepting `Action<CachingFilePathResolverOptions>` to override SizeLimit/TTL) as a `static` extension on `IServiceCollection` in `CliInvoke.Extensions`. It shall (1) ensure `IMemoryCache` is registered as Singleton with `MemoryCacheOptions.SizeLimit` taken from `CachingFilePathResolverOptions` (default 512); (2) decorate the currently-registered `IFilePathResolver` by capturing its implementation as the inner resolver and re-registering `IFilePathResolver` as `CachingFilePathResolver` (injecting the inner `IFilePathResolver`, the Singleton `IMemoryCache`, and the options) at the global `lifetime` (default Scoped per DD5). The resolver wrapper instance is per-scope; only the cache is shared (D008).
- **Constraints**: Registration name is `UseCachingFilePathResolver()` (D010). The resolver follows the global lifetime, not Singleton (D008). Decorator swap must avoid a circular `IFilePathResolver` dependency (resolve the inner from the prior registration, not from `IFilePathResolver` directly). `CliInvoke.Core` takes no caching dependency.
- **Cites**: D008, D010

### [I001] - `TruncationDefaults` key constant placement vs circular dependency

- **Prompt**: The blueprint places `TruncationDefaults` (the `MaxBytesPerStreamKey` constant) in `CliInvoke.Extensions`, but `ProcessInvocationPipeline` lives in `CliInvoke`, which references only `CliInvoke.Core` and not `CliInvoke.Extensions`. Having `CliInvoke` reference `CliInvoke.Extensions` would create a circular dependency (Extensions already depends on CliInvoke). Where should the `MaxBytesPerStreamKey` constant be defined so both the pipeline (CliInvoke) and the middleware (Extensions) can share it?
- **User Response**: "In CliInvoke (Recommended) — define `TruncationDefaults` (the `MaxBytesPerStreamKey` constant) in the `CliInvoke` project; `CliInvoke.Extensions` references it."
- **Resolution**: Drove the decomposition - `TruncationDefaults` (key constant) is placed in `CliInvoke` (folded into ticket TK002), so `ProcessInvocationPipeline` (CliInvoke) can read the key and `OutputTruncationMiddleware` (Extensions) can write it without a circular dependency. `TruncationOptions` and `OutputTruncationMiddleware` remain in Extensions (TK003).
- **Notes**: Minor deviation from the blueprint's File Changes list, which placed `TruncationDefaults.cs` under `CliInvoke.Extensions/Middleware/Truncation/`. Only the constant moves to `CliInvoke`; the middleware and options stay in Extensions.

<!-- next-d: D016 -->
<!-- next-i: I002 -->
<!-- next-t: T010 -->
