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
* [Used By](#used-by)
* [Roadmap](#cliinvokes-roadmap)
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

| Feature / Criterion                                                        |  CliInvoke   |                                  [CliWrap](https://github.com/Tyrrrz/CliWrap/)                                   |    [ProcessX](https://github.com/Cysharp/ProcessX)     |                             .NET Process class                             |
|----------------------------------------------------------------------------|:------------:|:----------------------------------------------------------------------------------------------------------------:|:------------------------------------------------------:|:--------------------------------------------------------------------------:|
| Dedicated builder, model, and invoker types (clear separation of concerns) |      ✅      |                                                        ❌                                                        |                           ❌                           | ⚠️, offers limited separation of concerns via ProcessStartInfo model class |
| Dependency Injection registration extensions                               |      ✅      |                                                        ❌                                                        |                           ❌                           |                                     ❌                                     |
| Installable via NuGet                                                      |      ✅      |                                                        ✅                                                        |                           ✅                           |                            ✅ , Built into .NET                            |
| Official cross‑platform support (advertised: Windows/macOS/Linux/BSD)      |      ✅      |                                                       ✅*                                                        |                          ❌*                           |                                     ✅                                     |  
| Buffered and non‑buffered execution modes                                  |      ✅      |                                                        ✅                                                        |                           ✅                           |           ⚠️, can lead to deadlocks or exceptions if not careful           |
| Support for Process/Command Timeout                                        |      ✅      |                              :warning:, limited to cancelling via CancellationToken                              | :warning:, limited to cancelling via CancellationToken |           :warning:, limited to cancelling via CancellationToken           |
| Graceful Cancellation Support via SIGTERM/SIGINT Signals                   |  ✅, 2.3.0+  |                                                        ✅                                                        |                           ❌                           |                                     ❌                                     |
| Small surface area and minimal dependencies                                |      ✅      |                                                        ✅                                                        |                           ✅                           |                                     ✅                                     |  
| Licensing / repository additional terms                                    | ✅ (MPL‑2.0) | ⚠️ (MIT; test project references a source‑available library; repo contains an informal "Terms of Use" statement) |                        ✅ (MIT)                        |                    ✅ (.NET Runtime licensed under MIT)                    |

Notes:

- *Indicates not explicitly advertised for all listed OSes but may work in practice; check each project's docs.
- The CliWrap repository includes a test project that references a source‑available (non‑open source) library; that
  library is used for tests and is not distributed with the runtime package. The repo also contains an informal "Terms
  of Use" statement — review repository files if legal certainty is required.

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

## Middleware

CliInvoke's `ProcessInvoker` supports an optional **middleware** system that lets you plug cross-cutting concerns (logging, validation, platform selection, retries, …) around the terminal process pipeline without changing how you call it. The pipeline remains the "leaf" that actually starts and waits on the process; middleware wraps it in the order you register.

### When to use middleware, and the two constructors

`ProcessInvoker` has two constructors:

```csharp
// 1. No middleware — the classic, unchanged behavior.
public ProcessInvoker(IExternalProcessFactory externalProcessFactory);

// 2. With middleware — every invocation runs through the chain, in order,
//    before the terminal pipeline executes.
public ProcessInvoker(
    IExternalProcessFactory externalProcessFactory,
    IEnumerable<IProcessMiddleware> middlewares);
```

Each constructor has an overload that also accepts a `MiddlewareItems? sharedItems` parameter to seed the per-chain item bag with pre-injected services (such as an `ILogger`). This is how middleware like `LoggingMiddleware` receives a logger at runtime:

```csharp
using CliInvoke.Core.Middleware; // MiddlewareItems

var items = new MiddlewareItems();
items.Set("Logger", myLogger);
var invoker = new ProcessInvoker(factory, items).UseLogging();
```

Use the first constructor when you don't need middleware. Use the second (or one of the `Use…` extension methods below) when you want logging, validation, or platform wrapping applied to every invocation. Call sites are identical either way: `ExecuteAsync`, `ExecuteBufferedAsync`, and `ExecutePipedAsync` are unchanged.

### The `IProcessMiddleware` contract

A middleware is any `IProcessMiddleware` implementation. It receives the `InvocationContext` and a `next` delegate; calling `next` continues the chain (or the terminal pipeline), omitting it short-circuits:

```csharp
public interface IProcessMiddleware
{
    Task InvokeAsync(
        InvocationContext context,
        Func<InvocationContext, CancellationToken, Task> next);
}
```

Middleware read and share data through `InvocationContext.Middleware.Items` (a typed `MiddlewareItems` bag). For example, `LoggingMiddleware` resolves an `ILogger` from that bag under the well-known key `"Logger"`.

### Built-in middleware

The public API is the **extension methods**, not the middleware classes (which are internal). All of them return a *new* `ProcessInvoker`, so they compose fluently:

```csharp
using CliInvoke;                       // ProcessInvoker
using CliInvoke.Extensions.Middleware;            // UseLogging
using CliInvoke.Extensions.Middleware.Validation; // UsePostExitValidation
using CliInvoke.Specializations.Middleware;        // UsePowerShell, UseCmd

// Log entry/exit (and each stdout/stderr line at Debug) for every invocation:
ProcessInvoker loggingInvoker = new ProcessInvoker(factory).UseLogging();

// Validate the result after exit (throws ProcessValidationException on failure):
ProcessInvoker validatedInvoker = new ProcessInvoker(factory)
    .UsePostExitValidation(PostExitValidation.ExitCodeIsZero());

// Run the command inside PowerShell Core / Windows cmd.exe:
ProcessInvoker psInvoker = new ProcessInvoker(factory).UsePowerShell();
ProcessInvoker cmdInvoker = new ProcessInvoker(factory).UseCmd();
```

* `UseLogging` — logs process entry and exit at `Information`, and each captured stdout/stderr line at `Debug` (when using `BufferedProcessResult`). Sensitive flags (`--password`, `--token`, `--api-key`) are redacted automatically. If no `ILogger` is supplied via the middleware items, a no-op logger is used.
* `UsePostExitValidation(validator)` — runs a validator built from CliInvoke's `CommonValidationRules` against the `ProcessResult` and throws `ProcessValidationException` (with a per-rule failure message) when it fails. Helpers: `PostExitValidation.ExitCodeIsZero()`, `ExitCodeIs(code)`, `ExitCodeIsOneOf(codes...)`, `StdoutMatches(regex)`, `StderrIsEmpty()`.
* `UsePowerShell` / `UseCmd` — rewrite the configuration so the original command executes inside `pwsh` (or `pwsh.exe` on Windows) using `-NoProfile -NonInteractive -Command`, or inside `cmd.exe` using `/c`. `UsePowerShell` also has an overload `UsePowerShell(windowCreation, useShellExecution)` for non-default behaviour; the parameterless form defaults both to `false`, matching the unified defaults used by `PowershellProcessInvoker`, `PowerShellMiddleware` and `ProcessConfiguration`. `UseCmd` is Windows-only and throws `PlatformNotSupportedException` on other platforms; the platform-restricted behaviour mirrors `CmdProcessInvoker`.

### Result-ownership and disposal through the chain

Middleware does **not** dispose the process result — the result is returned to you un-disposed, exactly as with a non-middleware invoker. You remain responsible for disposing `PipedProcessResult` (and its streams) and the `ProcessConfiguration` you created. See **[Resource Disposal](#resource-disposal)** for the full ownership rules and checklist.

### The result-swap rule

By default, middleware does **not** mutate the `ProcessResult` object. Logging and post-exit validation pass the result through unchanged. Platform-selection middleware (`UsePowerShell` / `UseCmd`) substitutes the result of the wrapped `pwsh` / `cmd.exe` invocation — the caller still sees a normal `ProcessResult`, but the data comes from the wrapped shell, not from the original command. Transforming or replacing the result is a deliberate, niche operation: a middleware that does so should write the new result onto `InvocationContext.Result` so the caller receives it.

## Resource Disposal

> [!IMPORTANT]
> CliInvoke has exactly **five Resource-Owning Types** that implement `IDisposable` and **must** be disposed after use to avoid resource leaks (open pipe handles, kernel handles, and pinned `SecureString` buffers):
>
> | # | Type                    | What it owns                                                      |
> |---|-------------------------|-------------------------------------------------------------------|
> | 1 | `ProcessConfiguration`  | `StreamWriter` (StandardInput), optional `UserCredential`         |
> | 2 | `IExternalProcess`      | Underlying `System.Diagnostics.Process` (pipes, handles, threads) |
> | 3 | `PipedProcessResult`    | `StandardOutput` and `StandardError` streams                      |
> | 4 | `UserCredential`        | `SecureString` password buffer                                    |
> | 5 | `UserCredentialSpec` | `SecureString` password buffer staged for `Build()`               |
>
> No other CliInvoke type implements `IDisposable`. Always wrap these types in `using` or `await using` statements.

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

## Used By

CliInvoke is used by these projects:

Want your project added to this list? [Open an issue](https://github.com/alastairlundy/cliinvoke/issues/new/)

## CliInvoke's Roadmap

CliInvoke aims to make working with Commands and external processes easier.

Future updates may focus on one or more of the following:

* Improved ease of use
* Improved stability
* New features
* Enhancing existing features

## New vs Old Package and Namespace

CliInvoke changed its NuGet package ID and namespace starting from the re-release of 2.0.0 (tagged as 2.0.0-v2) and has
since been published directly under the ``CliInvoke`` package ID prefix and namespace.

The previous package IDs are marked as deprecated and will not receive future updates.

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
