# CliInvoke

<!-- Badges -->
[![Latest NuGet](https://img.shields.io/nuget/v/AlastairLundy.CliInvoke.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke/)
[![Latest Pre-release NuGet](https://img.shields.io/nuget/vpre/AlastairLundy.CliInvoke.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke/)
[![Downloads](https://img.shields.io/nuget/dt/AlastairLundy.CliInvoke.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke/)
![License](https://img.shields.io/github/license/alastairlundy/CliInvoke)

<img src="https://github.com/alastairlundy/CliInvoke/blob/main/.assets/icon.png" width="192" height="192" alt="CliInvoke Logo">

CliInvoke is a .NET library for interacting with Command Line Interfaces and wrapping around executables.

Launch processes, redirect standard input and output streams, await process completion and much more.

## Features
* Clear separation of concerns between Process Configuration Builders, Process Configuration Models, and Invokers.
* Supports .NET Standard 2.0, .NET 8 and newer TFMs, and has few dependencies.
* Has Dependency Injection extensions to make using it a breeze.
* Support for specific specializations such as running executables or commands via Windows Powershell or CMD on Windows <sup>1</sup>
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

## Supported Platforms
CliInvoke can currently be added to .NET Standard 2.0, .NET 8, or .NET 9 or newer supported projects.

The following table details which target platforms are supported for executing commands via CliInvoke. 

| Operating System | Support Status                     | Notes                                                                                       |
|------------------|------------------------------------|---------------------------------------------------------------------------------------------|
| Windows          | Fully Supported :white_check_mark: |                                                                                             |
| macOS            | Fully Supported :white_check_mark: |                                                                                             |
| Mac Catalyst     | Untested Platform :warning:        | Support for this platform has not been tested but should theoretically work.                |
| Linux            | Fully Supported :white_check_mark: |                                                                                             |
| FreeBSD          | Fully Supported :white_check_mark: |                                                                                             |
| Android          | Untested Platform :warning:        | Support for this platform has not been tested but should theoretically work.                |
| IOS              | Not Supported :x:                  | Not supported due to ``Process.Start()`` not supporting IOS. <sup>3</sup>                   | 
| tvOS             | Not Supported :x:                  | Not supported due to ``Process.Start()`` not supporting tvOS <sup>3</sup>                   |
| watchOS          | Not Supported :x:                  | Not supported due to ``Process.Start()`` not supporting watchOS <sup>4</sup>                |
| Browser          | Not Planned :x:                    | Not supported due to not being a valid target Platform for executing programs or processes. |

<sup>3</sup> - See the [Process class documentation](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.start?view=net-9.0#system-diagnostics-process-start) for more info.

<sup>4</sup> - Lack of watchOS support is implied by lack of IOS support since [watchOS is based on IOS](https://en.wikipedia.org/wiki/WatchOS).

**Note:** This library has not been tested on Android or Tizen.

## Examples
### Quickstart
For a simpler way to configure ``ProcessConfiguration`` objects, check out [CliInvoke.Extensions](https://nuget.org/packages/AlastairLundy.CliInvoke.Extensions)

### Advanced Configuration with Builders

#### ``IProcessConfigurationInvoker``
The following examples shows how to configure and build a ``ProcessConfiguration`` depending on whether Buffering the output is desired.

##### Non-Buffered Execution Example
This example gets a non buffered ``ProcessResult`` that contains basic process exit code, id, and other information.

```csharp
using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Core;

using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;

  //Namespace and class code ommitted for clarity 

  // ServiceProvider and Dependency Injection setup code ommitted for clarity
  
  IProcessConfigurationInvoker _processConfigInvoker = serviceProvider.GetRequiredService<IProcessConfigurationInvoker>();

  // Fluently configure your Command.
  IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("Path/To/Executable")
                            .WithArguments(["arg1", "arg2"])
                            .WithWorkingDirectory("/Path/To/Directory");
  
  // Build it as a ProcessConfiguration object when you're ready to use it.
  ProcessConfiguration config = builder.Build();
  
  // Execute the process through ProcessInvoker and get the results.
ProcessResult result = await _processConfigInvoker.ExecuteAsync(config);
```

##### Buffered Execution Example
This example gets a ``BufferedProcessResult`` which contains redirected StandardOutput and StandardError as strings.

```csharp
using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Core;

using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;

  //Namespace and class code ommitted for clarity 

  // ServiceProvider and Dependency Injection setup code ommitted for clarity
  
  IProcessConfigurationInvoker _processConfigInvoker = serviceProvider.GetRequiredService<IProcessConfigurationInvoker>();

  // Fluently configure your Command.
  IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("Path/To/Executable")
                            .WithArguments(["arg1", "arg2"])
                            .WithWorkingDirectory("/Path/To/Directory");
  
  // Build it as a ProcessConfiguration object when you're ready to use it.
  ProcessConfiguration config = builder.Build();
  
  // Execute the process through ProcessInvoker and get the results.
BufferedProcessResult result = await _processConfigInvoker.ExecuteBufferedAsync(config);
```

## How to Build CliInvoke's code
Please see [building-cliinvoke.md](https://github.com/alastairlundy/CliInvoke/blob/main/docs/docs/building-cliinvoke.md) for how to build CliInvoke from source.

## How to Contribute to CliInvoke
Thank you in advance for considering contributing to CliInvoke.

Please see the [CONTRIBUTING.md file](https://github.com/alastairlundy/CliInvoke/blob/main/CONTRIBUTING.md) for code and localization contributions.

If you want to file a bug report or suggest a potential feature to add, please check out the [GitHub issues page](https://github.com/alastairlundy/CliInvoke/issues/) to see if a similar or identical issue is already open.
If there is not already a relevant issue filed, please [file one here](https://github.com/alastairlundy/CliInvoke/issues/new) and follow the respective guidance from the appropriate issue template.

Thanks.

## License
CliInvoke is licensed under the MPL 2.0 license. You can learn more about it [here](https://www.mozilla.org/en-US/MPL/)

If you use CliInvoke in your project please make an exact copy of the contents of CliInvoke's [LICENSE.txt file](https://github.com/alastairlundy/CliInvoke/blob/main/LICENSE.txt) available either in your third party licenses txt file or as a separate txt file.

## Acknowledgements

### Projects
This project would like to thank the following projects for their work:
* [CliWrap](https://github.com/Tyrrrz/CliWrap/) for inspiring this project
* [Polyfill](https://github.com/SimonCropp/Polyfill) for simplifying .NET Standard 2.0 support

For more information, please see the [THIRD_PARTY_NOTICES file](https://github.com/alastairlundy/CliInvoke/blob/main/THIRD_PARTY_NOTICES.txt).