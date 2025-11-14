# CliInvoke

<!-- Badges -->
[![Latest NuGet](https://img.shields.io/nuget/v/CliInvoke.svg)](https://www.nuget.org/packages/CliInvoke/)
[![Latest Pre-release NuGet](https://img.shields.io/nuget/vpre/CliInvoke.svg)](https://www.nuget.org/packages/CliInvoke/)
[![Downloads](https://img.shields.io/nuget/dt/CliInvoke.svg)](https://www.nuget.org/packages/CliInvoke/)
![License](https://img.shields.io/github/license/alastairlundy/CliInvoke)

<img src="https://github.com/alastairlundy/CliInvoke/blob/main/.assets/icon.png" width="192" height="192" alt="CliInvoke Logo">

CliInvoke is a .NET library for interacting with Command Line Interfaces and wrapping around executables.

Launch processes, redirect standard input and output streams, await process completion and much more.

## Table of Contents
* [Features](#features)
* [Comparison vs Alternatives](#comparison-vs-alternatives)
* [Installing CliInvoke](#installing-cliinvoke)
   * [Supported Platforms](#supported-platforms)
* [CliInvoke Examples](#examples)
* [Contributing to CliInvoke](#how-to-contribute-to-cliinvoke)
* [Used By](#used-by)
* [Roadmap](#cliinvokes-roadmap)
* [License](#license)
* [Acknowledgements](#acknowledgements)

## Features
* Clear separation of concerns between Process Configuration Builders, Process Configuration Models, and Invokers.
* Supports .NET Standard 2.0, .NET 8 and newer TFMs, and has few dependencies.
* Has Dependency Injection extensions to make using it a breeze.
* Support for specific specializations such as running executables or commands via Windows PowerShell or CMD on Windows <sup>1</sup>
* [SourceLink](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink) support

<sup>1</sup> Specializations library distributed separately.

## Comparison vs Alternatives

| Feature / Criterion                                                   | CliInvoke | [CliWrap](https://github.com/Tyrrrz/CliWrap/) | [ProcessX](https://github.com/Cysharp/ProcessX) |
|------------------------------------------------------------------------|:---------:|:-------:|:--------:|
| Dedicated builder, model, and invoker types (clear separation of concerns) | ✅        | ❌     | ❌      |
| Dependency Injection registration extensions                           | ✅        | ❌     | ❌      |
| Installable via NuGet                                                  | ✅        | ✅     | ✅      |
| Official cross‑platform support (advertised: Windows/macOS/Linux/BSD)  | ✅        | ✅*    | ❌*     |
| Buffered and non‑buffered execution modes                              | ✅        | ✅     | ✅      |
| Small surface area and minimal dependencies                            | ✅        | ✅     | ✅      |
| Licensing / repository additional terms                                | ✅ (MPL‑2.0) | ⚠️ (MIT; test project references a source‑available library; repo contains an informal "Terms of Use" statement) | ✅ (MIT) |

Notes:
- *Indicates not explicitly advertised for all listed OSes but may work in practice; check each project's docs.
- The CliWrap repository includes a test project that references a source‑available (non‑permissive) library; that library is used for tests and is not distributed with the runtime package. The repo also contains an informal "Terms of Use" statement — review repository files if legal certainty is required.


## Installing CliInvoke
CliInvoke is available on [the NuGet Gallery](https://nuget.org) but call be also installed via the ``dotnet`` SDK CLI.

The package(s) to install depends on your use case:
* For use in a .NET library - Install the [Abstractions Package](#abstractions-package), your developer users can install the Implementation and Dependency Injection packages.
* For use in a .NET app - Install the [Implementation Package](#implementation-package) and the [Dependency Injection Extensions Package](#dependency-injection-extensions)

| Project type / Need                                                         | Packages to install (dotnet add package ...)                                                                 | Notes |
|------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------|-------|
| Library author (provide abstractions only)                                   | `CliInvoke.Core`                                                                                | Only the Core (abstractions) package — consumers can choose implementations. |
| Library or app that needs concrete builders / implementations                | `CliInvoke.Core`, `CliInvoke`                                                      | Implementation package plus Core for models/abstractions. |
| Desktop or Console application (common case — use DI & convenience helpers)  | `CliInvoke.Core`, `CliInvoke`, `CliInvoke.Extensions`                | Includes DI registration and convenience extensions for easy setup. |
| Any project that needs platform‑specific or shell specializations (optional) | `CliInvoke.Specializations` (install in addition to the packages above as needed)               | Adds Cmd/PowerShell and other specializations; include only when required. |

### Links to packages
[CliInvoke.Core Nuget](https://nuget.org/packages/CliInvoke.Core)
[CliInvoke Nuget](https://nuget.org/packages/CliInvoke)
[CliInvoke.Extensions Nuget](https://nuget.org/packages/CliInvoke.Extensions)
[CliInvoke.Specializations Nuget](https://nuget.org/packages/CliInvoke.Specializations)

## Supported Platforms
CliInvoke supports Windows, macOS, Linux, FreeBSD, Android, and potentially some other operating systems.

For more details see the [list of supported platforms](docs/docs/Supported-OperatingSystems.md)

## Getting started

Install the packages you need (example: implementation + DI extensions):

```bash
dotnet add package CliInvoke
dotnet add package CliInvoke.Extensions
```

Minimal Program.cs (console app) — registers services, builds a simple process configuration, and runs it buffered:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Core.Factories;

class Program
{
    static async Task Main()
    {
        var services = new ServiceCollection();
        services.AddCliInvoke(); // from CliInvoke.Extensions
        var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IProcessConfigurationFactory>();
        var invoker = provider.GetRequiredService<IProcessConfigurationInvoker>();

        // Create a simple configuration (adjust path/args for your OS)
        var config = factory.Create("dotnet", "--info");

        // Run and get buffered output
        BufferedProcessResult result = await invoker.ExecuteBufferedAsync(config, CancellationToken.None);

        Console.WriteLine($"ExitCode: {result.ExitCode}");
        Console.WriteLine("Stdout:");
        Console.WriteLine(result.StandardOutput);
        Console.WriteLine("Stderr:");
        Console.WriteLine(result.StandardError);
    }
}
```

Notes
- Replace "dotnet --info" with the executable and arguments you need for your platform.
- For non‑buffered/streaming scenarios, use ExecuteAsync/ExecuteBufferedAsync variants and builder options to redirect streams instead of buffering everything in memory.

## Examples

### Simple ``ProcessConfiguration`` creation with Factory Pattern
This approach uses the ``IProcessConfigurationFactory`` interface factory to create a ``ProcessConfiguration``. It requires fewer parameters and sets up more defaults for you. 

It can be provided with a ``Action<IProcessConfigurationBuilder> configure`` optional parameter where greater control is desired.

#### Non-Buffered Execution Example
This example gets a non buffered ``ProcessResult`` that contains basic process exit code, Id, and other information.

```csharp
using CliInvoke.Core.Factories;
using CliInvoke.Core;
using AlastairLundy.CliIinvoke;

using Microsoft.Extensions.DependencyInjection;

// Dependency Injection setup code omitted for clarity

// Get services 
IProcessConfigurationFactory processConfigFactory = serviceProvider.GetRequiredService<IProcessConfigurationFactory>();
IProcessConfigurationInvoker _invoker_ = serviceProvider.GetRequiredService<IProcessConfigurationInvoker>();

// Simply create the process configuration.
ProcessConfiguration configuration = processConfigFactory.Create("path/to/exe", "arguments");

// Run the process configuration and get the results.
ProcessResult result = await _invoker.ExecuteAsync(configuration, CancellationToken.None);
```

#### Buffered Execution Example
This example gets a ``BufferedProcessResult`` which contains redirected Standard Output and Standard Error as strings.

```csharp
using CliInvoke.Core.Factories;
using CliInvoke.Core;

using Microsoft.Extensions.DependencyInjection;

// Dependency Injection setup code omitted for clarity

// Get services 
IProcessConfigurationFactory processConfigFactory = serviceProvider.GetRequiredService<IProcessConfigurationFactory>();
IProcessConfigurationInvoker _invoker_ = serviceProvider.GetRequiredService<IProcessConfigurationInvoker>();

// Simply create the process configuration.
ProcessConfiguration configuration = processConfigFactory.Create("path/to/exe", "arguments");

// Run the process configuration and get the results.
BufferedProcessResult result = await _invoker.ExecuteBufferedAsync(configuration, CancellationToken.None);
```


### Advanced Configuration with Builders

The following examples show how to configure and build a ``ProcessConfiguration`` depending on whether Buffering the output is desired.

#### Non-Buffered Execution Example
This example gets a non buffered ``ProcessResult`` that contains basic process exit code, id, and other information.

```csharp
using CliInvoke;
using CliInvoke.Core;

using CliInvoke.Builders;
using CliInvoke.Core.Builders;

using Microsoft.Extensions.DependencyInjection;

  //Namespace and class code ommitted for clarity 

  // ServiceProvider and Dependency Injection setup code oomittedfor clarity
  
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

## How to Build CliInvoke's code
Please see [building-cliinvoke.md](docs/docs/building-cliinvoke.md) for how to build CliInvoke from source.

## How to Contribute to CliInvoke
Please see the [CONTRIBUTING.md file](CONTRIBUTING.md) for code and localization contributions.

If you want to file a bug report or suggest a potential feature to add, please check out the [GitHub issues page](https://github.com/alastairlundy/CliInvoke/issues/) to see if a similar or identical issue is already open.
If there isn't already a relevant issue filed, please [file one here](https://github.com/alastairlundy/CliInvoke/issues/new) and follow the respective guidance from the appropriate issue template.

## Used By
CliInvoke is used by these projects:
* [WCountLib.Providers.wc](https://github.com/alastairlundy/WCount/tree/main/src/lib/WCountLib.Providers.wc) - Implements WCountLib.Abstractions using the Unix ``wc`` command.

Want your project added to this list? [Open an issue](https://github.com/alastairlundy/cliinvoke/issues/new/)

## CliInvoke's Roadmap
CliInvoke aims to make working with Commands and external processes easier.

Whilst an initial set of features are available in version 1, there is room for more features, and for modifications of existing features in future updates.

Future updates may focus on one or more of the following:
* Improved ease of use
* Improved stability 
* New features
* Enhancing existing features

## License
CliInvoke is licensed under the MPL 2.0 license. You can learn more about it [here](https://www.mozilla.org/en-US/MPL/)

If you use CliInvoke in your project please make an exact copy of the contents of CliInvoke's [LICENSE.txt file](https://github.com/alastairlundy/CliInvoke/blob/main/LICENSE.txt) available either in your third party licenses TXT file or as a separate TXT file in the project's repository.

### CliInvoke Assets
CliInvoke's Icon is proprietary with all rights reserved to me (Alastair Lundy).

If you fork CliInvoke and re-distribute it, please replace the usage of the icon unless you have prior written approval from me. 

## Acknowledgements

### Projects
This project would like to thank the following projects for their work:
* [CliWrap](https://github.com/Tyrrrz/CliWrap/) for inspiring this project
* [Polyfill](https://github.com/SimonCropp/Polyfill) for simplifying .NET Standard 2.0 support

For more information, please see the [THIRD_PARTY_NOTICES file](https://github.com/alastairlundy/CliInvoke/blob/main/THIRD_PARTY_NOTICES.txt).
