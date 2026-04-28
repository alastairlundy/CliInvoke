# PipedProcessResult Disposal

`PipedProcessResult` owns the `StandardOutput` and `StandardError` streams and must be disposed to release these system resources.

## Usage via IProcessInvoker

When using `IProcessInvoker.ExecutePipedAsync`, the invoker manages the process lifecycle, but the caller is responsible for the resulting `PipedProcessResult`.

### Recommended Pattern: `await using`

```csharp
using CliInvoke.Core;

// Execute and get the piped result
await using PipedProcessResult pipedResult = await processInvoker.ExecutePipedAsync(config);

using StreamReader reader = new StreamReader(pipedResult.StandardOutput);
string output = await reader.ReadToEndAsync();

// pipedResult is disposed here
```

## Usage via IExternalProcess

When using `IExternalProcess.CapturePipedResultAsync`, the `IExternalProcess` instance manages the process, and the returned result manages the streams.

### Recommended Pattern: `await using`

```csharp
using CliInvoke.Core;
using CliInvoke.Core.Processes;

// Start the process
await using IExternalProcess process = invoker.StartAsync(config);
await process.StartAsync(CancellationToken.None);

// Capture the piped result
await using PipedProcessResult pipedResult = await process.CapturePipedResultAsync(CancellationToken.None);

using StreamReader reader = new StreamReader(pipedResult.StandardOutput);
string output = await reader.ReadToEndAsync();

// pipedResult and process are disposed here
```
