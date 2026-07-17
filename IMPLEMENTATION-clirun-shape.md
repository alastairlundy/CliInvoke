# Implementation Blueprint — CliRun Shape

> **Scope Binding.** This blueprint is a context pointer for the F4 deepening (CliRun shape) and is valid **only** for the spec at `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md`. It must not be applied to other specifications without explicit authorization.
>
> **Linked Spec:** `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md`
> **Decision Ledger:** `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md`
>
> Every technical statement in this blueprint that satisfies a functional requirement cites a `Dxxx` or `Txxx` record using `DECISIONS-CliInvoke-clirun-shape.md#<Dxxx|Txxx>` format. The `## Ledger Reference` section at the end lists every record cited.

## Scope

Refactor `src/CliInvoke/Extensions/CliRun.cs` so:

1. The dead `_externalProcessFactory` field is read by every `Run*Async` call [`DECISIONS-CliInvoke-clirun-shape.md#D001`].
2. The 6 near-identical `Run*Async` bodies funnel into one private internal method [`DECISIONS-CliInvoke-clirun-shape.md#D002`].
3. The 3 string-arg overloads share a `BuildStringArgsConfig` helper [`DECISIONS-CliInvoke-clirun-shape.md#D003`, `DECISIONS-CliInvoke-clirun-shape.md#D006`].
4. The default factory is constructed per-call with the static resolver [`DECISIONS-CliInvoke-clirun-shape.md#T005`].
5. The helper is pure (no pre-resolution); resolution happens at the factory level [`DECISIONS-CliInvoke-clirun-shape.md#T006`].
6. Tests under `tests/CliInvoke.Tests/CliRunTests.cs` cover the public surface, are parallel-safe, and use fakes that implement `IDisposable` where needed [`DECISIONS-CliInvoke-clirun-shape.md#T007`].
7. The F1 follow-up is shape-TBD; the F1 pipeline's signature is F1's decision, not F4's [`DECISIONS-CliInvoke-clirun-shape.md#D007`].

The public API surface is **binary-compatible**: signatures are unchanged, no types are added or removed. Behaviour is changed per `T005` (default factory construction is per-call, not lazy) and `T006` (helper is pure, resolution happens at the factory level) [`DECISIONS-CliInvoke-clirun-shape.md#D008`].

## Technical Implementation

### 1. The 3 string-arg overloads share a pure helper

