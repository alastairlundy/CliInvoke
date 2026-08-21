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

### Adding Middleware (Cross-cutting Concerns)
`ProcessInvoker` can be configured with an optional **middleware** chain that wraps the terminal Process Invocation Pipeline (Configuration → Invoke → OS Process → Result) in registration order. The public API is the fluent `Use*` extension methods — each returns a *new* `ProcessInvoker`, so they compose. Call sites (`ExecuteAsync` / `ExecuteBufferedAsync` / `ExecutePipedAsync`) are **unchanged** with or without middleware.

```csharp
using CliInvoke;                                  // ProcessInvoker
using CliInvoke.Core;                             // IExternalProcessFactory
using CliInvoke.Core.Middleware;                  // MiddlewareItems
using CliInvoke.Extensions.Middleware;            // UseLogging
using CliInvoke.Extensions.Middleware.Validation; // UsePostExitValidation, PostExitValidation
using CliInvoke.Specializations.Middleware;       // UsePowerShell, UseCmd

IExternalProcessFactory factory = serviceProvider.GetRequiredService<IExternalProcessFactory>();

// Logging on every invocation (resolves an ILogger from the MiddlewareItems bag, if seeded):
ProcessInvoker loggingInvoker = new ProcessInvoker(factory).UseLogging();

// Validate the result after exit (throws ProcessValidationException on failure):
ProcessInvoker validatedInvoker = new ProcessInvoker(factory)
    .UsePostExitValidation(PostExitValidation.ExitCodeIsZero());

// Run the command inside PowerShell Core / Windows cmd.exe:
ProcessInvoker psInvoker = new ProcessInvoker(factory).UsePowerShell();
ProcessInvoker cmdInvoker = new ProcessInvoker(factory).UseCmd();   // Windows-only

// Compose freely — each Use* returns a new ProcessInvoker:
ProcessInvoker invoker = new ProcessInvoker(factory)
    .UseLogging()
    .UsePostExitValidation(PostExitValidation.ExitCodeIsZero());

// Seed shared services (e.g. an ILogger) via the MiddlewareItems bag:
var items = new MiddlewareItems();
items.Set("Logger", myLogger);
ProcessInvoker seededInvoker = new ProcessInvoker(factory, items).UseLogging();

// Call sites are identical to the non-middleware path:
using ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");
BufferedProcessResult result = await invoker.ExecuteBufferedAsync(config);
```

**Disposal through the chain:** middleware returns the process result **un-disposed** (exactly as without middleware). You remain responsible for disposing `PipedProcessResult` (and its streams) and the `ProcessConfiguration` you created. See the `implement-resource-lifecycle` skill for the full ownership checklist.