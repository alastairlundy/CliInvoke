# IExternalProcess Migration Reference

This reference maps v2-style `IExternalProcess` / `ExternalProcess`
patterns to v3 replacements. It mirrors the [`IExternalProcess` Users walkthrough](../../../site/docs/migration-guides/3.0.0.md#iexternalprocess-users) in the migration guide.

## ExternalProcess is sealed

`ExternalProcess` is now `sealed` — it can no longer be subclassed.
If you subclassed the builder, switch to composing an
`IProcessConfigurationBuilder` instead.

## Constructor C only

`ExternalProcess` keeps only constructor C:
`(IFilePathResolver, ProcessConfiguration, ProcessExitConfiguration?)`.

The `(IFilePathResolver, string)` and
`(ProcessConfiguration, ProcessExitConfiguration?)` constructors were
removed.

### Before (v2-style)

```csharp
// v2: ExternalProcess with two-arg constructor and mutable ExitConfiguration
IFilePathResolver resolver = new FilePathResolver();
ExternalProcess process = new ExternalProcess(resolver, "dotnet");
process.ExitConfiguration = ProcessExitConfiguration.CreateGraceful();
await process.StartAsync(CancellationToken.None);
await process.WaitForExitOrTimeoutAsync(CancellationToken.None);
```

### After (v3)

```csharp
// v3: ExternalProcess is sealed, constructor C only, ExitConfiguration at construction
IFilePathResolver resolver = new FilePathResolver();
using ExternalProcess process = new ExternalProcess(
    resolver,
    new ProcessConfiguration("dotnet", "--version"),
    ProcessExitConfiguration.CreateGraceful());
await process.StartAsync(CancellationToken.None);
await process.WaitForExitOrTimeoutAsync(CancellationToken.None);
```

## ExitConfiguration is read-only

`IExternalProcess.ExitConfiguration` and
`ExternalProcess.ExitConfiguration` are now read-only `{ get; }` and
supplied at construction. There is no `WithExitConfiguration` method
and no setter.

## Lifecycle sequence

The `IExternalProcess` lifecycle in v3:

1. **Construct** — `new ExternalProcess(resolver, configuration, exitConfig)`
2. **Start** — `await process.StartAsync(CancellationToken.None)`
3. **Observe** — subscribe to `Started`/`Exited` events, or poll
   `HasStarted`/`HasExited`
4. **Capture** — `await process.WaitForExitOrTimeoutAsync(token)` or
   `await process.CaptureBufferedResultAsync(token)`
5. **Dispose** — `process.Dispose()` (or `using`)

## PipedProcessResult removed

`PipedProcessResult` was removed in v3. It previously held live
`StandardOutput`/`StandardError` streams and was disposable. For
streaming output, use `IExternalProcess`, which exposes the live
`Process` with `StandardOutput`/`StandardError` streams, stdin, and
lifecycle control.

The `ExecutePipedAsync`, `RunPipedAsync`, and
`CapturePipedResultAsync` methods no longer exist.

## Removed APIs

| Removed | Replacement |
|---------|-------------|
| `ExternalProcess(resolver, "path")` | `ExternalProcess(resolver, configuration, exitConfig)` |
| `ExternalProcess(config, exitConfig)` | `ExternalProcess(resolver, configuration, exitConfig)` |
| `process.ExitConfiguration = ...` (setter) | Pass at construction |
| Subclassing `ExternalProcess` | Sealed — use composition |
| `PipedProcessResult` | `IExternalProcess` for streaming |
| `ExecutePipedAsync` / `RunPipedAsync` | `IExternalProcess` lifecycle methods |

## Cross-references

- [Configuration guide](../../../site/docs/guides/configuration.md) — consumer reference
- [Resource Disposal guide](../../../site/docs/guides/resource-disposal.md) — ownership rules
- [Migration guide — ExternalProcess](../../../site/docs/migration-guides/3.0.0.md#5-externalprocess-keeps-only-constructor-c)
- [Migration guide — ExitConfiguration](../../../site/docs/migration-guides/3.0.0.md#3-exitconfiguration-is-read-only)
- [Migration guide — PipedProcessResult](../../../site/docs/migration-guides/3.0.0.md#7-processresult-equality-is-symmetric)
