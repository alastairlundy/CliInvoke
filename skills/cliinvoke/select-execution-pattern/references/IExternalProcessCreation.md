# IExternalProcess Creation

This reference explains how to create an `IExternalProcess` instance, illustrating the difference between using the recommended factory approach and direct instantiation of the `ExternalProcess` class.

## Using the Factory (Recommended Approach)

Even when creating a process without a DI container, the `IExternalProcessFactory` is the intended entry point. This ensures proper abstraction and allows the library to manage the concrete implementation of the process.

### Non-DI Creation
```csharp
using CliInvoke;
using CliInvoke.Core.Processes;
using CliInvoke.Processes;

// Manually instantiate the factory
IExternalProcessFactory factory = new ExternalProcessFactory();

using ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");
// The factory handles the instantiation of the concrete ExternalProcess implementation
await using IExternalProcess process = factory.CreateExternalProcess(config);
await process.StartAsync();

BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);
```

### Interaction with DI
If you have a DI container but need to create a process outside of a managed service (e.g., in a main method), you can resolve the factory from the service provider.

```csharp
using Microsoft.Extensions.DependencyInjection;
using CliInvoke;
using CliInvoke.Core.Processes;

// Resolve factory manually from service provider
IExternalProcessFactory factory = serviceProvider.GetRequiredService<<IIExternalProcessFactory>();

using ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");
await using IExternalProcess process = factory.CreateExternalProcess(config);
await process.StartAsync();

BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);
```

## Direct Instantiation

In specific scenarios where you need full control over the `ExternalProcess` constructor (such as providing a custom `IFilePathResolver`), you can instantiate the class directly.

### Basic Direct Instantiation
This approach uses the shared file path resolver internally.

```csharp
using CliInvoke;
using CliInvoke.Core.Processes;
using CliInvoke.Processes;

using ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");
await using ExternalProcess process = new ExternalProcess(config);
await process.StartAsync();

BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);
```

### Direct Instantiation with Custom Resolver
Use this approach when you need to specify how the executable path is resolved.

```csharp
using CliInvoke;
using CliInvoke.Core.Processes;
using CliInvoke.Core.Helpers;
using CliInvoke.Processes;

IFilePathResolver myResolver = new MyCustomFilePathResolver();
using ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");

await using ExternalProcess process = new ExternalProcess(myResolver, config);
await process.StartAsync();

BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);
```

### Note on Direct Instantiation
Directly instantiating `ExternalProcess` is generally discouraged in favor of `IExternalProcessFactory`. Using the factory provides a consistent layer of abstraction, allowing for easier testing (via mocking) and ensuring that the creation logic remains decoupled from the implementation.
