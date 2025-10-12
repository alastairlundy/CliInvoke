# CliInvoke.Core
This package contains Process Running and handling abstractions as well as common types used by implementing classes.

For an implementing package, check out [``CliInvoke``](https://www.nuget.org/packages/AlastairLundy.CliInvoke/).

Key Abstractions:
* ``IProcessConfigurationInvoker``

* Piping:
  * ``IProcessPipeHandler``

* Fluent Builders:
  * ``IArgumentsBuilder`` - An interface to assist with Argument Building and argument escaping.
  * ``IEnvironmentVariablesBuilder`` - An interface to assist with setting Environment variables.
  * ``IProcessConfigurationBuilder`` - An interface to fluently configure and build ``ProcessConfiguration`` objects.
  * ``IProcessExitConfigurationBuilder`` - An interface to fluently configure and build ``ProcessExitConfiguration`` objects.
  * ``IProcessResourcePolicyBuilder`` - An interface to fluently configure and build ``ProcessResourcePolicy`` objects.
  * ``IProcessTimeoutPolicyBuilder``
  * ``IUserCredentialBuilder``

[![NuGet](https://img.shields.io/nuget/v/AlastairLundy.CliInvoke.Core.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Core/)
[![NuGet](https://img.shields.io/nuget/dt/AlastairLundy.CliInvoke.Core.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Core/)

## Table of Contents
* [Features](#features)
* [Installing CliInvoke.Core](#how-to-install-and-use-cliinvokecore)
    * [Compatibility](#supported-platforms)
* [Examples](#examples)
* [Contributing to CliInvoke.Core](#how-to-contribute)
* [License](#license)
* [Acknowledgements](#acknowledgements)

## Features
* Easy to use safe Process Running classes and interfaces
* Models that help abstract away some of the complicated nature of Process objects
* [SourceLink](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink) support

## How to install and use CliInvoke.Core
CliInvoke.Core is available on [the Nuget Gallery](https://nuget.org) [here](https://nuget.org/packages/AlastairLundy.CliInvoke.Core).

### Installing CliInvoke.Core
CliInvoke.Core packages can be installed via the .NET SDK CLI, Nuget via your IDE or code editor's package interface, or via the Nuget website.

| Package Name                 | Nuget Link                                                                                    | .NET SDK CLI command                                |
|------------------------------|-----------------------------------------------------------------------------------------------|-----------------------------------------------------|
| AlastairLundy.CliInvoke.Core | [AlastairLundy.CliInvoke.Core Nuget](https://nuget.org/packages/AlastairLundy.CliInvoke.Core) | ``dotnet add package AlastairLundy.CliInvoke.Core`` |

### Supported Platforms
CliInvoke.Core can be added to any .NET Standard 2.0, .NET 8, or .NET 9 or newer supported project.

The following table details which target platforms are supported for running Processes.

| Operating System/Platform specific TFM | Support Status                     | Notes                                                                                               |
|----------------------------------------|------------------------------------|-----------------------------------------------------------------------------------------------------|
| Windows                                | Fully Supported :white_check_mark: |                                                                                                     |
| macOS                                  | Fully Supported :white_check_mark: |                                                                                                     |
| Mac Catalyst                           | Untested Platform :warning:        | Support for this platform has not been tested but should theoretically work.                        |
| Linux                                  | Fully Supported :white_check_mark: |                                                                                                     |
| FreeBSD                                | Fully Supported :white_check_mark: |                                                                                                     |
| Android                                | Untested Platform :warning:        | Support for this platform has not been tested but should theoretically work.                        |
| IOS                                    | Not Supported :x:                  | Not supported due to ``Process.Start()`` not supporting IOS. ^2                                     | 
| tvOS                                   | Not Supported :x:                  | Not supported due to ``Process.Start()`` not supporting tvOS ^2                                     |
| watchOS                                | Not Supported :x:                  | Not supported due to ``Process.Start()`` not supporting watchOS ^3                                  |
| Browser                                | Not Planned :x:                    | Not planned due to Client Side Rendering not being a valid target Platform for executing processes. |

^2 - See the [Process class documentation](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.start?view=net-9.0#system-diagnostics-process-start) for more info.

^3 - lack of IOS support implies lack of watchOS support since [watchOS is based on IOS](https://en.wikipedia.org/wiki/WatchOS).


**Note:** This library has not been tested on Android or Tizen.

## Using CliInvoke / Examples
The two main use cases for CliInvoke are intended to be:
1. [executing Programs/Commands programatically](#running-programscommands) which involves using abstractions to improve the experience of external Program/Command running.
2. [safe Process Running](#safe-process-running) which involves avoiding the pitfalls of the ``Process`` class whilst still dealing with ``ProcessStartInfo``.

CliInvoke provides both options to give developers a choice in the approach they adopt.
They are both equally safe and valid.

### Approaches

#### Safe Process Running
CliInvoke offers safe abstractions around Process Running to avoid accidentally not disposing of Processes,
along with avoiding other pitfalls.

#### Running Programs/Commands
Because of how much of a minefield the ``Process`` class is and how difficult it can be to configure correctly,
CliInvoke provides some abstractions to make it easier to configure Programs/Commands to be run.

CliInvoke provides fluent builder interfaces and implementing classes to easily configure ``ProcessConfiguration``.
``ProcessConfiguration`` is CliInvoke's main form of Process configuration (hence the name).

### Approach Examples

#### ``IProcessConfigurationInvoker``
The following examples shows how to configure and build a ``ProcessConfiguration`` depending on whether Buffering the output is desired.

##### Non-Buffered Execution Example
This example gets a non buffered ``ProcessResult`` that contains basic process exit code, id, and other information.

```csharp
using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Core;

using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;

using AlastairLundy.CliInvoke.Core.Primitives;

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

using AlastairLundy.CliInvoke.Core.Primitives;

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

### Command/Program Execution

## How to Build CliInvoke's code
Please see [building-cliinvoke.md](docs/docs/building-cliinvoke.md) for how to build CliInvoke from source.

## How to Contribute
Thank you in advance for considering contributing to CliInvoke.

Please see the [CONTRIBUTING.md file](https://github.com/alastairlundy/CliInvoke/blob/main/CONTRIBUTING.md) for code and localization contributions.

If you want to file a bug report or suggest a potential feature to add, please check out the [GitHub issues page](https://github.com/alastairlundy/CliInvoke/issues/) to see if a similar or identical issue is already open.
If there is not already a relevant issue filed, please [file one here](https://github.com/alastairlundy/CliInvoke/issues/new) and follow the respective guidance from the appropriate issue template.

Thanks.

## License
CliInvoke.Core is licensed under the MPL 2.0 license. You can learn more about it [here](https://www.mozilla.org/en-US/MPL/)

If you use CliInvoke.Core in your project, please make an exact copy of the contents of CliInvoke.Core's [LICENSE.txt file](https://github.com/alastairlundy/CliInvoke/blob/main/LICENSE.txt) available either in your third party licenses txt file or as a separate txt file.

## Acknowledgements

### Projects
This project would like to thank the following projects for their work:
* [Microsoft.Bcl.HashCode](https://github.com/dotnet/maintenance-packages) for providing a backport of the HashCode class and static methods to .NET Standard 2.0
* [Polyfill](https://github.com/SimonCropp/Polyfill) for simplifying .NET Standard 2.0 support

For more information, please see the [THIRD_PARTY_NOTICES file](https://github.com/alastairlundy/CliInvoke/blob/main/THIRD_PARTY_NOTICES.txt).