`BuildStringArgsConfig` is the helper, with the `out ProcessExitConfiguration` parameter pattern (reverted from the tuple plan per the reviewer's issues 4 and 5) [`DECISIONS-CliInvoke-clirun-shape.md#D006`]. The `out` parameter is a standard C# idiom for "return a primary value plus an auxiliary value"; the `using var` declaration applies directly to the disposable `ProcessConfiguration`.

```csharp
private static ProcessConfiguration BuildStringArgsConfig(
    string targetFilePath,
    string arguments,
    string? workingDirectory,
    bool redirectStandardOutput,
    TimeSpan? timeoutTimeSpan,
    out ProcessExitConfiguration exitConfiguration)
{
    workingDirectory ??= Environment.CurrentDirectory;

    ProcessConfiguration configuration = ProcessConfigurationFactory.Create(
        targetFilePath, arguments, workingDirectory, redirectStandardOutput);

    timeoutTimeSpan ??= ProcessTimeoutPolicy.Default.TimeoutThreshold;

    exitConfiguration = ProcessExitConfiguration.CreateGraceful(
        ProcessTimeoutPolicy.FromTimeSpan((TimeSpan)timeoutTimeSpan));

    return configuration;
}
```

The helper is **pure** [`DECISIONS-CliInvoke-clirun-shape.md#T006`]: it does not call `GetFilePathResolver()` and does not pre-resolve. Resolution happens at the factory level (`IExternalProcess.StartAsync`) per F2 D001/D013. The asymmetric precedence between string-arg and config-arg overloads that would result from pre-resolution is avoided because the factory is the single source of resolution.

Each of the 3 string-arg overloads calls the helper with the `using var` pattern and forwards to the matching config-arg overload:

```csharp
public static async Task<ProcessResult> RunAsync(string targetFilePath,
    string arguments = "", string? workingDirectory = null, TimeSpan? timeoutTimeSpan = null,
    CancellationToken cancellationToken = default)
{
    using var configuration = BuildStringArgsConfig(targetFilePath, arguments, workingDirectory,
        redirectStandardOutput: false, timeoutTimeSpan, out var exitConfiguration);
    return await RunAsync(configuration, exitConfiguration, cancellationToken);
}
```

The same pattern applies to `RunBufferedAsync` (with `redirectStandardOutput: true`) and `RunPipedAsync` (with `redirectStandardOutput: true`). The redirect-output flag is the only difference between the three call sites [`DECISIONS-CliInvoke-clirun-shape.md#D003`].

### 2. The default factory uses per-call construction with the static resolver

The `_externalProcessFactory` field initializer is `() => new ExternalProcessFactory(GetFilePathResolver())` — per-call construction that re-reads the static resolver on every call [`DECISIONS-CliInvoke-clirun-shape.md#T005`]. This honors `UseFilePathResolver(customResolver)` for default users; the parameterless `ExternalProcessFactory` ctor at `src/CliInvoke/Factories/ExternalProcessFactory.cs:30-33` would otherwise allocate its own `FilePathResolver` and silently drop the custom one (the reviewer's issue 1).

```csharp
private static Func<IExternalProcessFactory> _externalProcessFactory =
    () => new ExternalProcessFactory(GetFilePathResolver());
```

A code-review note at the field shall read: "T005: per-call allocation is intentional to honor `UseFilePathResolver`; do not cache without invalidation." The custom-factory path is unaffected — `UseExternalProcessFactory(customFactory)` continues to override the delegate [`DECISIONS-CliInvoke-clirun-shape.md#D001`].

`GetFilePathResolver()` already caches the resolver via double-check locking at `src/CliInvoke/Extensions/CliRun.cs:59-70`, so the resolver itself is not re-allocated. The per-call allocation is the `ExternalProcessFactory` and its captured resolver reference.

### 3. The 3 config-arg overloads funnel into one internal method

`RunInternalAsync<T>` is the funnel, with the generic-with-delegate form [`DECISIONS-CliInvoke-clirun-shape.md#D002`]:

```csharp
// F1 follow-up: when the Process Invocation Pipeline ships, route through _pipeline.ExecuteAsync.
private static async Task<T> RunInternalAsync<T>(
    ProcessConfiguration configuration,
    ProcessExitConfiguration? exitConfiguration,
    Func<IExternalProcess, CancellationToken, Task<T>> capture,
    CancellationToken cancellationToken)
{
    using IExternalProcess externalProcess = GetExternalProcessFactory()
        .CreateExternalProcess(configuration, exitConfiguration ?? ProcessExitConfiguration.CreateGraceful());

    await externalProcess.StartAsync(cancellationToken);

    return await capture(externalProcess, cancellationToken);
}
```

Each of the 3 config-arg overloads is a one-line forward:

```csharp
public static async Task<ProcessResult> RunAsync(ProcessConfiguration configuration,
    ProcessExitConfiguration? exitConfiguration = null,
    CancellationToken cancellationToken = default)
    => await RunInternalAsync(configuration, exitConfiguration,
        (p, t) => p.WaitForExitOrTimeoutAsync(t), cancellationToken);

public static async Task<BufferedProcessResult> RunBufferedAsync(ProcessConfiguration configuration,
    ProcessExitConfiguration? exitConfiguration = null,
    CancellationToken cancellationToken = default)
    => await RunInternalAsync(configuration, exitConfiguration,
        (p, t) => p.CaptureBufferedResultAsync(t), cancellationToken);

public static async Task<PipedProcessResult> RunPipedAsync(ProcessConfiguration configuration,
    ProcessExitConfiguration? exitConfiguration = null,
    CancellationToken cancellationToken = default)
    => await RunInternalAsync(configuration, exitConfiguration,
        (p, t) => p.CapturePipedResultAsync(t), cancellationToken);
```

The 3 lambdas are stateless (capture no closure variables) and are cached by the C# compiler as static delegates; no per-call closure allocation.

### 4. The F1 follow-up comment is module-only, not ledger-pointing

The comment at the top of `RunInternalAsync<T>` points to the F1 pipeline type name and method name, not to the F1 decision ledger [`DECISIONS-CliInvoke-clirun-shape.md#T004`]. A ledger is a session artifact, not a long-term document; pointing to it from code creates coupling that doesn't survive across sessions.

```csharp
// F1 follow-up: when the Process Invocation Pipeline ships, route through _pipeline.ExecuteAsync.
```

The exact signature of `_pipeline.ExecuteAsync` is F1's decision [`DECISIONS-CliInvoke-clirun-shape.md#D007`], not F4's. F4 does not lock the F1 D002 decision about whether `ProcessInvocationContext` is mutable or immutable. The "5-line edit" claim in the original D005 is withdrawn — the actual edit size depends on F1's signature.

### 5. Tests are public-surface, parallel-safe, fakes implement IDisposable where needed

Tests under `tests/CliInvoke.Tests/CliRunTests.cs` exercise the public `CliRun` surface only [`DECISIONS-CliInvoke-clirun-shape.md#T007`]. The test class is annotated with `[NotInParallel]` (TUnit) to serialize tests that mutate `CliRun`'s static state (`_externalProcessFactory`, `_filePathResolver`) — the reviewer's issue 6. Custom fakes implement `IDisposable` if they hold captured state or unmanaged resources — the reviewer's issue 13. The test class's `[AfterEach]` hook disposes them and resets `CliRun`'s statics.

```csharp
[NotInParallel]
public class CliRunTests
{
    private sealed class CountingExternalProcessFactory : IExternalProcessFactory, IDisposable
    {
        public int CreateExternalProcessCallCount { get; private set; }
        public IExternalProcess CreateExternalProcess(ProcessConfiguration configuration)
            => CreateExternalProcess(configuration, ProcessExitConfiguration.CreateGraceful());

        public IExternalProcess CreateExternalProcess(
            ProcessConfiguration configuration, ProcessExitConfiguration exitConfiguration)
        {
            CreateExternalProcessCallCount++;
            return new ExternalProcess(new FilePathResolver(), configuration, exitConfiguration);
        }

        public void Dispose() { /* no-op */ }
    }

    [AfterEach]
    public void Reset()
    {
        CliRun.UseExternalProcessFactory(new ExternalProcessFactory());
        CliRun.UseFilePathResolver(new FilePathResolver());
    }

    [Test]
    public async Task RunAsync_InvokesCustomFactory_Once()
    {
        var customFactory = new CountingExternalProcessFactory();
        CliRun.UseExternalProcessFactory(customFactory);

        using var configuration = ProcessConfigurationFactory.Create(
            ProcessTestHelper.GetTargetFilePath());
        await CliRun.RunAsync(configuration, cancellationToken: CancellationToken.None);

        await Assert.That(customFactory.CreateExternalProcessCallCount).IsEqualTo(1);
    }
}
```

The lazy-default-factory claim from the original T003 is dropped — `T005` reverted lazy caching, so the default factory is per-call. The throw-timing claim from the original T003 is dropped — `T006` reverted pre-resolution, so the throw from an unresolvable path is the resolver's throw type, propagated through the factory's `StartAsync`, not surfaced at the public call site.

## Implementation Order

The recommended order minimizes cross-coupling between the changes:

1. **Apply `D006`** (revert tuple to `out` parameter). Local to `src/CliInvoke/Extensions/CliRun.cs`; no other file touched.
2. **Apply `T006`** (helper is pure). The helper's body is simplified — no resolver reading. Local to the helper.
3. **Apply `T005`** (per-call factory construction). The `_externalProcessFactory` field initializer is changed. Local to the field.
4. **Apply `D002`** (funnel method). The 3 config-arg overloads become one-line forwards to `RunInternalAsync<T>`. Local to `src/CliInvoke/Extensions/CliRun.cs`.
5. **Apply `T004`** (F1 follow-up comment). A single `//` line at the top of `RunInternalAsync<T>`. Local.
6. **Apply `D008`** (D001 wording). Documentation only.
7. **Apply `T007`** (tests). A new file under `tests/CliInvoke.Tests/`.

The F2 amendment that T001 originally called for is **no longer required** because `T006` reverted pre-resolution; the slot remains the resolution mechanism per F2 D001/D013.

## Out of Scope

- The F1 pipeline module itself (`ProcessInvocationPipeline`, `ProcessInvocationContext`) — built separately per `DECISIONS-CliInvoke-process-invocation-pipeline.md`.
- The F2 long-term solution for eliminating the `BuilderProcessConfiguration` wrapper — DEFERRED per F2 D006.
- The `DECISIONS-CliInvoke-file-path-resolver-seam.md` ledger — **not yet present in the working tree**; cited forward-looking for T014 (double-check locking) and D008 (setter asymmetry). Will be created in a future session.

## Ledger Reference

This blueprint cites the following records from `docs/decisions/DECISIONS-CliInvoke-clirun-shape.md`:

### Functional decisions (Dxxx)

- **D001** — disposition of the dead `_externalProcessFactory` field (wording superseded by D008)
- **D002** — shape of the internal funnel method
- **D003** — the 3 string-arg overloads' 5-line config-building step
- **D004** — shape of the `BuildStringArgsConfig` return (superseded by D006)
- **D005** — F4 vs F1 (the Process Invocation Pipeline) relationship (signature superseded by D007)
- **D006** — `BuildStringArgsConfig` return shape (D004-revised)
- **D007** — F4 vs F1 follow-up signature (D005-revised)
- **D008** — D001's "public surface unchanged" wording (D001-revised)

### Technical decisions (Txxx)

- **T001** — `BuildStringArgsConfig` resolver reading (superseded by T006)
- **T002** — `ExternalProcessFactory` construction (superseded by T005)
- **T003** — test strategy (superseded by T007)
- **T004** — F1 follow-up comment format
- **T005** — `ExternalProcessFactory` construction (T002-revised)
- **T006** — `BuildStringArgsConfig` resolver reading (T001-revised)
- **T007** — test strategy (T003-revised)

## Cross-ledger dependencies

- **`DECISIONS-CliInvoke-process-configuration-shape.md` D001 and D013** (the resolution-slot contract) — preserved; T001's original F2 amendment is no longer required because T006 reverted pre-resolution.
- **`DECISIONS-CliInvoke-process-invocation-pipeline.md` D002** (the pipeline's input shape) — F1's signature is F1's decision, not F4's; the F1 follow-up is shape-TBD.
- **`DECISIONS-CliInvoke-file-path-resolver-seam.md`** — **not yet present in the working tree**; cited forward-looking for T014 (double-check locking) and D008 (setter asymmetry). Will be created in a future session.
