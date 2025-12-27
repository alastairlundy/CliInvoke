# CliInvoke

<!-- Badges -->
[![Latest NuGet](https://img.shields.io/nuget/v/CliInvoke.svg)](https://www.nuget.org/packages/CliInvoke/)
[![Latest Pre-release NuGet](https://img.shields.io/nuget/vpre/CliInvoke.svg)](https://www.nuget.org/packages/CliInvoke/)
[![Downloads](https://img.shields.io/nuget/dt/CliInvoke.svg)](https://www.nuget.org/packages/CliInvoke/)
![License](https://img.shields.io/github/license/alastairlundy/CliInvoke)

CliInvoke is a .NET library for interacting with Command Line Interfaces and wrapping around executables.

Launch processes, redirect standard input and output streams, await process completion and much more.

## Features
* Clear separation of concerns between Process Configuration Builders, Process Configuration Models, and Invokers.
* Supports .NET Standard 2.0, .NET 8 and newer TFMs, and has few dependencies.
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

## Installing CliInvoke
CliInvoke is available on [the Nuget Gallery](https://nuget.org) but call be also installed via the ``dotnet`` sdk cli.

The package(s) to install depends on your use case:
* For use in a .NET library - Install the [Abstractions Package](#abstractions-package), your developer users can install the Implementation and Dependency Injection packages.
* For use in a .NET app - Install the [Implementation Package](#implementation-package) and the [Dependency Injection Extensions Package](#extensions-package)

### Abstractions Package
[CliInvoke.Core Nuget](https://nuget.org/packages/CliInvoke.Core)

```bash
dotnet add package CliInvoke.Core
```

### Implementation Package

[CliInvoke Nuget](https://nuget.org/packages/CliInvoke)

```bash
dotnet add package CliInvoke
```

### Extensions Package

[CliInvoke.Extensions Nuget](https://nuget.org/packages/CliInvoke.Extensions)

```bash
dotnet add package CliInvoke.Extensions
```

### Specializations Package
[CliInvoke.Specializations Nuget](https://nuget.org/packages/CliInvoke.Specializations)

```bash
dotnet add package CliInvoke.Specializations
```

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
using CliInvoke.Core.Factories;
using CliInvoke.Core;
using AlastairLundy.CliIinvoke;

using Microsoft.Extensions.DependencyInjection;

// Dependency Injection setup code ommitted for clarity

// Get IProcessConfigurationFactory 
IProcessConfigurationFactory processConfigFactory = serviceProvider.GetRequiredService<IProcessConfigurationFactory>();

// Get IProcessConfigurationInvoker
IProcessConfigurationInvoker _invoker_ = serviceProvider.GetRequiredService<IProcessConfigurationInvoker>();

// Simply create the process configuration.
ProcessConfiguration configuration = processConfigFactory.Create("path/to/exe", "arguments");

// Run the process configuration and get the results.
ProcessResult result = await _invoker.ExecuteAsync(configuration, CancellationToken.None);
```

#### Buffered Execution Example
This example gets a ``BufferedProcessResult`` which contains redirected StandardOutput and StandardError as strings.

```csharp
using CliInvoke.Core.Factories;
using CliInvoke.Core;
using AlastairLundy.CliIinvoke;

using Microsoft.Extensions.DependencyInjection;

// Dependency Injection setup code ommitted for clarity

// Get IProcessConfigurationFactory 
IProcessConfigurationFactory processConfigFactory = serviceProvider.GetRequiredService<IProcessConfigurationFactory>();

// Get IProcessConfigurationInvoker
IProcessConfigurationInvoker _invoker_ = serviceProvider.GetRequiredService<IProcessConfigurationInvoker>();

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
using CliInvoke;
using CliInvoke.Core;

using CliInvoke.Builders;
using CliInvoke.Core.Builders;

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
using CliInvoke;
using CliInvoke.Builders;

using CliInvoke.Core;
using CliInvoke.Core.Builders;

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

## License
CliInvoke is licensed under the MPL 2.0 license. You can learn more about it [here](https://www.mozilla.org/en-US/MPL/)

If you use CliInvoke in your project, please make an exact copy of the contents of CliInvoke's [LICENSE.txt file](https://github.com/alastairlundy/CliInvoke/blob/main/LICENSE.txt) available either in your third party licenses txt file or as a separate txt file.

## Acknowledgements

### Projects
This project would like to thank the following projects for their work:
* [CliWrap](https://github.com/Tyrrrz/CliWrap/) for inspiring this project
* [Polyfill](https://github.com/SimonCropp/Polyfill) for simplifying .NET Standard 2.0 support

For more information, please see the [THIRD_PARTY_NOTICES file](https://github.com/alastairlundy/CliInvoke/blob/main/THIRD_PARTY_NOTICES.txt).
