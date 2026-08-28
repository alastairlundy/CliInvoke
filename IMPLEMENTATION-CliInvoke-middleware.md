# Implementation Blueprint — CliInvoke Middleware: Truncation, Retry, Cached FilePathResolver

## Scope Binding

- **Linked Spec:** `docs/decisions/DECISIONS-CliInvoke-middleware-truncation-caching-retry.md`
- **Decision Ledger:** `docs/decisions/DECISIONS-CliInvoke-middleware-truncation-caching-retry.md`
- **Notice:** This blueprint is a context pointer valid ONLY for the linked spec and ledger. Do not apply it to other specifications without explicit authorization.

## Summary

Implements three features resolved in the ledger: output truncation (T001–T003), retry-with-backoff (T004–T006), and a cached `IFilePathResolver` (T007–T009). Changes are confined to `CliInvoke.Core`, `CliInvoke`, and `CliInvoke.Extensions`. `CliInvoke.Core` gains no caching or middleware-implementation dependency; the caching package is added only to `CliInvoke.Extensions` [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D010`].

## File Changes

### `src/CliInvoke.Core/Primitives/Results/BufferedProcessResult.cs`
- Add `public bool WasTruncated { get; set; }` to `BufferedProcessResult` only (not the base `ProcessResult`) [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T003`].

### `src/CliInvoke.Core/Processes/IExternalProcess.cs`
- Extend `Task<BufferedProcessResult> CaptureBufferedResultAsync(CancellationToken cancellationToken)` with optional parameters `long? maxStandardOutputBytes = null` and `long? maxStandardErrorBytes = null` (non-breaking) [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T002`].

### `src/CliInvoke.Core/Validation/IProcessResultValidator.cs`
- Add `bool ShouldRetry(TProcessResult result)` to `IProcessResultValidator<TProcessResult>` with a default interface implementation `=> Validate(result)`, so existing implementers are unaffected [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T004`]. (`ProcessResultValidator<T>` requires no edit.)

### `src/CliInvoke/Processes/ExternalProcess.cs`
- Implement `CaptureBufferedResultAsync` with the new cap parameters; forward them to `_processWrapper.ReadAllTextAsync(cancellationToken, maxStandardOutputBytes, maxStandardErrorBytes)`; set `result.WasTruncated` from the capture's `WasTruncated` flag [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T002`, `DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T003`].

### `src/CliInvoke/Processes/Internal/ProcessWrapper.cs`
- Extend `ReadAllTextAsync` to accept `long? maxStandardOutputBytes` / `long? maxStandardErrorBytes`; truncate each stream as it is read (discard the remainder beyond the limit) and return `(string StandardOutput, string StandardError, bool WasTruncated)` [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T002`].

### `src/CliInvoke/ProcessInvocationPipeline.cs`
- In `InvokeAsync`, read the truncation cap via `ctx.Middleware?.Items.TryGet<long>(TruncationDefaults.MaxBytesPerStreamKey, out var cap)` and forward it to `CaptureBufferedResultAsync`, passing the same value to both the stdout and stderr caps [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T002`].

### `src/CliInvoke.Extensions/Middleware/Truncation/TruncationDefaults.cs` (new)
- `public static class TruncationDefaults` exposing `public const string MaxBytesPerStreamKey = "CliInvoke.Truncation.MaxBytesPerStream"` — the `MiddlewareItems` key used to carry the cap [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T002`].

### `src/CliInvoke.Extensions/Middleware/Truncation/TruncationOptions.cs` (new)
- `public sealed class TruncationOptions` with `long MaxSize` and `static TruncationOptions Default { get; }` returning `MaxSize = 1_048_576` (1 MB), following the `PowerShellMiddlewareOptions` pattern [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T003`].

### `src/CliInvoke.Extensions/Middleware/Truncation/OutputTruncationMiddleware.cs` (new)
- `internal sealed class OutputTruncationMiddleware : IProcessMiddleware`; in `InvokeAsync`, before `next`, write `context.Middleware.Items.Set<long>(TruncationDefaults.MaxBytesPerStreamKey, _options.MaxSize)`; otherwise pass through [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T001`, `DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T002`, `DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T003`].
- `public static class OutputTruncationMiddlewareExtensions` with `extension(IProcessMiddlewareBuilder builder)` providing `UseOutputTruncation()` (and an overload accepting `TruncationOptions`), mirroring `UseLogging()` [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T001`].

