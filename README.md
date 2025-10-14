# CliInvoke
CliInvoke is a library for interacting with Command Line Interfaces and wrapping around executables.

<img src="https://github.com/alastairlundy/CliInvoke/blob/main/.assets/icon.png" width="192" height="192" alt="CliInvoke Logo">

[![NuGet](https://img.shields.io/nuget/v/AlastairLundy.CliInvoke.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke/) 
[![NuGet](https://img.shields.io/nuget/dt/AlastairLundy.CliInvoke.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke/)

## Table of Contents
* [Features](#features)
* [Why CliInvoke?](#why-use-CliInvoke-over-cliwrap)
* [Installing CliInvoke](#how-to-install-and-use-CliInvoke)
    * [Installing CliInvoke](#installing-CliInvoke)
    * [Supported Platforms](#supported-platforms)
* [CliInvoke Examples](#using-CliInvoke--examples)
      * [Executing Commands](#executing-commands)
* [Contributing to CliInvoke](#how-to-contribute-to-cliinvoke)
* [Roadmap](#cliinvokes-roadmap)
* [License](#license)
  * [CliRunner Assets](#cliinvoke-assets)
* [Acknowledgements](#acknowledgements)

## Features
* Promotes the single responsibility principle and separation of concerns
* For .NET 8 and newer TFMs CliRunner has few dependencies.
* Compatible with .NET Standard 2.0 <sup>1</sup>
* Dependency Injection extensions to make using it easier.
* Support for specific specializations such as running executables or commands via Windows Powershell or CMD on Windows <sup>2</sup>
* [SourceLink](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink) support

<sup>1</sup> - [Polyfill](https://github.com/SimonCropp/Polyfill) is a dependency only required for .NET Standard 2.0 users. [Microsoft.Bcl.HashCode](https://www.nuget.org/packages/Microsoft.Bcl.HashCode) is a dependency only required for .NET Standard 2.0 users.

<sup>2</sup> - The Specialization library is distributed separately [here](https://nuget.org/packages/AlastairLundy.CliInvoke.Specializations).

## Why use CliInvoke over [CliWrap](https://github.com/Tyrrrz/CliWrap/)?
* Greater separation of concerns with the Command class - Command Building, Command Running, and Command Pipe handling are moved to separate classes.
* Supports Dependency Injection
* Classes and code follow the Single Responsibility Principle
* No hidden or additional licensing terms are required beyond the source code license.
* No imported C code - This library is entirely written in C#.
* No lock in regarding Piping support

## How to install and use CliInvoke
CliInvoke is available on Nuget.

These are the CliInvoke projects:
* CliInvoke - The main CliInvoke package.
* [CliInvoke.Extensions](src/CliInvoke.Extensions/README.md)
* [CliInvoke.Specializations](SPECIALIZATIONS_README.md)

### Installing CliInvoke
CliInvoke's packages can be installed via the .NET SDK CLI, Nuget via your IDE or code editor's package interface, or via the Nuget website.

| Package Name                            | Nuget Link                                                                                                          | .NET SDK CLI command                                           |
|-----------------------------------------|---------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------|
| AlastairLundy.CliInvoke                 | [CliInvoke Nuget](https://nuget.org/packages/AlastairLundy.CliInvoke)                                               | ``dotnet add package AlastairLundy.CliInvoke``                 |
| AlastairLundy.CliInvoke.Extensions      | [AlastairLundy.CliInvoke.Extensions Nuget](https://nuget.org/packages/AlastairLundy.CliInvoke.Extensions)           | ``dotnet add package AlastairLundy.CliInvoke.Extensions``      |
| AlastairLundy.CliInvoke.Extensibility   | [AlastairLundy.CliInvoke.Extensibility Nuget](https://nuget.org/packages/AlastairLundy.CliInvoke.Extensibility)     | ``dotnet add package AlastairLundy.CliInvoke.Extensibility``   |
| AlastairLundy.CliInvoke.Specializations | [AlastairLundy.CliInvoke.Specializations Nuget](https://nuget.org/packages/AlastairLundy.CliInvoke.Specializations) | ``dotnet add package AlastairLundy.CliInvoke.Specializations`` |


### Supported Platforms
CliInvoke can be added to .NET Standard 2.0, .NET 8, or .NET 9 supported projects.

The following table details which target platforms are supported for executing commands via CliInvoke. 

| Operating System | Support Status                     | Notes                                                                                     |
|------------------|------------------------------------|-------------------------------------------------------------------------------------------|
| Windows          | Fully Supported :white_check_mark: |                                                                                           |
| macOS            | Fully Supported :white_check_mark: |                                                                                           |
| Mac Catalyst     | Untested Platform :warning:        | Support for this platform has not been tested but should theoretically work.              |
| Linux            | Fully Supported :white_check_mark: |                                                                                           |
| FreeBSD          | Fully Supported :white_check_mark: |                                                                                           |
| Android          | Untested Platform :warning:        | Support for this platform has not been tested but should theoretically work.              |
| IOS              | Not Supported :x:                  | Not supported due to ``Process.Start()`` not supporting IOS. <sup>3</sup>                 | 
| tvOS             | Not Supported :x:                  | Not supported due to ``Process.Start()`` not supporting tvOS <sup>3</sup>                 |
| watchOS          | Not Supported :x:                  | Not supported due to ``Process.Start()`` not supporting watchOS <sup>4</sup>              |
| Browser          | Not Planned :x:                    | Not planned due to not being a valid target Platform for executing programs or processes. |

<sup>3</sup> - See the [Process class documentation](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.start?view=net-9.0#system-diagnostics-process-start) for more info.

<sup>4</sup> - Lack of watchOS support is implied by lack of IOS support since [watchOS is based on IOS](https://en.wikipedia.org/wiki/WatchOS).


**Note:** This library has not been tested on Android or Tizen.

## Using CliInvoke / Examples

### Fluent ``ProcessConfiguration`` building
CliInvoke enables use of a fluent builder style of syntax to easily configure and run Commands.

The following example shows how to configure and build a Command that returns a BufferedProcessResult which contains redirected StandardOutput and StandardError as strings.

```csharp
using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Builders.Abstractions;
using AlastairLundy.CliInvoke.Builders;

using AlastairLundy.Extensions.Processes;

  //Namespace and classs code ommitted for clarity 

  // ServiceProvider and Dependency Injection code ommitted for clarity
  
  ICliCommandInvoker _commandInvoker = serviceProvider.GetRequiredService<ICliCommandInvoker>();

  // Fluently configure your Command.
  ICliCommandConfigurationBuilder builder = new CliCommandConfigurationBuilder("Path/To/Executable")
                            .WithArguments(["arg1", "arg2"])
                            .WithWorkingDirectory("/Path/To/Directory");
  
  // Build it as a CliCommandConfiguration object when you're ready to use it.
  CliCommandConfiguration commandConfig = builder.Build();
  
  // Execute the CliCommand through CommandRunner and get the results.
BufferedProcessResult result = await _commandInvoker.ExecuteBufferedAsync(commandConfig);
```

## How to Contribute to CliInvoke
Thank you in advance for considering contributing to CliInvoke.

Please see the [CONTRIBUTING.md file](CONTRIBUTING.md) for code and localization contributions.

If you want to file a bug report or suggest a potential feature to add, please check out the [GitHub issues page](https://github.com/alastairlundy/CliInvoke/issues/) to see if a similar or identical issue is already open.
If there is not already a relevant issue filed, please [file one here](https://github.com/alastairlundy/CliInvoke/issues/new) and follow the respective guidance from the appropriate issue template.

Thanks.

## CliInvoke's Roadmap
CliInvoke aims to make working with Commands and external processes easier.

Whilst an initial set of features are available in version 1, there is room for more features, and for modifications of existing features in future updates.

That being said, all stable releases from 1.0 onwards must be stable and should not contain regressions.

Future updates should aim focus on one or more of the following:
* Improved ease of use
* Improved stability 
* New features
* Enhancing existing features

## License
CliInvoke is licensed under the MPL 2.0 license. If you modify any of CliInvoke's files then the modified files must be licensed under the MPL 2.0 .

If you use CliInvoke in your project please make an exact copy of the contents of CliInvoke's [LICENSE.txt file](https://github.com/alastairlundy/CliInvoke/blob/main/LICENSE.txt) available either in your third party licenses txt file or as a separate txt file.

## Acknowledgements

### Projects
This project would like to thank the following projects for their work:
* [CliWrap](https://github.com/Tyrrrz/CliWrap/) for inspiring this project
* [Polyfill](https://github.com/SimonCropp/Polyfill) for simplifying .NET Standard 2.0 support

For more information, please see the [THIRD_PARTY_NOTICES file](https://github.com/alastairlundy/CliInvoke/blob/main/THIRD_PARTY_NOTICES.txt).
