## CliInvoke.Extensions
This readme covers the **CliInvoke.Extensions** package. Looking for the [CliInvoke Readme](https://github.com/alastairlundy/CliInvoke/blob/main/README.md)?

This package adds the ``AddCliInvoke`` Dependency Injection extension method to enable easy CliInvoke setup when using the Microsoft.Extensions.DependencyInjection package 

<!-- Badges -->
[![Latest NuGet](https://img.shields.io/nuget/v/AlastairLundy.CliInvoke.Extensions.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Extensions/)
[![Latest Pre-release NuGet](https://img.shields.io/nuget/vpre/AlastairLundy.CliInvoke.Extensions.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Extensions/)
[![Downloads](https://img.shields.io/nuget/dt/AlastairLundy.CliInvoke.Extensions.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Extensions/)
![License](https://img.shields.io/github/license/alastairlundy/CliInvoke)

## Usage

### DependencyInjection
CliInvoke.Extensions provides an extension method to make it easier to use CliInvoke with Microsoft.Extensions.DependencyInjection.

The ``AddCliInvoke`` IServiceCollection extension method adds CliInvoke's and ProcessExtensions' interface-able services.

The services injected includes:
* ``IFilePathResolver``
* ``IProcessPipeHandler``
* ``IProcessInvoker``
* ``IProcessFactory``

## Why a separate package?
There's a few different reasons:
* Provides extension methods in a 
* Not everybody necessarily uses Microsoft's Dependency Injection packages.
* Helps de-couple the Dependency Injection extension functionality from the main library

## Usage Examples

### ``Cli`` helper class


## Licensing
CliInvoke.Extensions is licensed under the MPL 2.0 license.

If you use this package in your project please make an exact copy of the contents of the LICENSE.txt file available either in your third party licenses txt file or as a separate txt file.

## Acknowledgements
This project would like to thank the following projects for their work:
* [Microsoft.Extensions.DependencyInjection.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) for providing Dependency Injection Abstractions for .NET .

For more information, please see the [THIRD_PARTY_NOTICES file](https://github.com/alastairlundy/CliInvoke/blob/main/CliInvokeLibrary/CliInvoke.Extensions/THIRD_PARTY_NOTICES.txt).
