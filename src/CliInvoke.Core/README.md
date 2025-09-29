# CliInvoke.Core
This package contains Process Running and handling abstractions as well as common types used by implementing classes.

For an implementing package, check out [``CliInvoke``](https://www.nuget.org/packages/AlastairLundy.CliInvoke/).

Key Abstractions:
* ``IProcessInvoker``
* ``IProcessFactory``

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
CliInvoke.Core can be added to any .NET Standard 2.0, .NET Standard 2.1, .NET 8, or .NET 9 supported project.

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

## Examples
One of the main use cases for CliInvoke.Core is intended to be [safe Process Running](#safe-process-running).

### Safer Process Running
CliInvoke.Core offers safe abstractions around Process Running to avoid accidentally not disposing of Processes after they are executed.

``IProcessFactory`` and ``IProcessInvoker`` are both equally capable of fulfilling this criterion, **however** ``IProcessFactory`` enables more direct control over Process related primitives prior to running the Process.

If you don't want to use CliInvoke's abstractions around Processes, such as ``ProcessConfiguration`` and CliInvoke's other primitives,  then ``IProcessFactory`` is a better fit. 

**Note**: Neither ``IProcessFactory`` nor ``IProcessInvoker`` are dependent upon on the other to work.

#### ``IProcessFactory``
``IProcessFactory`` is an interface for enabling easy Process Creation, Running, and Disposal depending on the methods used.

The ``From`` method and its overloads provide for easy standalone process creation.
The ``StartNew`` method provides for creating and starting new processes within the same method call.

The ``WaitForExitAsync``, ``WaitForBufferedExitAsync``, ``WaitForPipedExitAsync`` methods provide for:
1. safe process running (including process disposal in the case of an ``Exception``)
2. gathering the results of the Process's execution (varies depending on the specific method)
3. disposing of the Process after it has exited
4. returning the gathered Process execution results

These examples show how they might be used:

##### ``WaitForExitAsync``
```csharp
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Primitives;

// Using namespaces for Dependency Injection code ommitted for clarity

      // Dependency Injection setup code ommitted for clarity

    IProcessFactory _processFactory = serviceProvider.GetRequiredService<IProcessFactory>();
    
    // Define processStartInfo here
    
    // This process that is returned is a Process that has been started.
    Process process = _processFactory.StartNew(processStartInfo);
    
    // Wait for the Process to finish before safely disposing of it.
   ProcessResult result = await processFactory.WaitForExitAsync(process);
```

##### ``WaitForBufferedExitAsync``
```csharp
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Primitives;

// Using namespaces for Dependency Injection code ommitted for clarity

      // Dependency Injection setup code ommitted for clarity

    IProcessFactory _processFactory = serviceProvider.GetRequiredService<IProcessFactory>();
    
    // Define processStartInfo here
    
    Process process = _processFactory.From(processStartInfo);
    
    process.Start();
    
    // Wait for the Process to finish before safely disposing of it.
   BufferedProcessResult result = await processFactory.WaitForBufferedExitAsync(process);
```

Asynchronous methods in ``IProcessFactory`` allow for an optional CancellationToken parameter.

Some overloads for ``WaitForExitAsync`` and ``WaitForBufferedExitAsync`` allow for specifying ProcessResultValidation.

### Command/Program Execution

## How to Contribute
Thank you in advance for considering contributing to CliInvoke.

Please see the [CONTRIBUTING.md file](https://github.com/alastairlundy/CliInvoke/blob/main/CONTRIBUTING.md) for code and localization contributions.

If you want to file a bug report or suggest a potential feature to add, please check out the [GitHub issues page](https://github.com/alastairlundy/CliInvoke/issues/) to see if a similar or identical issue is already open.
If there is not already a relevant issue filed, please [file one here](https://github.com/alastairlundy/CliInvoke/issues/new) and follow the respective guidance from the appropriate issue template.

Thanks.

## License
CliInvoke.Core is licensed under the MPL 2.0 license. If you modify any of CliInvoke.Core's files, then the modified files must be licensed under the MPL 2.0.

If you use CliInvoke.Core in your project, please make an exact copy of the contents of CliInvoke.Core's [LICENSE.txt file](https://github.com/alastairlundy/CliInvoke/blob/main/LICENSE.txt) available either in your third party licenses txt file or as a separate txt file.

## Acknowledgements

### Projects
This project would like to thank the following projects for their work:
* [Microsoft.Bcl.HashCode](https://github.com/dotnet/maintenance-packages) for providing a backport of the HashCode class and static methods to .NET Standard 2.0

For more information, please see the [THIRD_PARTY_NOTICES file](https://github.com/alastairlundy/CliInvoke.Cores/blob/main/THIRD_PARTY_NOTICES.txt).
