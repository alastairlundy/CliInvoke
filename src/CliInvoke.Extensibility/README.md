## CliInvoke.Extensibility
This readme covers the **CliInvoke.Extensibility** package. Looking for the [CliInvoke Readme](https://github.com/alastairlundy/CliInvoke/blob/main/README.md)?

This package adds:
* abstractions to make extending CliInvoke, and it's supported types easier.

[![NuGet](https://img.shields.io/nuget/v/AlastairLundy.CliInvoke.Extensibility.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Extensibility/)
[![NuGet](https://img.shields.io/nuget/dt/AlastairLundy.CliInvoke.Extensibility.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Extensibility/)

## Usage

### SpecializedCommandConfiguration
CliInvoke.Extensibility provides an abstraction to make it easier to create custom Command Configurations.

This abstraction comes in the form of ``SpecializedCliCommandConfiguration``.

## Why a separate package?
There's a few different reasons:
* De-couples the extensibility related abstractions from the main library
* Not every user of CliInvoke will necessarily use or need CliInvoke Specializations or other extensibility powered features.

## Licensing
CliInvoke.Extensibility is licensed under the MPL 2.0 license. If you modify any of the package's files then the modified files must be licensed under the MPL 2.0 .

If you use this package in your project please make an exact copy of the contents of the LICENSE.txt file available either in your third party licenses txt file or as a separate txt file.

For more information, please see the [THIRD_PARTY_NOTICES file](https://github.com/alastairlundy/CliInvoke/blob/main/CliInvokeLibrary/CliInvoke.Extensibility/THIRD_PARTY_NOTICES.txt).
