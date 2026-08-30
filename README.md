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
* [Middleware](#middleware)
* [Resource Disposal](#resource-disposal)
* [Documentation](#documentation)
* [Contributing to CliInvoke](#how-to-contribute-to-cliinvoke)
* [License](#license)
* [Acknowledgements](#acknowledgements)

## Features

* Clear separation of concerns between Process Configuration Builders, Process Configuration Models, and Invokers.
* Supports .NET 10 and has few dependencies.
* Has Dependency Injection extensions to make using it a breeze.
* Support for specific specializations such as running executables or commands via Windows PowerShell or CMD on
  Windows <sup>1</sup>
* [SourceLink](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink) support

<sup>1</sup> Specializations library distributed separately.

## Comparison vs Alternatives

CliInvoke is compared against [CliWrap](https://github.com/Tyrrrz/CliWrap/), [ProcessX](https://github.com/Cysharp/ProcessX), and the built-in .NET `Process` class across features like configuration separation, DI support, middleware, cross-platform support, and licensing.

See the [full comparison table](site/docs/comparison.md) for a detailed feature-by-feature breakdown.

## Installing CliInvoke

CliInvoke is available on [the NuGet Gallery](https://nuget.org) but can also be installed via the ``dotnet`` SDK CLI.

The package(s) to install depends on your use case:

* For use in a .NET library – Install the abstractions package, your developer users can install the Implementation and
  Dependency Injection packages.
* For use in a .NET app – Install the implementation package and the Dependency Injection Extensions Package

| Project type / Need                                                          | Packages to install (dotnet add package ...)                                      | Notes                                                                        |
|------------------------------------------------------------------------------|-----------------------------------------------------------------------------------|------------------------------------------------------------------------------|
| Library author (provide abstractions only)                                   | `CliInvoke.Core`                                                                  | Only the Core (abstractions) package — consumers can choose implementations. |
| Library or app that needs concrete builders / implementations                | `CliInvoke.Core`, `CliInvoke`                                                     | Implementation package plus Core for models/abstractions.                    |
| Desktop or Console application (common case — use DI & convenience helpers)  | `CliInvoke.Core`, `CliInvoke`, `CliInvoke.Extensions`                             | Includes DI registration and convenience extensions for easy setup, and some Middleware implementations.         |
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

## Middleware

CliInvoke's `ProcessInvoker` supports an optional **middleware** system that lets you plug cross-cutting concerns — logging, validation, platform selection, retries — around the process pipeline without changing how you call it. Middleware wraps the terminal pipeline in the order you register, and call sites (`ExecuteAsync`, `ExecuteBufferedAsync`, `ExecutePipedAsync`) remain identical.

Built-in middleware includes `UseLogging`, `UsePostExitValidation`, `UsePowerShell`, and `UseCmd`. Middleware can be configured by hand or through DI via the `IProcessMiddlewareBuilder` callback in `AddCliInvoke`.

For the full guide — constructor details, the `IProcessMiddleware` contract, DI configuration, result ownership, and the result-swap rule — see the **[Middleware Guide](site/docs/guides/middleware.md)**.

## Resource Disposal

> [!IMPORTANT]
> CliInvoke has exactly **four Resource-Owning Types** that implement `IDisposable` and **must** be disposed after use to avoid resource leaks (open pipe handles, kernel handles, and pinned `SecureString` buffers):
>
> | # | Type                    | What it owns                                                      |
> |---|-------------------------|-------------------------------------------------------------------|
> | 1 | `IExternalProcess`      | Underlying `System.Diagnostics.Process` (pipes, handles, threads) |
> | 2 | `PipedProcessResult`    | `StandardOutput` and `StandardError` streams                      |
> | 3 | `UserCredential`        | `SecureString` password buffer                                    |
> | 4 | `UserCredentialSpec` | `SecureString` password buffer staged for `Build()`               |
>
> No other CliInvoke type implements `IDisposable`. Always wrap these types in `using` or `await using` statements.
>
> `ProcessConfiguration` is a plain immutable value object and does **not** implement `IDisposable`. The `StandardInput` (`StreamWriter`) and `UserCredential` you place inside it remain **your** responsibility to dispose — CliInvoke never disposes them on your behalf.

For the full disposal reference — ownership rules, disposal patterns, and a checklist — see the **[Resource Disposal Guide](site/docs/guides/resource-disposal.md)**.

> [!NOTE]
> Middleware does not change these rules. A middleware chain returns the process result **un-disposed** to the caller, so the disposal contract described above applies exactly as it does without middleware. See **[Middleware](#middleware)** for the result-ownership note.

## Documentation

Full documentation is available in the [CliInvoke Developer Portal](site/docs/readme.md). Pick the path that fits you:

| Who you are                                                        | Start here                                                                                                                                                                                               |
|--------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Beginner** — "I just need to run a command"                      | [Quickstart](site/docs/getting-started-quickstart.md) → [Choosing your Invocation Pattern](site/docs/guides/choosing-invocation-pattern.md)                                                              |
| **Professional Developer** — "I'm building a testable app with DI" | [Getting Started](site/docs/getting-started.md) → [Configuration](site/docs/guides/configuration.md)                                                                                                     |
| **Power User** — "I need full lifecycle control"                   | [Choosing your Invocation Pattern → IExternalProcess](site/docs/guides/choosing-invocation-pattern.md#iexternalprocess--power-user-lifecycle-control) → [Architecture](site/docs/guides/architecture.md) |

> [!NOTE]
> **Upgrading to 3.0.0?** The `CliRun.UseExternalProcessFactory` / `CliRun.UseFilePathResolver`
> methods, the configurable `ExitConfiguration` setter, and several `ProcessInvoker` /
> `ExternalProcess` constructors were removed. `CliRun` is now a stateless
> batteries-included facade; callers needing a custom factory or resolver should use
> `IProcessInvoker` (or the DI container) instead. See the
> **[3.0.0 Migration Guide](site/docs/migration-guides/3.0.0.md)** and
> **[CHANGELOG.md](CHANGELOG.md)** for the full breaking-change list.

Other guides: [Troubleshooting](site/docs/guides/troubleshooting.md) · [Migration Guides](site/docs/migration-guides/readme.md) · [Building from Source](site/docs/building-cliinvoke.md)

## How to Build CliInvoke's code

Please see [building-cliinvoke.md](site/docs/building-cliinvoke.md) for how to build CliInvoke from source.

## How to Contribute to CliInvoke

Please see the [CONTRIBUTING.md file](CONTRIBUTING.md) for code and localisation contributions.

If you want to file a bug report or suggest a potential feature to add, please check out
the [GitHub issues page](https://github.com/alastairlundy/CliInvoke/issues/) to see if a similar or identical issue is
already open.
If there isn't already a relevant issue filed,
please [file one here](https://github.com/alastairlundy/CliInvoke/issues/new) and follow the respective guidance from
the appropriate issue template.

## License

CliInvoke is licensed under the MPL 2.0 license. You can learn more about it [here](https://www.mozilla.org/en-US/MPL/)

Should your project incorporate CliInvoke, ensure that the full text of CliInvoke's LICENSE.txt is either incorporated
into your third-party licenses TXT file or provided as a distinct TXT file within your project's repository.

### CliInvoke Assets

The CliInvoke icon is a separately-owned asset and is **not** licensed under MPL-2.0 like
the rest of the codebase.

If you fork CliInvoke and re-distribute it, please replace the icon with your own artwork
unless you have written permission from the maintainer. To request permission, open a
[GitHub issue](https://github.com/alastairlundy/CliInvoke/issues/new) tagged `asset-license`.

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
