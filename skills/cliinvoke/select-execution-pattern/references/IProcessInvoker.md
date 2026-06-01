# IProcessInvoker Usage

This reference explains how to use the `IProcessInvoker` interface to execute processes, illustrating the recommended dependency injection-based approach.

## Interaction with DI

The `IProcessInvoker` service is designed to be obtained from a dependency injection container. This ensures proper abstraction and allows the library to manage the concrete implementation of the process invoker.

### Basic DI Usage
```csharp
using CliInvoke.Core;
using Microsoft.Extensions.DependencyInjection;

// Resolve IProcessInvoker from service provider
IProcessInvoker processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();

// Create process configuration
using ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");

// Execute the process and capture buffered output
BufferedProcessResult result = await processInvoker.ExecuteBufferedAsync(config);

// Handle the result
if (result.ExitCode != 0)
{
    Console.Error.WriteLine($"Process failed with exit code {result.ExitCode}");
    Console.Error.WriteLine(result.StandardError);
}
else
{
    Console.WriteLine(result.StandardOutput);
}
```

### Usage with Process Exit Configuration
For more control over process execution (such as setting timeouts), you can provide a `ProcessExitConfiguration`:

```csharp
using CliInvoke.Core;
using CliInvoke.Core.Processes;
using Microsoft.Extensions.DependencyInjection;

// Resolve IProcessInvoker from service provider
IProcessInvoker processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();

// Create process configuration
using ProcessConfiguration config = new ProcessConfiguration("dotnet", "build");

// Create exit configuration with timeout policy
var exitConfig = new ProcessExitConfiguration(
    ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromSeconds(30))
);

// Execute the process with exit configuration
BufferedProcessResult result = await processInvoker.ExecuteBufferedAsync(
    config,
    exitConfig
);

// Check for errors using the helper method
if (result.HasErrors())
{
    Console.Error.WriteLine("Process encountered errors:");
    Console.Error.WriteLine(result.StandardError);
}
else
{
    Console.WriteLine("Process completed successfully:");
    Console.WriteLine(result.StandardOutput);
}
```

### Available Execute Methods

The `IProcessInvoker` interface provides three methods for executing processes:

1. `ExecuteAsync` - Returns a `ProcessResult` with exit code only
2. `ExecuteBufferedAsync` - Returns a `BufferedProcessResult` with exit code, standard output, and standard error
3. `ExecutePipedAsync` - Returns a `PipedProcessResult` for scenarios requiring input/output piping

All methods follow the same parameter pattern:
- `ProcessConfiguration` - Required configuration for the process
- `ProcessExitConfiguration?` - Optional configuration for process exit behavior (timeout, etc.)
- `CancellationToken` - Optional cancellation token

### Note on DI Usage
Using dependency injection to obtain `IProcessInvoker` provides several benefits:
- Abstraction from the concrete `ProcessInvoker` implementation
- Easier testing through mocking of the `IProcessInvoker` interface
- Centralized management of service lifetimes
- Consistency with other CliInvoke services like `IExternalProcessFactory`