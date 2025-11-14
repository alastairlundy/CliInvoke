# Getting Started

## Installing CliInvoke
The main way to install CliInvoke is using [nuget](https://www.nuget.org/packages/CliInvoke/) directly or through your IDE or Code Editor of choice.

### Versions

### Stable Versions
Where possible you should always use a stable version of CliInvoke and update to the latest minor CliInvoke update within the Major.Minor.Build scheme.

### Pre-release Versions
Versions starting with ``0.`` or ending with ``-alpha.``. ``-beta.``, or ``-rc.`` are pre-release versions and may not be as stable or bug-free as stable releases. 

When configuring Nuget setup in your ``.csproj`` file, staying within a major version of CliInvoke is recommended.

## Setting up CliInvoke

### Dependency Injection 
There's 2 main ways of setting up CliInvoke with dependency injection: manually, and using CliInvoke's ``AddCliInvoke`` configuration extension methods with the ``CliInvoke.Extensions`` nuget package.

#### Using ``AddCliInvoke``
For this approach you'll need the ``CliInvoke.Extensions`` nuget package.

If your project doesn't already use Dependency Injection, you can set it up as follows:

```csharp
using Microsoft.Extensions.DependencyInjection;

using CliInvoke.Extensions;

namespace MyApp;

    class Program
    {
      internal ServiceProvider serviceProvider;

        static void Main(string[] args)
        {
            // Create the service collection
            var services = new ServiceCollection();

            // Register Your other dependencies here
            
            // AddCliInvoke goes here
            services.AddCliInvoke();

            // Optional extra in case you want to run a Process Configuration through another Process Configuration
            services.AddDefaultRunnerProcessInvoker();

            // Build the service provider
            serviceProvider = services.BuildServiceProvider();

            //Your other code goes here
        }
}
```

#### Manual Setup
This example manually sets up ``IProcessPipeHandler``, ``IProcessInvoker`` and other dependencies as Singletons.

Most developer users using CliInvoke in their applications should use the Extensions package's ``AddCliInvoke`` and optional ``AddDefaultRunnerProcessInvoker`` methods instead of manually configuring Dependency Injection unless there is good reason to avoid using it.

Configuring ``DefaultRunnerProcessInvoker`` to be injected for the abstract class ``RunnerProcessInvokerBase`` is relatively trivial but can 
lead to errors if configured incorrectly. 

Configuring a custom class that implements ``RunnerProcessInvokerBase`` to be used instead of the ``DefaultRunnerProcessInvoker`` is not trivial and manual setup of it should be avoided if possible. The Extensions package contains a ``AddDerivedRunnerProcessInvoker`` extension method that can handle this.


```csharp
using Microsoft.Extensions.DependencyInjection;

using CliInvoke;
using CliInvoke.Piping;
using CliInvoke.Core;
using CliInvoke.Core.Piping;
using CliInvoke.Core.Extensibility.Factories;
using CliInvoke.Core.Extensibility;
using CliInvoke.Extensibility.Factories;
using CliInvoke.Extensibility;


namespace MyApp;

    class Program
    {
      internal ServiceProvider serviceProvider;

        static void Main(string[] args)
        {
            // Create the service collection
            var services = new ServiceCollection();

            // Register Your other dependencies here
            
            services.AddSingleton<IFilePathResolver, FilePathResolver>();
            services.AddSingleton<IProcessPipeHandler, ProcessPipeHandler>();
            services.AddSingleton<IProcessInvoker, ProcessInvoker>();

            // Optional - Add if you intend to run a Process Configuration through another Process.
            services.AddSingleton<IProcessRunnerFactory, ProcessRunnerFactory>();
            // Both the directly above and below code are needed for
            // running Process Configurations through other Process Configurations.

            // RunnerProcessConfiguration code ommitted for clarity -

            services.AddScoped<RunnerProcessInvokerBase>(sp => new DefaultRunnerProcessInvoker(
                    sp.GetRequiredService<IProcessInvoker>(),
                    sp.GetRequiredService<IRunnerProcessFactory>(),
                    runnerProcessConfiguration));


            // Build the service provider
            serviceProvider = services.BuildServiceProvider();

            //Your other code goes here
        }
}
```

## Example Usage
Here's an example of a simple usage of creating a CliInvoke command. For more detailed examples, see the wiki page.

```csharp
using CliInvoke;
using CliInvoke.Abstractions;
using CliInvoke.Builders;
using CliInvoke.Builders.Abstractions;

ICliCommandInvoker commandRunner = serviceProvider.GetRequiredService<ICliCommandInvoker>();

ICliCommandConfigurationBuilder builder = new CliCommandConfigurationBuilder("Path/To/Exe")
              .WithArguments(["arg1", "arg2"])
              .WithWorkingDirectory("/Path/To/Directory");

CliCommandConfiguration command = builder.Build();

var result = await commandRunner.ExecuteBufferedAsync(command);
```