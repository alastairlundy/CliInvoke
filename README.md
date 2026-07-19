# CliInvoke

<!-- Badges -->
[![Latest NuGet](https://img.shields.io/nuget/v/CliInvoke?style=flat-square&label=Latest%20Stable%20Release)](https://www.nuget.org/packages/CliInvoke/)
[![Latest Pre-release NuGet](https://img.shields.io/nuget/vpre/CliInvoke?style=flat-square&label=Latest%20Pre-Release)](https://www.nuget.org/packages/CliInvoke/)
[![Downloads](https://img.shields.io/nuget/dt/CliInvoke?style=flat-square)](https://www.nuget.org/packages/CliInvoke/)
[![GitHub License](https://img.shields.io/github/license/alastairlundy/CliInvoke?style=flat-square)](https://github.com/alastairlundy/CliInvoke/blob/main/LICENSE.txt)
![OpenSSF Scorecard Score](https://img.shields.io/ossf-scorecard/github.com/alastairlundy/CliInvoke?style=flat-square&label=OpenSSF%20Scorecard%20Score)

<img src="https://github.com/alastairlundy/CliInvoke/blob/main/.assets/icon.png" width="192" height="192" alt="CliInvoke Logo">

CliInvoke is a .NET library for interacting with Command Line Interfaces and wrapping around executables.

Launch processes, redirect standard input and output streams, await process completion, and much more.

## Table of Contents

* [Features](#features)
* [Comparison vs Alternatives](#comparison-vs-alternatives)
* [Installing CliInvoke](#installing-cliinvoke)
    * [Supported Platforms](#supported-platforms)
* [Examples](#examples)
* [Resource Disposal](#resource-disposal)
* [Documentation](#documentation)
* [Contributing to CliInvoke](#how-to-contribute-to-cliinvoke)
* [Used By](#used-by)
* [Roadmap](#cliinvokes-roadmap)
* [License](#license)
* [Acknowledgements](#acknowledgements)

## Features

* Clear separation of concerns between Process Configuration Builders, Process Configuration Models, and Invokers.
* Supports .NET 8 and newer TFMs and has few dependencies.
* Has Dependency Injection extensions to make using it a breeze.
* Support for specific specializations such as running executables or commands via Windows PowerShell or CMD on
  Windows <sup>1</sup>
* [SourceLink](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink) support

<sup>1</sup> Specializations library distributed separately.

## Comparison vs Alternatives

| Feature / Criterion                                                        |  CliInvoke  |                                  [CliWrap](https://github.com/Tyrrrz/CliWrap/)                                   |    [ProcessX](https://github.com/Cysharp/ProcessX)     |                             .NET Process class                             |
|----------------------------------------------------------------------------|:-----------:|:----------------------------------------------------------------------------------------------------------------:|:------------------------------------------------------:|:--------------------------------------------------------------------------:|
| Dedicated builder, model, and invoker types (clear separation of concerns) |      ✅      |                                                        ❌                                                         |                           ❌                            | ⚠️, offers limited separation of concerns via ProcessStartInfo model class |
| Dependency Injection registration extensions                               |      ✅      |                                                        ❌                                                         |                           ❌                            |                                     ❌                                      |
| Installable via NuGet                                                      |      ✅      |                                                        ✅                                                         |                           ✅                            |                            ✅ , Built into .NET                             |
| Official cross‑platform support (advertised: Windows/macOS/Linux/BSD)      |      ✅      |                                                        ✅*                                                        |                           ❌*                           |                                     ✅                                      |  
| Buffered and non‑buffered execution modes                                  |      ✅      |                                                        ✅                                                         |                           ✅                            |           ⚠️, can lead to deadlocks or exceptions if not careful           |
| Support for Process/Command Timeout                                        |      ✅      |                              :warning:, limited to cancelling via CancellationToken                              | :warning:, limited to cancelling via CancellationToken |           :warning:, limited to cancelling via CancellationToken           |
| Graceful Cancellation Support via SIGTERM/SIGINT Signals                   |  ✅, 2.3.0+  |                                                        ✅                                                         |                           ❌                            |                                     ❌                                      |
| Small surface area and minimal dependencies                                |      ✅      |                                                        ✅                                                         |                           ✅                            |                                     ✅                                      |  
| Licensing / repository additional terms                                    | ✅ (MPL‑2.0) | ⚠️ (MIT; test project references a source‑available library; repo contains an informal "Terms of Use" statement) |                        ✅ (MIT)                         |                    ✅ (.NET Runtime licensed under MIT)                     |

Notes:

- *Indicates not explicitly advertised for all listed OSes but may work in practice; check each project's docs.
- The CliWrap repository includes a test project that references a source‑available (non‑open source) library; that
  library is used for tests and is not distributed with the runtime package. The repo also contains an informal "Terms
  of Use" statement — review repository files if legal certainty is required.

## Installing CliInvoke

CliInvoke is available on [the NuGet Gallery](https://nuget.org) but call be also installed via the ``dotnet`` SDK CLI.

The package(s) to install depends on your use case:

* For use in a .NET library – Install the abstractions package, your developer users can install the Implementation and
  Dependency Injection packages.
* For use in a .NET app – Install the implementation package and the Dependency Injection Extensions Package

| Project type / Need                                                          | Packages to install (dotnet add package ...)                                      | Notes                                                                        |
|------------------------------------------------------------------------------|-----------------------------------------------------------------------------------|------------------------------------------------------------------------------|
| Library author (provide abstractions only)                                   | `CliInvoke.Core`                                                                  | Only the Core (abstractions) package — consumers can choose implementations. |
| Library or app that needs concrete builders / implementations                | `CliInvoke.Core`, `CliInvoke`                                                     | Implementation package plus Core for models/abstractions.                    |
| Desktop or Console application (common case — use DI & convenience helpers)  | `CliInvoke.Core`, `CliInvoke`, `CliInvoke.Extensions`                             | Includes DI registration and convenience extensions for easy setup.          |
| Any project that needs platform‑specific or shell specializations (optional) | `CliInvoke.Specializations` (install in addition to the packages above as needed) | Adds Cmd/PowerShell and other specializations; include only when required.   |

### Links to packages

[CliInvoke.Core Nuget](https://nuget.org/packages/CliInvoke.Core)
[CliInvoke Nuget](https://nuget.org/packages/CliInvoke)
[CliInvoke.Extensions Nuget](https://nuget.org/packages/CliInvoke.Extensions)
[CliInvoke.Specializations Nuget](https://nuget.org/packages/CliInvoke.Specializations)

## Supported Platforms

CliInvoke supports Windows, macOS, Linux, FreeBSD, Android, and potentially some other operating systems.

For more details see the [list of supported platforms](site/docs/Supported-OperatingSystems.md)

## Design Patterns & When to Use Them

CliInvoke provides three distinct design patterns for invoking processes. See [PATTERNS.md](PATTERNS.md) for comprehensive documentation on each pattern.

* **`CliRun`** – Beginner-friendly/quickstart entrypoint. Use for basic scripting, CI/CD tasks, or simple command execution. Zero boilerplate, optional arguments with sensible defaults.
* **`IProcessInvoker`** – DI-centric pattern and support for end-to-end process management. Use when building applications that need testability, dependency injection integration, or custom process configuration per invocation.
* **`IExternalProcess` & `IExternalProcessFactory`** – Process-like API with DI support, rich capability, stable and predictable behaviour. Use when you need granular lifecycle control, manual start/stop sequences, or power-user scenarios similar to `System.Diagnostics.Process`.

## Examples

### Beginner Friendly / Quickstart

For simple use cases, the `CliRun` helper provides a straightforward API to execute commands with minimal boilerplate:

```csharp
using CliInvoke;
using CliInvoke.Core;

// Execute a command and get the result
ProcessResult result = await CliRun.RunAsync("dotnet", "--version");
Console.WriteLine($"Exit Code: {result.ExitCode}");
```

For capturing output, use `RunBufferedAsync`:

```csharp
using CliInvoke;
using CliInvoke.Core;

// Execute and capture stdout/stderr
BufferedProcessResult result = await CliRun.RunBufferedAsync("dotnet", "--info");
Console.WriteLine(result.StandardOutput);
Console.WriteLine(result.StandardError);
```

`CliRun` is ideal for scripting, quick prototypes, and basic command execution where you don't need dependency injection or advanced configuration.

For detailed documentation on all available patterns and when to use them, see [PATTERNS.md](PATTERNS.md).

### Advanced Configuration

For fine-grained control over process execution — custom timeouts, cancellation strategies, buffered vs. non-buffered output, and builder-based configuration — see the **[Configuration Guide](site/docs/guides/configuration.md)** and the **[Choosing your Invocation Pattern](site/docs/guides/choosing-invocation-pattern.md)** guide in the documentation portal.

## Resource Disposal

> [!IMPORTANT]
> CliInvoke has exactly **five Resource-Owning Types** that implement `IDisposable` and **must** be disposed after use to avoid resource leaks (open pipe handles, kernel handles, and pinned `SecureString` buffers):
>
> | # | Type | What it owns |
> |---|------|-------------|
> | 1 | `ProcessConfiguration` | `StreamWriter` (StandardInput), optional `UserCredential` |
> | 2 | `IExternalProcess` | Underlying `System.Diagnostics.Process` (pipes, handles, threads) |
> | 3 | `PipedProcessResult` | `StandardOutput` and `StandardError` streams |
> | 4 | `UserCredential` | `SecureString` password buffer |
> | 5 | `UserCredentialBuilder` | `SecureString` password buffer staged for `Build()` |
>
> No other CliInvoke type implements `IDisposable`. Always wrap these types in `using` or `await using` statements.

For the full disposal reference — ownership rules, disposal patterns, and a checklist — see the **[Resource Disposal Guide](site/docs/guides/resource-disposal.md)**.

## Documentation

Full documentation is available in the [CliInvoke Developer Portal](site/docs/readme.md). Pick the path that fits you:

| Who you are | Start here |
|---|---|
| **Beginner** — "I just need to run a command" | [Quickstart](site/docs/getting-started-quickstart.md) → [Choosing your Invocation Pattern](site/docs/guides/choosing-invocation-pattern.md) |
| **Professional Developer** — "I'm building a testable app with DI" | [Getting Started](site/docs/getting-started.md) → [Configuration](site/docs/guides/configuration.md) |
| **Power User** — "I need full lifecycle control" | [Choosing your Invocation Pattern → IExternalProcess](site/docs/guides/choosing-invocation-pattern.md#iexternalprocess--power-user-lifecycle-control) → [Architecture](site/docs/guides/architecture.md) |

Other guides: [Troubleshooting](site/docs/guides/troubleshooting.md) · [Migration Guides](site/docs/migration-guides/readme.md) · [Building from Source](site/docs/building-cliinvoke.md)

## How to Build CliInvoke's code

Please see [building-cliinvoke.md](site/docs/building-cliinvoke.md) for how to build CliInvoke from source.

## How to Contribute to CliInvoke

Please see the [CONTRIBUTING.md file](CONTRIBUTING.md) for code and localization contributions.

If you want to file a bug report or suggest a potential feature to add, please check out
the [GitHub issues page](https://github.com/alastairlundy/CliInvoke/issues/) to see if a similar or identical issue is
already open.
If there isn't already a relevant issue filed,
please [file one here](https://github.com/alastairlundy/CliInvoke/issues/new) and follow the respective guidance from
the appropriate issue template.

## Used By

CliInvoke is used by these projects:

* [WCountLib.Providers.wc](https://github.com/alastairlundy/WCount/tree/main/src/lib/WCountLib.Providers.wc) –
  Implements WCountLib.Abstractions using the Unix ``wc`` command.

Want your project added to this list? [Open an issue](https://github.com/alastairlundy/cliinvoke/issues/new/)

## CliInvoke's Roadmap

CliInvoke aims to make working with Commands and external processes easier.

Whilst there is a modest set of features are available today, there is room for more features and for modifications of
existing features in future updates.

Future updates may focus on one or more of the following:

* Improved ease of use
* Improved stability
* New features
* Enhancing existing features

## New vs Old Package and Namespace

CliInvoke changed it's Nuget package Id and namespace starting from the re-release of 2.0.0 (tagged as 2.0.0-v2) and has
since been published directly under the ``CliInvoke`` package ID prefix and namespace.

The previous packages Ids are marked as deprecated and will not receive future updates.

## License

CliInvoke is licensed under the MPL 2.0 license. You can learn more about it [here](https://www.mozilla.org/en-US/MPL/)

Should your project incorporate CliInvoke, ensure that the full text of CliInvoke's LICENSE.txt is either incorporated
into your third-party licenses TXT file or provided as a distinct TXT file within your project's repository.

### CliInvoke Assets

CliInvoke's Icon is owned by and has all rights reserved to me (Alastair Lundy).

If you fork CliInvoke and re-distribute it, please replace the icon unless you have prior written approval from me.

## Star History

<a href="https://www.star-history.com/?repos=alastairlundy%2Fcliinvoke&type=date&logscale=&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=alastairlundy/cliinvoke&type=date&theme=dark&legend=top-left" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=alastairlundy/cliinvoke&type=date&legend=top-left" />
   <img alt="Star History Chart" src="https://api.star-history.com/chart?repos=alastairlundy/cliinvoke&type=date&legend=top-left" />
 </picture>
</a>

## Acknowledgements

### Projects

This project would like to thank the following projects for their work:

* [CliWrap](https://github.com/Tyrrrz/CliWrap/) for inspiring this project
* [Polyfill](https://github.com/SimonCropp/Polyfill) for simplifying older TFM support

For more information, please see
the [THIRD_PARTY_NOTICES file](https://github.com/alastairlundy/CliInvoke/blob/main/THIRD_PARTY_NOTICES.txt).
