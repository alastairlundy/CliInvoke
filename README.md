# CliInvoke

[![NuGet](https://img.shields.io/nuget/v/AlastairLundy.CliInvoke.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke/) 
[![NuGet](https://img.shields.io/nuget/dt/AlastairLundy.CliInvoke.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke/)

CliInvoke is a .NET library for interacting with Command Line Interfaces and wrapping around executables.

Launch processes, redirect standard input and output streams, await process completion and so much more.

<img src="https://github.com/alastairlundy/CliInvoke/blob/main/.assets/icon.png" width="192" height="192" alt="CliInvoke Logo">

## Table of Contents
* [Features](#features)
* [Why CliInvoke?](#why-use-CliInvoke-over-cliwrap)
* [Installing CliInvoke](#how-to-install-and-use-CliInvoke)
    * [Installing CliInvoke](#installing-CliInvoke)
    * [Supported Platforms](#supported-platforms)
* [CliInvoke Examples](#using-CliInvoke--examples)
      * [Executing Commands](#executing-commands)
* [Contributing to CliInvoke](#how-to-contribute-to-clirunner)
* [Roadmap](#clirunners-roadmap)
* [License](#license)
  * [CliRunner Assets](#clirunner-assets)
* [Acknowledgements](#acknowledgements)

## Features
* Promotes the single responsibility principle and separation of concerns
* For .NET 8 and newer TFMs CliRunner has few dependencies.
* Compatible with .NET Standard 2.0<sup>1</sup>
* Dependency Injection extensions to make using CliInvoke a breeze.
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
* Uses .NET's built in ``Process`` type.

## How to install and use CliInvoke
CliInvoke is available on Nuget.

These are the CliInvoke projects:
* CliInvoke - The main CliInvoke package.
* [CliInvoke.Extensions](CliInvokeLibrary/CliInvoke.Extensions/README.md)
* [CliInvoke.Specializations](SPECIALIZATIONS_README.md)

### Installing CliInvoke
CliInvoke's packages can be installed via the .NET SDK CLI, Nuget via your IDE or code editor's package interface, or via the Nuget website.

| Package Name                            | Nuget Link                                                                                                          | .NET SDK CLI command                                           |
|-----------------------------------------|---------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------|
| AlastairLundy.CliInvoke                 | [CliInvoke Nuget](https://nuget.org/packages/AlastairLundy.CliInvoke)                                               | ``dotnet add package AlastairLundy.CliInvoke``                 |
| AlastairLundy.CliInvoke.Extensions      | [AlastairLundy.CliInvoke.Extensions Nuget](https://nuget.org/packages/AlastairLundy.CliInvoke.Extensions)           | ``dotnet add package AlastairLundy.CliInvoke.Extensions``      |
| AlastairLundy.CliInvoke.Specializations | [AlastairLundy.CliInvoke.Specializations Nuget](https://nuget.org/packages/AlastairLundy.CliInvoke.Specializations) | ``dotnet add package AlastairLundy.CliInvoke.Specializations`` |


### Supported Platforms
CliInvoke can be added to .NET Standard 2.0, .NET 8, or .NET 9 supported projects.

The following table details which target platforms are supported for executing commands via CliInvoke. 

| Operating System | Support Status                     | Notes                                                                                       |
|------------------|------------------------------------|---------------------------------------------------------------------------------------------|
| Windows          | Fully Supported :white_check_mark: |                                                                                             |
| macOS            | Fully Supported :white_check_mark: |                                                                                             |
| Mac Catalyst     | Untested Platform :warning:        | Support for this platform has not been tested but should theoretically work.                |
| Linux            | Fully Supported :white_check_mark: |                                                                                             |
| FreeBSD          | Fully Supported :white_check_mark: |                                                                                             |
| Android          | Untested Platform :warning:        | Support for this platform has not been tested but should theoretically work.                |
| IOS              | Not Supported :x:                  | Not supported due to ``Process.Start()`` not supporting IOS. <sup>3</sup>                             | 
| tvOS             | Not Supported :x:                  | Not supported due to ``Process.Start()`` not supporting tvOS <sup>3</sup>                             |
| watchOS          | Not Supported :x:                  | Not supported due to ``Process.Start()`` not supporting watchOS <sup>4</sup>                          |
| Browser          | Not Planned :x:  | Not supported due to not being a valid target Platform for executing programs or processes. |

<sup>3</sup> - See the [Process class documentation](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.start?view=net-9.0#system-diagnostics-process-start) for more info.

<sup>4</sup> - Lack of watchOS support is implied by lack of IOS support since [watchOS is based on IOS](https://en.wikipedia.org/wiki/WatchOS).


**Note:** This library has not been tested on Android or Tizen.

## Using CliInvoke / Examples
One of the main use cases for CliInvoke is intended to be executing programs programatically, but other valid use cases also exist such as [safer Process Running](#safer-process-running).

### Executing Commands
CliInvoke enables use of a fluent builder style of syntax to easily configure and run Commands.

The following example shows how to configure and build a Command that returns a BufferedProcessResult which contains redirected StandardOutput and StandardError as strings.

```csharp
using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Core;

using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;

using AlastairLundy.CliInvoke.Primitives.Results;

  //Namespace and class code ommitted for clarity 

  // ServiceProvider and Dependency Injection setup code ommitted for clarity
  
  IProcessInvoker _processInvoker_ = serviceProvider.GetRequiredService<IProcessInvoker>();

  // Fluently configure your Command.
  IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("Path/To/Executable")
                            .WithArguments(["arg1", "arg2"])
                            .WithWorkingDirectory("/Path/To/Directory");
  
  // Build it as a ProcessConfiguration object when you're ready to use it.
  ProcessConfiguration config = builder.Build();
  
  // Execute the process through ProcessInvoker and get the results.
BufferedProcessResult result = await _processInvoker_.ExecuteBufferedAsync(config);
```

## How to Build CliInvoke's code

### Requirements
CliInvoke requires the latest .NET release SDK to be installed to target all supported TFM (Target Framework Moniker) build targets.

Currently, the required .NET SDK is .NET 9. 

The current build targets include: 
* .NET 8
* .NET 9
* .NET Standard 2.0

Any version of the .NET 9 SDK can be used, but using the latest version is preferred.

### Versioning new releases
CliInvoke aims to follow Semantic versioning with ```[Major].[Minor].[Build]``` for most circumstances and an optional ``.[Revision]`` when only a configuration change is made, or a new build of a preview release is made.

#### Pre-releases
Pre-release versions should have a suffix of -alpha, -beta, -rc, or -preview followed by a ``.`` and what pre-release version number they are. The number should be incremented by 1 after each release unless it only contains a configuration change, or another packaging, or build change. An example pre-release version may look like 1.1.0-alpha.2 , this version string would indicate it is the 2nd alpha pre-release version of 1.1.0 .

#### Stable Releases
Stable versions should follow semantic versioning and should only increment the Revision number if a release only contains configuration or build packaging changes, with no change in functionality, features, or even bug or security fixes.

Releases that only implement bug fixes should see the Build version incremented.

Releases that add new non-breaking changes should increment the Minor version. Minor breaking changes may be permitted in Minor version releases where doing so is necessary to maintain compatibility with an existing supported platform, or an existing piece of code that requires a breaking change to continue to function as intended.

Releases that add major breaking changes or significantly affect the API should increment the Major version. Major version releases should not be released with excessive frequency and should be released when there is a genuine need for the API to change significantly for the improvement of the project.

### Building for Testing
You can build for testing by building the desired project within your IDE or VS Code, or manually by entering the following command: ``dotnet build -c Debug``.

If you encounter any bugs or issues, try running the ``CliInvoke.Tests`` project and setting breakpoints in the affected CliInvoke project's code where appropriate. Failing that, please [report the issue](https://github.com/alastairlundy/CliInvoke/issues/new/) if one doesn't already exist for the bug(s).

### Building for Release
Before building a release build, ensure you apply the relevant changes to the relevant ``.csproj`` file corresponding to the package you are trying to build:
* Update the Package Version variable 
* Update the project file's Changelog
* Remove/replace the CliInvoke icon if distributing a non-official release build to a wider audience. See [CliInvoke Assets](#CliInvoke-assets) for more details.

You should ensure the project builds under debug settings before producing a release build.

#### Producing Release Builds
To manually build a project for release, enter ``dotnet build -c Release /p:ContinuousIntegrationBuild=true`` for a release with [SourceLink](https://github.com/dotnet/sourcelink) enabled or just ``dotnet build -c Release`` for a build without SourceLink.

Builds should generally always include Source Link and symbol packages if intended for wider distribution.

**NOTES**: 
* ``CliInvoke.Specializations`` and ``CliInvoke.Extensions`` both take a dependency on the CliInvoke base package from Nuget - For the respective libraries to use a newer CliInvoke version, that version must be published on Nuget.

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

### CliInvoke Assets
CliInvoke's Icon is NOT licensed under the MPL 2.0 license and is licensed under Copyright with all rights reserved to me (Alastair Lundy).

If you fork CliInvoke and re-distribute it, please replace the usage of the icon unless you have prior written agreements from me. 

## Acknowledgements

### Projects
This project would like to thank the following projects for their work:
* [CliWrap](https://github.com/Tyrrrz/CliWrap/) for inspiring this project
* [Polyfill](https://github.com/SimonCropp/Polyfill) for simplifying .NET Standard 2.0 support

For more information, please see the [THIRD_PARTY_NOTICES file](https://github.com/alastairlundy/CliInvoke/blob/main/THIRD_PARTY_NOTICES.txt).
