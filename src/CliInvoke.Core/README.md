# CliInvoke.Core
This package contains Process Running and handling abstractions as well as common types used by implementing classes.

For an implementing package, check out [CliInvoke](https://www.nuget.org/packages/AlastairLundy.CliInvoke/).

<!-- Badges -->
[![Latest NuGet](https://img.shields.io/nuget/v/AlastairLundy.CliInvoke.Core.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Core/)
[![Latest Pre-release NuGet](https://img.shields.io/nuget/vpre/AlastairLundy.CliInvoke.Core.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Core/)
[![Downloads](https://img.shields.io/nuget/dt/AlastairLundy.CliInvoke.Core.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Core/)
![License](https://img.shields.io/github/license/alastairlundy/CliInvoke)

Key Abstractions:
* ``IProcessInvoker``
* ``IProcessConfigurationFactory``

* Piping:
  * ``IProcessPipeHandler``

* Fluent Builders:
  * ``IArgumentsBuilder`` - An interface to help with Argument Building and argument escaping.
  * ``IEnvironmentVariablesBuilder`` - An interface to help with setting Environment variables.
  * ``IProcessConfigurationBuilder`` - An interface to fluently configure and build ``ProcessConfiguration`` objects.
  * ``IProcessResourcePolicyBuilder`` - An interface to fluently configure and build ``ProcessResourcePolicy`` objects.
  * ``IUserCredentialBuilder``

## Features
* Clear separation of concerns between Process Configuration Builders, Process Configuration Models, and Invokers.
* Supports .NET Standard 2.0, .NET 8, and newer TFMs, and has few dependencies.
* Has Dependency Injection extensions to make using it a breeze.
* Support for specific specializations such as running executables or commands via Windows PowerShell or CMD on Windows <sup>1</sup>
* [SourceLink](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink) support

<sup>1</sup> Specializations library distributed separately.

## Why CliInvoke?

| Feature                                                                | CliInvoke | CliWrap |                ProcessX                 |
|------------------------------------------------------------------------|:---------:|:-------:|:---------------------------------------:|
| Configure and Run External Processes using code written in .NET        |     ✅     |    ✅    | ❌, Uses mixture of .NET and BASH syntax |
| No Additional Licensing Terms Required for Use                         |     ✅     |    ❌    |                    ✅                    |
| Uses only managed .NET code                                            |     ✅     |    ❌    |                    ✅                    |
| Follows Separation of Concerns and the Single Responsibility Principle |     ✅     |    ❌    |                    ❌                    |
| Allows for alternative Process Runners to be specified and/or used     |     ✅     |    ❌    |                    ❌                    |

## Installing CliInvoke.Core
CliInvoke.Core packages can be installed via the .NET SDK CLI, Nuget via your IDE or code editor's package interface, or via the Nuget website.

| Package Name                 | Nuget Link                                                                                    | .NET SDK CLI command                                |
|------------------------------|-----------------------------------------------------------------------------------------------|-----------------------------------------------------|
| AlastairLundy.CliInvoke.Core | [AlastairLundy.CliInvoke.Core Nuget](https://nuget.org/packages/AlastairLundy.CliInvoke.Core) | ``dotnet add package AlastairLundy.CliInvoke.Core`` |

## Supported Platforms
CliInvoke supports Windows, macOS, Linux, FreeBSD, Android, and potentially some other operating systems.

For more details see the [list of supported platforms](https://github.com/alastairlundy/CliInvoke/blob/main/docs/docs/Supported-OperatingSystems.md)

## Examples

### Simple ``ProcessConfiguration`` creation with Factory Pattern
This approach uses the ``IProcessConfigurationFactory`` interface factory to create a ``ProcessConfiguration``. It requires fewer parameters and sets up more defaults for you. 

It can be provided with a ``Action<IProcessConfigurationBuilder> configure`` optional parameter where greater control is desired.

#### Non-Buffered Execution Example
This example gets a non buffered ``ProcessResult`` that contains basic process exit code, id, and other information.

```csharp
using AlastairLundy.CliInvoke.Core.Factories;
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliIinvoke;

using Microsoft.Extensions.DependencyInjection;

// Dependency Injection setup code ommitted for clarity

// Get IProcessConfigurationFactory 
IProcessConfigurationFactory processConfigFactory = serviceProvider.GetRequiredService<IProcessConfigurationFactory>();

// Get IProcessConfigurationInvoker
IProcessInvoker _invoker_ = serviceProvider.GetRequiredService<IProcessInvoker>();

// Simply create the process configuration.
ProcessConfiguration configuration = processConfigFactory.Create("path/to/exe", "arguments");

// Run the process configuration and get the results.
ProcessResult result = await _invoker.ExecuteAsync(configuration, CancellationToken.None);
```

#### Buffered Execution Example
This example gets a ``BufferedProcessResult`` which contains redirected StandardOutput and StandardError as strings.

```csharp
using AlastairLundy.CliInvoke.Core.Factories;
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliIinvoke;

using Microsoft.Extensions.DependencyInjection;

// Dependency Injection setup code ommitted for clarity

// Get IProcessConfigurationFactory 
IProcessConfigurationFactory processConfigFactory = serviceProvider.GetRequiredService<IProcessConfigurationFactory>();

// Get IProcessConfigurationInvoker
IProcessnvoker _invoker_ = serviceProvider.GetRequiredService<IProcessInvoker>();

// Simply create the process configuration.
ProcessConfiguration configuration = processConfigFactory.Create("path/to/exe", "arguments");

// Run the process configuration and get the results.
BufferedProcessResult result = await _invoker.ExecuteBufferedAsync(configuration, CancellationToken.None);
```


### Advanced Configuration with Builders

The following examples shows how to configure and build a ``ProcessConfiguration`` depending on whether Buffering the output is desired.

#### Non-Buffered Execution Example
This example gets a non buffered ``ProcessResult`` that contains basic process exit code, id, and other information.

```csharp
using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Core;

using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;

using Microsoft.Extensions.DependencyInjection;

  //Namespace and class code ommitted for clarity 

  // ServiceProvider and Dependency Injection setup code ommitted for clarity
  
  IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();

  // Fluently configure your Command.
  IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("Path/To/Executable")
                            .SetArguments(["arg1", "arg2"])
                            .SetWorkingDirectory("/Path/To/Directory");
  
  // Build it as a ProcessConfiguration object when you're ready to use it.
  ProcessConfiguration config = builder.Build();
  
  // Execute the process through ProcessInvoker and get the results.
ProcessResult result = await _processConfigInvoker.ExecuteAsync(config);
```

#### Buffered Execution Example
This example gets a ``BufferedProcessResult`` which contains redirected StandardOutput and StandardError as strings.

```csharp
using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Builders;

using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Builders;

using Microsoft.Extensions.DependencyInjection;


  //Namespace and class code ommitted for clarity 

  // ServiceProvider and Dependency Injection setup code ommitted for clarity
  
  IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();

  // Fluently configure your Command.
  IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("Path/To/Executable")
                            .SetArguments(["arg1", "arg2"])
                            .SetWorkingDirectory("/Path/To/Directory")
                            .RedirectStandardOutput(true)
                           .RedirectStandardError(true);
  
  // Build it as a ProcessConfiguration object when you're ready to use it.
  ProcessConfiguration config = builder.Build();
  
  // Execute the process through ProcessInvoker and get the results.
BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(config);
```

## Acknowledgements

### Projects
This project would like to thank the following projects for their work:
* [Microsoft.Bcl.HashCode](https://github.com/dotnet/maintenance-packages) for providing a backport of the HashCode class and static methods to .NET Standard 2.0
* [Polyfill](https://github.com/SimonCropp/Polyfill) for simplifying .NET Standard 2.0 support

For more information, please see the [THIRD_PARTY_NOTICES file](https://github.com/alastairlundy/CliInvoke/blob/main/THIRD_PARTY_NOTICES.txt).
