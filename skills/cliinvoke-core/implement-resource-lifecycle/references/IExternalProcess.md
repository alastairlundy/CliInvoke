# IExternalProcess Lifecycle

`IExternalProcess` wraps a `System.Diagnostics.Process` and must be disposed to release OS handles (pipes, process handles, thread-pool wait handles).

## Construction

`IExternalProcess` is created through one of two seams:

### Factory (Recommended)

Use `IExternalProcessFactory.CreateExternalProcess(ProcessConfiguration)` — this is the intended entry point for both DI and non-DI scenarios:

```csharp
using CliInvoke;
using CliInvoke.Factories;

IExternalProcessFactory factory = new ExternalProcessFactory();
ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");

using IExternalProcess process = factory.CreateExternalProcess(config);
await process.StartAsync(CancellationToken.None);

BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);
```

### Direct Instantiation

Use `ExternalProcess(IFilePathResolver, ProcessConfiguration, ProcessExitConfiguration?)` when you need a custom `IFilePathResolver`. `ExternalProcess` is `sealed` — subclassing is not supported:

```csharp
using CliInvoke;
using CliInvoke.Processes;

ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");

using ExternalProcess process = new ExternalProcess(new FilePathResolver(), config);
await process.StartAsync(CancellationToken.None);

BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);
```

## No-Mutation Contract

`ProcessConfiguration` is passed to the factory or constructor and is not mutated afterward. All properties are init-only. The resolved file path (after `FilePathResolver` processing) surfaces on the result as `ProcessResult.ExecutedFilePath`, not by mutating the configuration.

## Recommended Disposal Pattern: `using`

`IExternalProcess` implements `IDisposable` (not `IAsyncDisposable`). Use a `using` declaration:

```csharp
using IExternalProcess process = factory.CreateExternalProcess(config);
await process.StartAsync(ct);

BufferedProcessResult result = await process.CaptureBufferedResultAsync(ct);
string output = result.StandardOutput;
// process.Dispose() called automatically when leaving scope
```

### Alternative: `try/finally`

When the disposable crosses a scope that is not a `using` declaration:

```csharp
IExternalProcess process = factory.CreateExternalProcess(config);
await process.StartAsync(ct);
try
{
    BufferedProcessResult result = await process.CaptureBufferedResultAsync(ct);
}
finally
{
    process.Dispose();
}
```

## What `Dispose()` Releases

- Anonymous pipe handles for stdin/stdout/stderr redirection.
- A process handle (OS-level child process reference).
- A thread-pool wait handle and output read thread used for stream pumping.

These are all kernel-level resources that the garbage collector cannot reclaim. Failing to dispose leaks handles and can cause `IOException("Too many open files")` on Linux or `Win32Exception("Not enough quota")` on Windows in long-running services.

## Ownership Rules

- The caller that receives the `IExternalProcess` owns it and must dispose it after `WaitForExitOrTimeoutAsync` / `CaptureBufferedResultAsync` completes.
- The invoker does not retain a reference to the process after returning.
- Middleware returns the process result **un-disposed** — you remain responsible for disposal.

## Caller-Owned Resources Inside ProcessConfiguration

`ProcessConfiguration` itself is **not** disposable. Two of its properties are your responsibility:

- **`StandardInput`** — a `StreamWriter` you supplied (or the default `StreamWriter.Null`). If you provide a real stream, dispose it after the final invocation.
- **`Credential`** — a `UserCredential` you supplied (or the default `UserCredential.Null`). If you provide a real `UserCredential`, dispose it.

Hold these in their own `using` declarations so their lifetime is independent of the configuration:

```csharp
using var stdin = new StreamWriter(new MemoryStream());
using var credential = new UserCredential("domain", "user", password, false);

ProcessConfiguration config = new ProcessConfiguration("cmd")
{
    ArgumentList = ["/c", "echo hello"],
    StandardInput = stdin,
    Credential = credential
};

BufferedProcessResult result = await CliRun.RunBufferedAsync(config);
// stdin and credential are disposed by their own `using` declarations.
// ProcessConfiguration is not disposable — nothing to dispose.
```
