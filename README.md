# CliInvoke

<!-- Badges -->
[![Latest NuGet](https://img.shields.io/nuget/v/CliInvoke.svg?style=flat-square&label=Latest%20Stable%20Release)](https://www.nuget.org/packages/CliInvoke/)
[![Latest Pre-release NuGet](https://img.shields.io/nuget/vpre/CliInvoke.svg?style=flat-square&label=Latest%20Pre-Release)](https://www.nuget.org/packages/CliInvoke/)
[![Downloads](https://img.shields.io/nuget/dt/CliInvoke.svg?style=flat-square)](https://www.nuget.org/packages/CliInvoke/)
[![GitHub License](https://img.shields.io/github/license/alastairlundy/CliInvoke?style=flat-square&color=6a0dad)](https://github.com/alastairlundy/CliInvoke/blob/main/LICENSE.txt)
![OpenSSF Scorecard Score](https://img.shields.io/ossf-scorecard/github.com/alastairlundy/CliInvoke?style=flat-square&label=OpenSSF%20Scorecard%20Score)

<img src="https://github.com/alastairlundy/CliInvoke/blob/main/.assets/icon.png" width="192" height="192" alt="CliInvoke Logo">

CliInvoke is a .NET library for interacting with Command Line Interfaces and wrapping around executables.

Launch processes, redirect standard input and output streams, await process completion, and much more.

## Table of Contents

* [Features](#features)
* [Comparison vs Alternatives](#comparison-vs-alternatives)
* [Installing CliInvoke](#installing-cliinvoke)
    * [Supported Platforms](#supported-platforms)
* [CliInvoke Examples](#examples)
* [Contributing to CliInvoke](#how-to-contribute-to-cliinvoke)
* [Used By](#used-by)
* [Roadmap](#cliinvokes-roadmap)
* [License](#license)
* [Acknowledgements](#acknowledgements)

## Features

* Clear separation of concerns between Process Configuration Builders, Process Configuration Models, and Invokers.
* Supports .NET Standard 2.0, .NET 8, and newer TFMs, and has few dependencies.
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

For more details see the [list of supported platforms](docs/docs/Supported-OperatingSystems.md)

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

### Advanced Configuration with Builders
CliInvoke features several builder interfaces with advanced features intended for developers needing fine‑grained control of process configuration. For most common scenarios creating a ``ProcessConfiguration`` directly or using the static ``ProcessConfigurationFactory`` is sufficient.

These examples use ``IProcessInvoker`` but the ``IExternalProcess`` and ``IExternalProcessFactory`` pattern can be used instead as they both work with ``ProcessConfiguration`` objects.

The following examples show how to configure and build a ``ProcessConfiguration`` depending on whether Buffering the
output is desired.

#### Non-Buffered Execution Example

This example gets a non buffered ``ProcessResult`` that contains basic process exit code, id, and other information.

```csharp
using CliInvoke;
using CliInvoke.Core;

using CliInvoke.Builders;
using CliInvoke.Core.Builders;

using Microsoft.Extensions.DependencyInjection;

  //Namespace and class code ommitted for clarity 

  // ServiceProvider and Dependency Injection setup code oomittedfor clarity
  
  IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();

  // Fluently configure your Command.
  IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("Path/To/Executable")
                            .SetArguments(["arg1", "arg2"])
                            .SetWorkingDirectory("/Path/To/Directory");
  
  // Build it as a ProcessConfiguration object when you're ready to use it.
  ProcessConfiguration config = builder.Build();
  
  // Execute the process through ProcessInvoker and get the results.
ProcessResult result = await _processConfigInvoker.ExecuteAsync(config);
```

#### Buffered Execution Example

This example gets a ``BufferedProcessResult`` which contains redirected StandardOutput and StandardError as strings.

```csharp
using CliInvoke;
using CliInvoke.Builders;

using CliInvoke.Core;
using CliInvoke.Core.Builders;

using Microsoft.Extensions.DependencyInjection;


  //Namespace and class code ommitted for clarity
  // ServiceProvider and Dependency Injection setup code ommitted for clarity
  
  IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();

  // Fluently configure your Command.
  IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("Path/To/Executable")
                            .SetArguments(["arg1", "arg2"])
                            .SetWorkingDirectory("/Path/To/Directory")
                            .RedirectStandardOutput(true)
                           .RedirectStandardError(true);
  
  // Build it as a ProcessConfiguration object when you're ready to use it.
  ProcessConfiguration config = builder.Build();
  
  // Execute the process through ProcessInvoker and get the results.
BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(config);
```

### Cancellation and Timeout
CliInvoke provides flexible timeout and cancellation support for process execution. By default, processes have a **10-minute timeout** with graceful exit behaviour.

#### Default Timeout Policy

The default timeout policy is used without explicit configuration:

```csharp
using CliInvoke;
using CliInvoke.Core;

// Uses the default timeout with graceful exit
ProcessResult result = await CliRun.RunAsync("dotnet", "build");
```

#### Custom Timeout Configuration

You can customize the timeout by creating a `ProcessExitConfiguration` with a `ProcessTimeoutPolicy`:

```csharp
using CliInvoke;
using CliInvoke.Core;

// Create a custom timeout policy (5-minute timeout with graceful cancellation)
ProcessTimeoutPolicy customTimeout = ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromMinutes(5), ProcessCancellationMode.Graceful);
ProcessExitConfiguration exitConfig = new ProcessExitConfiguration(customTimeout);

IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("dotnet")
    .SetArguments(["build"]);

ProcessConfiguration config = builder.Build();

// Execute with custom timeout
IProcessInvoker invoker = new ProcessInvoker(FilePathResolver.Shared);
ProcessResult result = await invoker.ExecuteAsync(config, exitConfig);
```

#### Disable Timeout

To disable the timeout entirely, use `ProcessTimeoutPolicy.None`:

```csharp
ProcessExitConfiguration noTimeout = new ProcessExitConfiguration(ProcessTimeoutPolicy.None);
// Process will wait indefinitely for completion
```

#### Cancellation Support
Cancel process execution using a `CancellationToken`:

```csharp
using CliInvoke;
using CliInvoke.Core;
using System.Threading;

CancellationTokenSource cts = new CancellationTokenSource();

// Cancel after 30 seconds
cts.CancelAfter(TimeSpan.FromSeconds(30));

try
{
    ProcessResult result = await CliRun.RunAsync("dotnet", "build", 
        cancellationToken: cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Process was cancelled");
}
```

##### Graceful vs Forceful Cancellation

CliInvoke supports different cancellation strategies, controlled via `ProcessCancellationMode`:

* **Graceful** (default) – Sends SIGTERM/SIGINT signals to allow the process to shut down cleanly and release resources. The process has an opportunity to handle the signal and exit gracefully.
* **Forceful** – Forcefully terminates the process and all child processes immediately without waiting for graceful shutdown.

You can configure the cancellation behavior when a timeout occurs:

```csharp
using CliInvoke;
using CliInvoke.Core;

// Configure forceful cancellation on timeout (immediate termination)
ProcessTimeoutPolicy forcefulTimeout = new ProcessTimeoutPolicy(
    timeoutThreshold: TimeSpan.FromSeconds(30), 
    cancellationMode: ProcessCancellationMode.Forceful);

ProcessExitConfiguration exitConfig = new ProcessExitConfiguration(forcefulTimeout);
```

##### Cancellation Exception Behavior

By default, you can control whether cancellations throw exceptions using `ProcessCancellationExceptionBehavior`. You can configure this in the `ProcessExitConfiguration`:

```csharp
using CliInvoke;
using CliInvoke.Core;

// Configure to allow an exception on cancellation
ProcessExitConfiguration exitConfig = new ProcessExitConfiguration(
    timeoutPolicy: ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromSeconds(10)),
    resultValidation: ProcessResultValidation.ExitCodeZero,
    cancellationValidation: ProcessCancellationExceptionBehavior.AllowException);

try
{
    ProcessResult result = await invoker.ExecuteAsync(config, exitConfig);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Process was cancelled and exception was thrown");
}
```

## Resource Cleanup

CliInvoke uses several types that manage unmanaged resources and should be disposed after use. This section explains disposal patterns for the key artifacts:

### ProcessConfiguration

**Why**: Holds unmanaged resources including `StandardInput` (a `StreamWriter`) and optionally a `SecureString` credential that contain sensitive data.

**Who**: The caller owns the `ProcessConfiguration` instance and is responsible for disposal.

**When**: After the process has finished and the configuration is no longer needed.

**How**: Use a `using` statement or explicit `Dispose()`:

```csharp
using var config = new ProcessConfiguration("cmd", "/c echo hello");
await invoker.ExecuteAsync(config);
// Disposed automatically when exiting the using block
```

### IExternalProcess

**Why**: Wraps an underlying `System.Diagnostics.Process` which owns OS resources (pipes, handles, threads).

**Who**: The caller who receives or creates the external process instance.

**When**: Immediately after `CaptureBufferedResultAsync()` completes or when monitoring is complete.

**How**: Use `await using` in async contexts:

```csharp
await using var process = invoker.StartAsync(config);
var result = await process.CaptureBufferedResultAsync(cancellationToken);
// Process is disposed automatically
```

Alternatively, call `Dispose()` explicitly:

```csharp
var process = invoker.StartAsync(config);
try
{
    var result = await process.CaptureBufferedResultAsync(cancellationToken);
}
finally
{
    process.Dispose();
}
```

### PipedProcessResult

**Why**: Holds `StandardOutput` and `StandardError` streams that own OS resources and buffered data, which must be released.

**Who**: The caller who receives the result from `CapturePipedResultAsync()`.

**When**: After reading from the streams and before the application exits or the result is no longer needed.

**How**: Use `await using` for async disposal (preferred on .NET 8+) or `using` for sync disposal:

```csharp
await using var result = await process.CapturePipedResultAsync(cancellationToken);
using (var reader = new StreamReader(result.StandardOutput))
{
    string output = await reader.ReadToEndAsync();
    // Use output...
}
// Streams are disposed automatically
```

Alternatively, call `Dispose()` explicitly:

```csharp
var result = await process.CapturePipedResultAsync(cancellationToken);
try
{
    using (var reader = new StreamReader(result.StandardOutput))
    {
        string output = await reader.ReadToEndAsync();
    }
}
finally
{
    result.Dispose();
}
```

### UserCredential

**Why**: Holds a `SecureString` containing a password, which is sensitive data that should be cleared from memory.

**Who**: Shared responsibility — the library disposes the credential when the configuration is disposed. For standalone credentials, the caller is responsible.

**When**: After the credential is no longer needed or when the parent configuration is disposed.

**How**: Dispose directly or via the parent `ProcessConfiguration`:

```csharp
using var credential = new UserCredential("domain", "user", securePassword, false);
// Use credential...
// Disposed automatically; SecureString is cleared from memory
```

Or, rely on automatic disposal through the configuration:

```csharp
var config = new ProcessConfiguration("cmd", "/c echo hello");
config.Credential = credential;
using (config)
{
    // Use configuration; credential is disposed when config is disposed
}
```

### UserCredentialBuilder

**Why**: Holds a `SecureString` while building a credential, which owns sensitive data.

**Who**: The caller who creates and uses the builder.

**When**: Immediately after calling `Build()` if the builder is not needed for further mutations.

**How**: Dispose both builder and built credential:

```csharp
UserCredential credential;
using (var builder = new UserCredentialBuilder())
{
    credential = builder
        .SetUsername("user")
        .SetPassword(securePassword)
        .Build();
}
// builder disposed; now dispose the credential
using (credential)
{
    // Use credential...
}
```

#### Common Disposal Tips

* Prefer `await using` for `IExternalProcess` and `PipedProcessResult` in async contexts to ensure cleanup.
* Never dispose a `StandardInput`, `StandardOutput`, `StandardError`, or `SecureString` twice — the library handles it when owning the resources.
* If you reuse a `ProcessConfiguration` multiple times, call `Dispose()` manually after the final use.
* Wrap builders and built credentials in `using` statements to ensure `SecureString` cleanup.
* Only these five types require explicit disposal: `ProcessConfiguration`, `IExternalProcess`, `UserCredential`, `UserCredentialBuilder`, and `PipedProcessResult`. Other CliInvoke types do not implement `IDisposable`.

## How to Build CliInvoke's code

Please see [building-cliinvoke.md](docs/docs/building-cliinvoke.md) for how to build CliInvoke from source.

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

Whilst an initial set of features are available in version 1, there is room for more features and for modifications of
existing features in future updates.

Future updates may focus on one or more of the following:

* Improved ease of use
* Improved stability
* New features
* Enhancing existing features

## New vs Old Package and Namespace

CliInvoke changed it's Nuget package ID and namespace starting from the re-release of 2.0.0 (tagged as 2.0.0-v2) and has
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
* [Polyfill](https://github.com/SimonCropp/Polyfill) for simplifying .NET Standard 2.0 support

For more information, please see
the [THIRD_PARTY_NOTICES file](https://github.com/alastairlundy/CliInvoke/blob/main/THIRD_PARTY_NOTICES.txt).
