## CliInvoke.Extensions
This readme covers the **CliInvoke.Extensions** package. Looking for the [CliInvoke Readme](https://github.com/alastairlundy/CliInvoke/blob/main/README.md)?

This package adds the ``AddCliInvoke`` Dependency Injection extension method to enable easy CliInvoke setup when using the Microsoft.Extensions.DependencyInjection package 

<!-- Badges -->
[![Latest NuGet](https://img.shields.io/nuget/v/CliInvoke.Extensions.svg)](https://www.nuget.org/packages/CliInvoke.Extensions/)
[![Latest Pre-release NuGet](https://img.shields.io/nuget/vpre/CliInvoke.Extensions.svg)](https://www.nuget.org/packages/CliInvoke.Extensions/)
[![Downloads](https://img.shields.io/nuget/dt/CliInvoke.Extensions.svg)](https://www.nuget.org/packages/CliInvoke.Extensions/)
![License](https://img.shields.io/github/license/alastairlundy/CliInvoke)

## Usage

### DependencyInjection
CliInvoke.Extensions provides extension methods to make it easier to use CliInvoke with Microsoft.Extensions.DependencyInjection.

The methods added include:
* ``AddCliInvoke`` extension method to set up CliInvoke's interfaces and implementing classes with Dependency Injection.
* ``AddDefaultRunnerProcessInvoker`` extension method to set up CliInvoke's ``DefaultRunnerProcessInvoker`` and abstract class ``RunnerProcessInvokerBase`` with Dependency Injection - Only required for running Process Configurations through other Process Configurations.
* ``AddDerivedRunnerProcessInvoker`` extension method to set up a class that derives from the abstract class ``RunnerProcessInvokerBase`` along with the abstract class itself with Dependency Injection - Only required for running Process Configurations through other Process Configurations where a custom implementation of ``RunnerProcessInvokerBase`` is desired.

The services injected include:
* ``IFilePathResolver`` - Used by ``IProcessInvoker`` to resolve the file path of a ``ProcessConfiguration``'s ``TargetFilePath`` string.
* ``IProcessPipeHandler`` - Used by ``IProcessInvoker`` to pipe Standard Input, Output, and Error Streams.
* ``IProcessInvoker`` - Runs a process based on a ``ProcessConfiguration``
* ``IProcessConfigurationFactory`` - Enables easy ``ProcessConfiguration`` creation.

## Why a separate package?
There's a few different reasons:
* Provides extension methods in a 
* Not everybody necessarily uses Microsoft's Dependency Injection packages.
* Helps de-couple the Dependency Injection extension functionality from the main library

## Usage Examples

### Dependency Injection

## Licensing
CliInvoke.Extensions is licensed under the MPL 2.0 license.

If you use this package in your project, please make an exact copy of the LICENSE.txt file available either in your third party licenses txt file or as a separate txt file.

## Acknowledgements
This project would like to thank the following projects for their work:
* [Microsoft.Extensions.DependencyInjection.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) for providing Dependency Injection Abstractions for .NET.

For more information, please see the [THIRD_PARTY_NOTICES file](https://github.com/alastairlundy/CliInvoke/blob/main/CliInvokeLibrary/CliInvoke.Extensions/THIRD_PARTY_NOTICES.txt).
