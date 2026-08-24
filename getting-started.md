---
title: "Getting Started"
---

# Getting Started

## Installing CliInvoke
The main way to install CliInvoke is using [nuget](https://www.nuget.org/packages/CliInvoke/) directly or through your IDE or Code Editor of choice.

### Versions

#### Stable Versions
Where possible you should always use a stable version of CliInvoke and update to the latest minor CliInvoke update within the Major.Minor.Build scheme.

#### Pre-release Versions
Versions starting with ``0.`` or ending with ``-alpha.``, ``-beta.``, or ``-rc.`` are pre-release versions and may not be as stable or bug-free as stable releases.

When configuring Nuget setup in your ``.csproj`` file, staying within a major version of CliInvoke is recommended.

## Setting up CliInvoke

### Dependency Injection
There are 2 main ways of setting up CliInvoke with dependency injection: manually, and using CliInvoke's ``AddCliInvoke`` configuration extension method from the ``CliInvoke.Extensions`` package.

#### Using ``AddCliInvoke``
For this approach you'll need the ``CliInvoke.Extensions`` nuget package.

If your project doesn't already use Dependency Injection, you can set it up as follows:

```csharp
using Microsoft.Extensions.DependencyInjection;

using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Extensions;

namespace MyApp;

class Program
{
    internal static ServiceProvider ServiceProvider;

    static void Main(string[] args)
    {
        // Create the service collection
        var services = new ServiceCollection();

        // Register your other dependencies here

        // AddCliInvoke registers IProcessInvoker, IExternalProcessFactory,
        // IProcessConfigurationBuilder, IRunnerConfigurationFactory and more.
        services.AddCliInvoke();

        // Build the service provider
        ServiceProvider = services.BuildServiceProvider();

        // Your other code goes here
    }
}
```

You can also configure the middleware pipeline when registering:

```csharp
services.AddCliInvoke(builder => builder.UseMiddleware<LoggingMiddleware>());
```

#### Manual Setup
This example manually registers ``IProcessInvoker`` and the other core CliInvoke services as Singletons.

Most developers using CliInvoke in their applications should use the Extensions package's ``AddCliInvoke`` method instead of manually configuring Dependency Injection unless there is a good reason to avoid it.

```csharp
using Microsoft.Extensions.DependencyInjection;

using CliInvoke;
using CliInvoke.Builders;
using CliInvoke.Core;
using CliInvoke.Core.Builders;
using CliInvoke.Core.Extensibility;
using CliInvoke.Core.Factories;
using CliInvoke.Extensibility;
using CliInvoke.Factories;

namespace MyApp;

class Program
{
    internal static ServiceProvider ServiceProvider;

    static void Main(string[] args)
    {
        // Create the service collection
        var services = new ServiceCollection();

        // Register your other dependencies here

        services.AddSingleton<IFilePathResolver, FilePathResolver>();
        services.AddSingleton<IProcessConfigurationBuilder, ProcessConfigurationBuilder>();
        services.AddSingleton<IExternalProcessFactory, ExternalProcessFactory>();
        services.AddSingleton<IProcessInvoker, ProcessInvoker>();

        // Optional - register if you intend to run a Process Configuration through another Process.
        services.AddSingleton<IRunnerConfigurationFactory, RunnerConfigurationFactory>();

        // Build the service provider
        ServiceProvider = services.BuildServiceProvider();

        // Your other code goes here
    }
}
```

#### Custom Middleware
You can write your own middleware that runs as part of the ``IProcessInvoker`` pipeline. Middleware implements ``IProcessMiddleware`` (in ``CliInvoke.Core.Middleware``) and receives an ``InvocationContext`` plus the ``next`` delegate:

```csharp
using CliInvoke.Core.Middleware;

public class LoggingMiddleware : IProcessMiddleware
{
    public Task InvokeAsync(InvocationContext context, Func<InvocationContext, Task> next)
    {
        // inspect or modify context.Configuration here
        return next(context);
    }
}
```

Register it with ``AddCliInvoke`` as shown above.

## Example Usage
Here are some simple examples of using CliInvoke. For more detailed examples, see the wiki page.

### Basic run
```csharp
using CliInvoke;
using CliInvoke.Core;

using ProcessConfiguration configuration = new ProcessConfiguration("dotnet", "--version");
ProcessResult result = await CliRun.RunAsync(configuration, ProcessExitConfiguration.CreateGraceful());
Console.WriteLine($"Exit code: {result.ExitCode}");
```

### Capture output
```csharp
using CliInvoke;
using CliInvoke.Core;

using ProcessConfiguration configuration = new ProcessConfiguration("dotnet", "--version");
BufferedProcessResult result = await CliRun.RunBufferedAsync(configuration, ProcessExitConfiguration.CreateGraceful());
Console.WriteLine(result.StandardOutput);
```

### Using Dependency Injection
```csharp
using Microsoft.Extensions.DependencyInjection;

using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Extensions;

ServiceCollection services = new();
services.AddCliInvoke();
ServiceProvider provider = services.BuildServiceProvider();
IProcessInvoker invoker = provider.GetRequiredService<IProcessInvoker>();

using ProcessConfiguration config = ProcessConfigurationFactory.Create("dotnet", "--version");
BufferedProcessResult result = await invoker.ExecuteBufferedAsync(config, ProcessExitConfiguration.CreateGraceful());
Console.WriteLine(result.StandardOutput);
```

### Bypassing middleware with IExternalProcess
```csharp
using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Core.Factories;
using CliInvoke.Extensions;

IExternalProcessFactory factory = provider.GetRequiredService<IExternalProcessFactory>();
using ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");
using IExternalProcess process = factory.CreateExternalProcess(config);
await process.StartAsync(CancellationToken.None);
ProcessResult result = await process.WaitForExitOrTimeoutAsync(CancellationToken.None);
```

### Fire-and-forget
```csharp
using CliInvoke;

int processId = CliRun.FireAndForget("dotnet", "build");
```

### Specializations (PowerShell / Cmd)
```csharp
using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Specializations.Configurations;

using PowershellProcessConfiguration config = new PowershellProcessConfiguration("-Command Get-Process");
BufferedProcessResult result = await CliRun.RunBufferedAsync(config, ProcessExitConfiguration.CreateGraceful());
```

You can also route invocations through PowerShell or Cmd using the ``UsePowerShell()`` / ``UseCmd()`` middleware extensions.
