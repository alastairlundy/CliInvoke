# CliInvoke.Core

This package contains Process Running and handling abstractions as well as common types used by implementing classes.

For an implementing package, check out [CliInvoke](https://www.nuget.org/packages/CliInvoke/).

<!-- Badges -->
[![Latest NuGet](https://img.shields.io/nuget/v/CliInvoke.Core.svg)](https://www.nuget.org/packages/CliInvoke.Core/)
[![Latest Pre-release NuGet](https://img.shields.io/nuget/vpre/CliInvoke.Core.svg)](https://www.nuget.org/packages/CliInvoke.Core/)
[![Downloads](https://img.shields.io/nuget/dt/CliInvoke.Core.svg)](https://www.nuget.org/packages/CliInvoke.Core/)
![License](https://img.shields.io/github/license/alastairlundy/CliInvoke)

Key abstractions:

* ``IProcessInvoker`` - Runs a ``ProcessConfiguration`` and returns the result.
* ``IExternalProcessFactory`` - Creates ``IExternalProcess`` instances from a ``ProcessConfiguration`` for lifecycle control.

* Output redirection:
    * Output redirection is handled via ``IProcessInvoker.ExecuteBufferedAsync`` (or
      ``IExternalProcess.CaptureBufferedResultAsync``).

* Fluent specs and builders:
    * ``ArgumentsSpec`` - A spec for argument building and argument escaping.
    * ``EnvironmentVariablesSpec`` - A spec for setting environment variables.
    * ``IProcessConfigurationBuilder`` - An interface to fluently configure and build ``ProcessConfiguration`` objects. Prefer direct init construction. Reach for the builder only for argument escaping, user credentials, or resource-policy flows.
    * ``ProcessResourcePolicySpec`` - A spec for fluently configuring and building ``ProcessResourcePolicy``
      objects.
    * ``UserCredentialSpec``

## Features

* Clear separation of concerns between Process Configuration Builders, Process Configuration Models, and Invokers.
* Supports .NET 10 and has few dependencies.
* Dependency Injection extensions in the main `CliInvoke` package register `IProcessInvoker`, `IExternalProcessFactory`, and middleware from a single `AddCliInvoke()` call.
* Support for specific specializations such as running executables or commands via Windows PowerShell or CMD on
  Windows <sup>1</sup>
* [SourceLink](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink) support

<sup>1</sup> Specializations library distributed separately.

## Comparison vs alternatives

CliInvoke is compared against [CliWrap](https://github.com/Tyrrrz/CliWrap/), [ProcessX](https://github.com/Cysharp/ProcessX), and the built-in .NET `Process` class across features like configuration separation, DI support, middleware, cross-platform support, and licensing.

See the [full comparison table](../../site/docs/comparison.md) for a detailed feature-by-feature breakdown.

## Installing CliInvoke.Core

CliInvoke.Core packages can be installed via the .NET SDK CLI, Nuget via your IDE or code editor's package interface, or
via the Nuget website.

| Package Name   | Nuget Link                                                        | .NET SDK CLI command                  |
|----------------|-------------------------------------------------------------------|---------------------------------------|
| CliInvoke.Core | [CliInvoke.Core Nuget](https://nuget.org/packages/CliInvoke.Core) | ``dotnet add package CliInvoke.Core`` |

## Supported Platforms

CliInvoke supports Windows, macOS, Linux, FreeBSD, and Android.

For more details see
the [list of supported platforms](https://github.com/alastairlundy/CliInvoke/blob/main/site/docs/Supported-OperatingSystems.md)

## Design Patterns & When to Use Them

CliInvoke.Core provides abstractions and types used by different design patterns. For comprehensive documentation on design patterns, see [DESIGN_PATTERNS.md](../../DESIGN_PATTERNS.md).

* **`CliRun`** – Beginner-friendly/quickstart entrypoint. Use for basic scripting, CI/CD tasks, or simple command execution. Zero boilerplate, optional arguments with sensible defaults. (Requires `CliInvoke` package)
* **`IProcessInvoker`** – DI-centric pattern for end-to-end process management. Use when building applications that need testability, dependency injection integration, or custom process configuration per invocation.
* **`IExternalProcess` & `IExternalProcessFactory`** – Process-like API with greater flexibility. Use when you need granular lifecycle control, manual start/stop sequences, or power-user scenarios similar to `System.Diagnostics.Process`.

## Examples

### Simple ``ProcessConfiguration`` with init construction

Build the configuration directly. ``TargetFilePath`` is required and all other properties take documented defaults.

#### Non-buffered execution example

This example gets a non-buffered ``ProcessResult`` that contains the exit code, process id, and other information.

```csharp
using CliInvoke.Core;

using Microsoft.Extensions.DependencyInjection;

// Dependency Injection setup code omitted for clarity

// Get IProcessInvoker
IProcessInvoker invoker = serviceProvider.GetRequiredService<IProcessInvoker>();

// Build the process configuration directly.
ProcessConfiguration configuration = new ProcessConfiguration
{
    TargetFilePath = "path/to/exe",
    Arguments = "arguments"
};

// Run the process configuration and get the result.
ProcessResult result = await invoker.ExecuteAsync(configuration);
```

#### Buffered execution example

This example gets a ``BufferedProcessResult`` which contains redirected StandardOutput and StandardError as strings.

```csharp
using CliInvoke.Core;

using Microsoft.Extensions.DependencyInjection;

// Dependency Injection setup code omitted for clarity

// Get IProcessInvoker
IProcessInvoker invoker = serviceProvider.GetRequiredService<IProcessInvoker>();

// Build the process configuration directly.
ProcessConfiguration configuration = new ProcessConfiguration
{
    TargetFilePath = "path/to/exe",
    Arguments = "arguments"
};

// Run the process configuration and get the result.
BufferedProcessResult result = await invoker.ExecuteBufferedAsync(configuration);
```

### Advanced configuration with builders

Reach for ``IProcessConfigurationBuilder`` only when you need argument escaping, user credentials, or resource-policy callback flows. For everything else, prefer direct init construction as shown above. The builder implementation ships in the main ``CliInvoke`` package.

#### Non-buffered execution example

```csharp
using CliInvoke.Builders;
using CliInvoke.Core;
using CliInvoke.Core.Builders;

using Microsoft.Extensions.DependencyInjection;

// Namespace and class code omitted for clarity

// ServiceProvider and Dependency Injection setup code omitted for clarity

IProcessInvoker processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();

// Fluently configure the command. The builder is disposable.
using IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("Path/To/Executable");
builder.SetArguments(["arg1", "arg2"]);
builder.SetWorkingDirectory("/Path/To/Directory");

// Build it as a ProcessConfiguration object when ready to use it.
ProcessConfiguration config = builder.Build();

// Execute the process through the invoker and get the result.
ProcessResult result = await processInvoker.ExecuteAsync(config);
```

#### Buffered execution example

```csharp
using CliInvoke.Builders;
using CliInvoke.Core;
using CliInvoke.Core.Builders;

using Microsoft.Extensions.DependencyInjection;

// Namespace and class code omitted for clarity

// ServiceProvider and Dependency Injection setup code omitted for clarity

IProcessInvoker processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();

// Fluently configure the command. The builder is disposable.
using IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("Path/To/Executable");
builder.SetArguments(["arg1", "arg2"]);
builder.SetWorkingDirectory("/Path/To/Directory");
builder.SetOutputRedirection(true);

// Build it as a ProcessConfiguration object when ready to use it.
ProcessConfiguration config = builder.Build();

// Execute the process through the invoker and get the result.
BufferedProcessResult result = await processInvoker.ExecuteBufferedAsync(config);
```

#### Cancellation and Timeout

CliInvoke provides flexible timeout and cancellation support for process execution. By default, processes have a **2-minute timeout** with graceful exit behavior.

##### Default Timeout Policy

The default timeout policy is applied when creating a `ProcessExitConfiguration` without explicit parameters:

```csharp
using CliInvoke.Core;

// Default ProcessExitConfiguration has 2-minute timeout with graceful exit
ProcessExitConfiguration exitConfig = new ProcessExitConfiguration();
ProcessResult result = await invoker.ExecuteAsync(config, exitConfig);
```

##### Custom Timeout Configuration

You can customize the timeout by creating a `ProcessExitConfiguration` with a `ProcessTimeoutPolicy`:

```csharp
using CliInvoke.Core;

// Create a custom timeout policy (5-minute timeout with graceful exit)
ProcessTimeoutPolicy customTimeout = ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromMinutes(5));
ProcessExitConfiguration exitConfig = new ProcessExitConfiguration(customTimeout);

// Execute with custom timeout
ProcessResult result = await invoker.ExecuteAsync(config, exitConfig);
```

##### Cancellation Support

Cancel process execution using a `CancellationToken`:

```csharp
using CliInvoke.Core;
using System.Threading;

CancellationTokenSource cts = new CancellationTokenSource();

// Cancel after 30 seconds
cts.CancelAfter(TimeSpan.FromSeconds(30));

try
{
    ProcessExitConfiguration exitConfig = ProcessExitConfiguration.CreateGraceful();
    ProcessResult result = await invoker.ExecuteAsync(config, exitConfig, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Process was cancelled");
}
```

##### Graceful vs Forceful Cancellation

CliInvoke supports two cancellation strategies, controlled via `ProcessExitBehaviour`:

* **Graceful Exit** (default) – Sends SIGTERM/SIGINT signals to allow the process to shut down cleanly and release resources. The process has an opportunity to handle the signal and exit gracefully.
* **Forceful Exit** – Forcefully terminates the process and all child processes immediately without waiting for graceful shutdown.

You can configure the cancellation behavior when a timeout occurs or when cancellation is requested:

```csharp
using CliInvoke.Core;

// Configure forceful exit on timeout (immediate termination)
ProcessTimeoutPolicy forcefulTimeout = new ProcessTimeoutPolicy(
    timeoutThreshold: TimeSpan.FromSeconds(30),
    enabled: true,
    exitBehaviour: ProcessExitBehaviour.ForcefulExit);

ProcessExitConfiguration exitConfig = new ProcessExitConfiguration(forcefulTimeout);
```

##### Cancellation Exception Behavior

By default, cancellations do not throw exceptions—the process simply exits and a result is returned. You can change this behavior with `CancellationThrowsException`:

```csharp
using CliInvoke.Core;

// Configure to throw an exception on cancellation
ProcessExitConfiguration exitConfig = new ProcessExitConfiguration(
    timeoutPolicy: ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromSeconds(10)),
    requestedCancellationExitBehaviour: ProcessExitBehaviour.GracefulExit,
    cancellationThrowsException: true);

try
{
    ProcessResult result = await invoker.ExecuteAsync(config, exitConfig);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Process was cancelled and exception was thrown");
}
```

##### Disable Timeout

To disable the timeout entirely, use `ProcessTimeoutPolicy.None`:

```csharp
ProcessExitConfiguration noTimeout = new ProcessExitConfiguration(ProcessTimeoutPolicy.None);
// Process will wait indefinitely for completion
```

## Acknowledgements

### Projects

Thanks to these projects:

* [Polyfill](https://github.com/SimonCropp/Polyfill) for simplifying TFM support

For more information, please see
the [THIRD_PARTY_NOTICES file](https://github.com/alastairlundy/CliInvoke/blob/main/THIRD_PARTY_NOTICES.txt).
