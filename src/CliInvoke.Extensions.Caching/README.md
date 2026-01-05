## CliInvoke.Extensions.Caching
This readme covers the **CliInvoke.Extensions.Caching** package. Looking for the [CliInvoke Readme](https://github.com/alastairlundy/CliInvoke/blob/main/README.md)?

This package adds the ``UseCachedFilePathResolver`` Dependency Injection extension method to enable easy CliInvoke Caching setup when using the Microsoft.Extensions.DependencyInjection package.

<!-- Badges -->
[![Latest NuGet](https://img.shields.io/nuget/v/CliInvoke.Extensions.Caching.svg)](https://www.nuget.org/packages/CliInvoke.Extensions.Caching/)
[![Latest Pre-release NuGet](https://img.shields.io/nuget/vpre/CliInvoke.Extensions.Caching.svg)](https://www.nuget.org/packages/CliInvoke.Extensions.Caching/)
[![Downloads](https://img.shields.io/nuget/dt/CliInvoke.Extensions.Caching.svg)](https://www.nuget.org/packages/CliInvoke.Extensions.Caching/)
![License](https://img.shields.io/github/license/alastairlundy/CliInvoke)

## Usage

### DependencyInjection
CliInvoke.Extensions.Caching provides extension methods to make it easier to use Caching with CliInvoke.

The methods added include:
* ``UseCachedFilePathResolver`` extension method to set up CliInvoke's interfaces and implementing classes with Dependency Injection.

The services injected include:
* ``IFilePathResolver`` - Used by ``IProcessInvoker`` to resolve the file path of a ``ProcessConfiguration``'s ``TargetFilePath`` string.

## Why a separate package?
Not everybody necessarily may want or need Caching support for CliInvoke

## Usage Examples

## Licensing
CliInvoke.Extensions.Caching is licensed under the MPL 2.0 license.

If you use this package in your project, please make an exact copy of the LICENSE.txt file available either in your third party licenses txt file or as a separate txt file.

## Acknowledgements
This project would like to thank the following projects for their work:
* [Microsoft.Extensions.DependencyInjection.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) for providing Dependency Injection Abstractions for .NET.

For more information, please see the [THIRD_PARTY_NOTICES file](https://github.com/alastairlundy/CliInvoke/blob/main/THIRD_PARTY_NOTICES.txt).