### `src/CliInvoke.Extensions/Middleware/Retry/RetryBackoffStrategy.cs` (new)
- `public enum RetryBackoffStrategy { Fixed, Exponential }` [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T005`].

### `src/CliInvoke.Extensions/Middleware/Retry/RetryOptions.cs` (new)
- `public sealed class RetryOptions` with `int MaxAttempts` (default 3), `TimeSpan BaseDelay` (default 100 ms, convention), `RetryBackoffStrategy Strategy` (default Exponential), and `static RetryOptions Default { get; }`, following the `PowerShellMiddlewareOptions` pattern [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T005`].

### `src/CliInvoke.Extensions/Middleware/Retry/RetryMiddleware.cs` (new)
- `internal sealed class RetryMiddleware : IProcessMiddleware`; constructor takes `IProcessResultValidator<ProcessResult> retryableConditions` and `RetryOptions` from DI. In `InvokeAsync`, call `await next(context)`; while `retryableConditions.ShouldRetry(ctx.Result)` is true and attempts remain, re-invoke `next` with the configured backoff [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T006`, `DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T004`].

### `src/CliInvoke.Extensions/Middleware/Retry/RetryMiddlewareExtensions.cs` (new)
- `public static class RetryMiddlewareExtensions` with `extension(IProcessMiddlewareBuilder builder)` providing `UseRetryPolicy()` (default options + default retryable-conditions validator) and overloads accepting a custom `IProcessResultValidator<ProcessResult>` and/or `RetryOptions` [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T006`].

### `src/CliInvoke.Extensions/Caching/CachingFilePathResolverOptions.cs` (new)
- `public sealed class CachingFilePathResolverOptions` with `int SizeLimit` (default 512), `TimeSpan AbsoluteExpirationRelativeToNow` (default 5 minutes), `PostEvictionCallback? PostEvictionCallback`, and `static CachingFilePathResolverOptions Default { get; }` [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T008`].

### `src/CliInvoke.Extensions/Caching/CachingFilePathResolver.cs` (new)
- `public sealed class CachingFilePathResolver : IFilePathResolver`; constructor takes an inner `IFilePathResolver` and `IMemoryCache` (Singleton). Both `ResolveFilePath` and `TryResolveFilePath` check the cache keyed on the raw target; on a miss, delegate to the inner resolver (which runs PATH-first per GLOSSARY DD1), cache the resolved absolute `FileInfo` with `AbsoluteExpirationRelativeToNow` and `PostEvictionCallback`, then return [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T007`].

### `src/CliInvoke.Extensions/DependencyInjection/CachingFilePathResolverExtensions.cs` (new)
- `public static class CachingFilePathResolverExtensions` with `UseCachingFilePathResolver()` and `UseCachingFilePathResolver(Action<CachingFilePathResolverOptions> configure)` on `IServiceCollection`. Ensures `IMemoryCache` is registered as Singleton with `MemoryCacheOptions.SizeLimit` from options (default 512); decorates the currently-registered `IFilePathResolver` (capturing its implementation as the inner) and re-registers `IFilePathResolver` as `CachingFilePathResolver` at the global `lifetime` (default Scoped) [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T009`].

### `src/Directory.Packages.props`
- Add a `Microsoft.Extensions.Caching.Memory` version entry (Central Package Management) [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T007`, `DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T009`, `DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D010`].

### `src/CliInvoke.Extensions/CliInvoke.Extensions.csproj`
- Add `<PackageReference Include="Microsoft.Extensions.Caching.Memory" />` (no Version; CPM) [`DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T007`, `DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T009`, `DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D010`].

## Ledger Reference

**Design decisions (D):** D001, D002, D003, D004, D005, D006, D007, D008, D009, D010, D011, D012, D013, D014, D015.

**Technical decisions (T):** T001, T002, T003, T004, T005, T006, T007, T008, T009.
