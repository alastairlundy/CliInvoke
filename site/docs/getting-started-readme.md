---
title: Getting Started
layout: simple
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
There are 2 main ways of setting up CliInvoke with dependency injection: manually, and using CliInvoke's ``AddCliInvoke`` configuration extension methods (namespace ``CliInvoke.Extensions``), which ship in the ``CliInvoke`` package.

#### Using ``AddCliInvoke``
For this approach you'll need the ``CliInvoke`` nuget package.

If your project doesn't already use Dependency Injection, you can set it up as follows:

```csharp
using Microsoft.Extensions.DependencyInjection;

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

        // AddCliInvoke registers all CliInvoke services (IProcessInvoker,
        // IExternalProcessFactory, IProcessConfigurationBuilder, etc.) for you.
        services.AddCliInvoke();

        // Build the service provider
        ServiceProvider = services.BuildServiceProvider();

        // Your other code goes here
    }
}
```

#### Manual Setup
This example manually sets up ``IProcessInvoker``, ``IExternalProcessFactory`` and other dependencies as Singletons.

Most developers using CliInvoke in their applications should use the ``AddCliInvoke`` method instead of manually configuring Dependency Injection unless there is good reason to avoid using it. ``AddCliInvoke`` registers all of the services shown below for you.

```csharp
using Microsoft.Extensions.DependencyInjection;

using CliInvoke;
using CliInvoke.Builders;
using CliInvoke.Core;
using CliInvoke.Core.Builders;
using CliInvoke.Core.Extensibility;
using CliInvoke.Extensibility;

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
        services.AddSingleton<IShellDetector, ShellDetector>();

        // Build the service provider
        ServiceProvider = services.BuildServiceProvider();

        // Your other code goes here
    }
}
```

## Example Usage
Here's an example of a simple usage of creating a CliInvoke command. For more detailed examples, see the wiki page.

```csharp
using CliInvoke;
using CliInvoke.Core;

IProcessInvoker commandRunner = serviceProvider.GetRequiredService<IProcessInvoker>();

ProcessConfiguration command = new("dotnet", "--version")
{
    WorkingDirectoryPath = @"C:\Path\To\Directory"
};

BufferedProcessResult result = await commandRunner.ExecuteBufferedAsync(command, ProcessExitConfiguration.CreateGraceful());
```
