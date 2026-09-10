# IExternalProcess Creation

This reference explains how to create an `IExternalProcess` instance, illustrating the difference between using the recommended factory approach and direct instantiation of the `ExternalProcess` class.

## Using the Factory (Recommended Approach)

Even when creating a process without a DI container, the `IExternalProcessFactory` is the intended entry point. This ensures proper abstraction and allows the library to manage the concrete implementation of the process.

### Non-DI Creation

```csharp
using CliInvoke;
using CliInvoke.Factories;

// Manually instantiate the factory
IExternalProcessFactory factory = new ExternalProcessFactory();

ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");
// The factory handles the instantiation of the concrete ExternalProcess implementation
using IExternalProcess process = factory.CreateExternalProcess(config);
await process.StartAsync(CancellationToken.None);

BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);
```

### Interaction with DI

If you have a DI container but need to create a process outside of a managed service (e.g., in a main method), you can resolve the factory from the service provider.

```csharp
using Microsoft.Extensions.DependencyInjection;
using CliInvoke;
using CliInvoke.Factories;

// Resolve factory manually from service provider
IExternalProcessFactory factory = serviceProvider.GetRequiredService<IExternalProcessFactory>();

ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");
using IExternalProcess process = factory.CreateExternalProcess(config);
await process.StartAsync(CancellationToken.None);

BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);
```

## Direct Instantiation

In specific scenarios where you need full control over the `ExternalProcess` constructor (such as providing a custom `IFilePathResolver`), you can instantiate the class directly. `ExternalProcess` is `sealed` — subclassing is not supported.

### Basic Direct Instantiation

This approach uses the shared file path resolver internally.

```csharp
using CliInvoke;
using CliInvoke.Processes;

ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");
using ExternalProcess process = new ExternalProcess(new FilePathResolver(), config);
await process.StartAsync(CancellationToken.None);

BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);
```

### Direct Instantiation with Custom Resolver

Use this approach when you need to specify how the executable path is resolved.

```csharp
using CliInvoke;
using CliInvoke.Processes;

IFilePathResolver myResolver = new MyCustomFilePathResolver();
ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");

using ExternalProcess process = new ExternalProcess(myResolver, config);
await process.StartAsync(CancellationToken.None);

BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);
```

### Note on Direct Instantiation

Directly instantiating `ExternalProcess` is generally discouraged in favor of `IExternalProcessFactory`. Using the factory provides a consistent layer of abstraction, making testing easier (via mocking) and keeping creation logic decoupled from the implementation.

## Middleware Coverage

The `IExternalProcess` / `ExternalProcess` path is the **bypass pattern**: middleware does **not** apply. There is no middleware API at the `IExternalProcess` layer — the factory and direct-construction paths are unaffected by middleware. If you need cross-cutting concerns (logging, post-exit validation, platform wrapping), configure them on a `ProcessInvoker` via the `Use*` extension methods and let the invoker drive the factory through `IProcessInvoker`.

Middleware operates above the invoker: `ProcessInvoker` / `IProcessInvoker` → middleware chain → `IExternalProcessFactory` → `IExternalProcess`. The `IExternalProcess` layer is below the middleware chain and never sees it.
