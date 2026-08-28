# CliInvoke.Specializations

This readme covers the **CliInvoke Specializations** library.

Looking for the [CliInvoke Readme](https://github.com/alastairlundy/CliInvoke/blob/main/README.md)?


<!-- Badges -->
[![Latest NuGet](https://img.shields.io/nuget/v/CliInvoke.Specializations.svg)](https://www.nuget.org/packages/CliInvoke.Specializations/)
[![Latest Pre-release NuGet](https://img.shields.io/nuget/vpre/CliInvoke.Specializations.svg)](https://www.nuget.org/packages/CliInvoke.Specializations/)
[![Downloads](https://img.shields.io/nuget/dt/CliInvoke.Specializations.svg)](https://www.nuget.org/packages/CliInvoke.Specializations/)
![License](https://img.shields.io/github/license/alastairlundy/CliInvoke)

## Usage

CliInvoke.Specializations comes with two specializations as of 3.0.0:

- [CmdProcessConfiguration](#cmdprocessconfiguration) — An easier way to execute processes and commands through
  Windows' `cmd.exe`.
- [PowershellProcessConfiguration](#powershellprocessconfiguration) — An easier way to execute processes and commands
  through the modern Cross-Platform open source PowerShell (PowerShell is not installed by CliInvoke and is expected to
  be installed if you plan to use it.)

All Command specialization classes come with an already configured `TargetFilePath` that points to the relevant
executable.

### Quick start with CliRun

The fastest way to run a specialization configuration is via the static `CliRun` helper. It handles process creation
and disposal for you, so you only need to build the configuration and await the result.

```csharp
using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Specializations.Configurations;

// Run a PowerShell command using the cross-platform pwsh executable.
using PowershellProcessConfiguration config = new PowershellProcessConfiguration("-Command Get-Process");

BufferedProcessResult result = await CliRun.RunBufferedAsync(config, ProcessExitConfiguration.CreateGraceful());

// result.StandardOutput contains the captured output; result.ExitCode holds the process exit code.
```

`CliRun` also exposes `RunAsync` (returns a `ProcessResult`), `RunPipedAsync` (returns a `PipedProcessResult`), and
`FireAndForget` for fire-and-forget execution.

### Dependency Injection

If you prefer to resolve an invoker from a dependency injection container, call `AddCliInvoke()` (namespace
`CliInvoke.Extensions`). This registers the core services, the `IProcessInvoker` implementation, the
`IRunnerConfigurationFactory`, and the specializations middleware (`UsePowerShell()` and `UseCmd()`) on the
`IProcessMiddlewareBuilder`.

```csharp
using CliInvoke.Extensions;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new ServiceCollection();

services.AddCliInvoke();

// The specializations middleware is registered automatically, but you can also compose it explicitly:
// services.AddCliInvoke(builder => builder.UsePowerShell().UseCmd());

using IServiceProvider serviceProvider = services.BuildServiceProvider();
```

### CmdProcessConfiguration

The `CmdProcessConfiguration` `TargetFilePath` points to Windows' copy of `cmd.exe`. This is only supported on Windows.

```csharp
using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Core.Extensibility;
using CliInvoke.Specializations.Configurations;

// ServiceProvider and Dependency Injection code ommitted for clarity

IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();
IRunnerConfigurationFactory _runnerConfigurationFactory = serviceProvider.GetRequiredService<IRunnerConfigurationFactory>();

// Create your runner configuration.
ProcessConfiguration runnerConfig = new CmdProcessConfiguration("Your arguments go here",
    // redirectStandardInput, outputRedirection, workingDirectoryPath
    false, true, Environment.SystemDirectory);

// Create your configuration to be run.
ProcessConfiguration config = new ProcessConfiguration("Path/To/Exe", "With/Arguments");

// Creates a ProcessConfiguration that will use the runner configuration to run the desired configuration.
ProcessConfiguration processToRun = _runnerConfigurationFactory.CreateRunnerConfiguration(config, runnerConfig);

BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(processToRun);
```

If the result of the command being run is not of concern you can call `ExecuteAsync()` instead of
`ExecuteBufferedAsync()` and ignore the returned `ProcessResult` like so:

```csharp
using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Core.Extensibility;
using CliInvoke.Specializations.Configurations;

// ServiceProvider and Dependency Injection code ommitted for clarity

IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();
IRunnerConfigurationFactory _runnerConfigurationFactory = serviceProvider.GetRequiredService<IRunnerConfigurationFactory>();

// Create your runner configuration.
ProcessConfiguration runnerConfig = new CmdProcessConfiguration("Your arguments go here",
    // redirectStandardInput, outputRedirection, workingDirectoryPath
    false, true, Environment.SystemDirectory);

// Create your configuration to be run.
ProcessConfiguration config = new ProcessConfiguration("Path/To/Exe", "With/Arguments");

// Creates a ProcessConfiguration that will use the runner configuration to run the desired configuration.
ProcessConfiguration processToRun = _runnerConfigurationFactory.CreateRunnerConfiguration(config, runnerConfig);

ProcessResult result = await _processInvoker.ExecuteAsync(processToRun);
```

### PowershellProcessConfiguration

The `PowershellProcessConfiguration`'s `TargetFilePath` points to the installed copy of cross-platform PowerShell if it
is installed.

This is only supported on platforms that cross-platform PowerShell supports.

```csharp
using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Core.Extensibility;
using CliInvoke.Specializations.Configurations;

// ServiceProvider and Dependency Injection code ommitted for clarity

IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();
IRunnerConfigurationFactory _runnerConfigurationFactory = serviceProvider.GetRequiredService<IRunnerConfigurationFactory>();

// Create your runner configuration.
ProcessConfiguration runnerConfig = new PowershellProcessConfiguration("-Command Get-Process",
    // redirectStandardInput, outputRedirection
    false, true);

// Create your configuration to be run.
ProcessConfiguration config = new ProcessConfiguration("Path/To/Exe", "With/Arguments");

// Creates a ProcessConfiguration that will use the runner configuration to run the desired configuration.
ProcessConfiguration processToRun = _runnerConfigurationFactory.CreateRunnerConfiguration(config, runnerConfig);

BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(processToRun);
```

## Licensing

CliInvoke and CliInvoke Specializations are licensed under the MPL 2.0 license.

If you use CliInvoke or CliInvoke.Specializations in your project, please make an exact copy of CliInvoke's LICENSE.txt
file available either in your third party licenses txt file or as a separate txt file.
