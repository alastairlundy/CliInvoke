# CliInvoke.Specializations

This readme covers the **CliInvoke Specializations** library.

Looking for the [CliInvoke Readme](https://github.com/alastairlundy/CliInvoke/blob/main/README.md)?


<!-- Badges -->
[![Latest NuGet](https://img.shields.io/nuget/v/CliInvoke.Specializations.svg)](https://www.nuget.org/packages/CliInvoke.Specializations/)
[![Latest Pre-release NuGet](https://img.shields.io/nuget/vpre/CliInvoke.Specializations.svg)](https://www.nuget.org/packages/CliInvoke.Specializations/)
[![Downloads](https://img.shields.io/nuget/dt/CliInvoke.Specializations.svg)](https://www.nuget.org/packages/CliInvoke.Specializations/)
![License](https://img.shields.io/github/license/alastairlundy/CliInvoke)

## Usage

CliInvoke.Specializations ships three shell middleware types: `PowerShellMiddleware` (`UsePowerShell()`), `CmdMiddleware` (`UseCmd()`), and `DefaultShellMiddleware` (`UseDefaultShell()`). They wrap the command in the relevant shell at invocation time.

The legacy `CmdProcessConfiguration` and `PowershellProcessConfiguration` subclasses are deprecated, marked `[Obsolete]`, and will be removed in 4.0. New code should use `ProcessConfiguration` with the matching middleware instead. The old subclasses are still documented below for existing users:

- [CmdProcessConfiguration](#cmdprocessconfiguration-deprecated) — only for existing users, runs through Windows' `cmd.exe`.
- [PowershellProcessConfiguration](#powershellprocessconfiguration-deprecated) — only for existing users, runs through cross-platform PowerShell (PowerShell is not installed by CliInvoke and must already be installed).

### Quick start with CliRun

The fastest path is the static `CliRun` helper. `CliRun` runs each call through a fresh pipeline with no middleware, so target the shell executable directly.

```csharp
using CliInvoke;
using CliInvoke.Core;

// Run a PowerShell command using the cross-platform pwsh executable.
ProcessConfiguration config = new ProcessConfiguration
{
    TargetFilePath = "pwsh",
    Arguments = "-Command Get-Process"
};

BufferedProcessResult result = await CliRun.RunBufferedAsync(config, ProcessExitConfiguration.CreateGraceful());
```

`CliRun` also exposes `RunAsync` (returns a `ProcessResult`) and
`FireAndForget` for fire-and-forget execution. To wrap commands through middleware instead, use the dependency injection path below.

### Dependency injection

If you prefer to resolve an invoker from a dependency injection container, call `AddCliInvoke()` (namespace
`CliInvoke.Extensions`, shipped in the main `CliInvoke` package). This registers the core services, the
`IProcessInvoker` implementation, the `IRunnerConfigurationFactory`, and the `IExternalProcessFactory`.

#### AddCliInvokeSpecializations

`AddCliInvokeSpecializations()` (namespace `CliInvoke.Extensions`, shipped in this package) registers the
Specializations middleware types (`PowerShellMiddleware`, `CmdMiddleware`, `DefaultShellMiddleware`, and
`ShellMiddlewareOptions`), so the convenience builder extensions `UsePowerShell()`, `UseCmd()`, and
`UseDefaultShell()` can resolve them from the DI container.

`DefaultShellMiddleware` detects the user's default shell (pwsh, Windows PowerShell, or cmd) and wraps the
command in it automatically. Use it when you want cross-platform shell detection instead of targeting a
specific shell.

> **`AddCliInvoke()` is required.** `AddCliInvokeSpecializations()` only registers middleware types; it does
> **not** register core CliInvoke services. You **must** call `AddCliInvoke()` as well, or the invoker,
> process factory, and other core services will not be available.

Both registrations accept an optional `ServiceLifetime` parameter (default `Scoped`). The two calls are
independent and can be chained in either order, but both must use the **same lifetime**. Middleware lifetimes
are matched to the invoker lifetime to avoid capturing scoped services into a singleton:

```csharp
using CliInvoke.Extensions;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new ServiceCollection();

// AddCliInvoke() is required — it registers core services.
// AddCliInvokeSpecializations() registers the Cmd/PowerShell/DefaultShell middleware types.
services.AddCliInvoke(builder => builder.UsePowerShell().UseCmd())
    .AddCliInvokeSpecializations();

using ServiceProvider serviceProvider = services.BuildServiceProvider();
```

> Calling `AddCliInvoke(builder => builder.UsePowerShell())` without `AddCliInvokeSpecializations()` compiles but
> throws `InvalidOperationException` when the invoker is first resolved, because the `PowerShellMiddleware` type
> is not registered in the container.

### CmdProcessConfiguration (deprecated)

> [!WARNING]
> `CmdProcessConfiguration` is marked `[Obsolete]` and will be removed in 4.0. New code should use `ProcessConfiguration` with `UseCmd()` middleware. The example below remains for existing users.

```csharp
using CliInvoke.Core;
using CliInvoke.Extensions;
using CliInvoke.Specializations.Middleware;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new ServiceCollection();

services.AddCliInvoke(builder => builder.UseCmd())
    .AddCliInvokeSpecializations();

using ServiceProvider serviceProvider = services.BuildServiceProvider();

IProcessInvoker processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();

ProcessConfiguration config = new ProcessConfiguration
{
    TargetFilePath = "Path/To/Exe",
    Arguments = "With/Arguments"
};

BufferedProcessResult result = await processInvoker.ExecuteBufferedAsync(config);
```

The legacy subclass equivalent, for existing users only: `CmdProcessConfiguration` `TargetFilePath` points to Windows' copy of `cmd.exe`. This is only supported on Windows.

```csharp
using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Core.Extensibility;
using CliInvoke.Specializations.Configurations;

    // DI setup omitted for clarity

IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();
IRunnerConfigurationFactory _runnerConfigurationFactory = serviceProvider.GetRequiredService<IRunnerConfigurationFactory>();

ProcessConfiguration runnerConfig = new CmdProcessConfiguration("Your arguments go here",
    false, true, Environment.SystemDirectory);

ProcessConfiguration config = new ProcessConfiguration("Path/To/Exe", "With/Arguments");
ProcessConfiguration processToRun = _runnerConfigurationFactory.CreateRunnerConfiguration(config, runnerConfig);

BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(processToRun);
```

To discard the output, call `ExecuteAsync()` instead:

```csharp
// Same setup as above, then:
ProcessResult result = await _processInvoker.ExecuteAsync(processToRun);
```

### PowershellProcessConfiguration (deprecated)

> [!WARNING]
> `PowershellProcessConfiguration` is marked `[Obsolete]` and will be removed in 4.0. New code should use `ProcessConfiguration` with `UsePowerShell()` middleware. The example below remains for existing users.

```csharp
using CliInvoke.Core;
using CliInvoke.Extensions;
using CliInvoke.Specializations.Middleware;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new ServiceCollection();

services.AddCliInvoke(builder => builder.UsePowerShell())
    .AddCliInvokeSpecializations();

using ServiceProvider serviceProvider = services.BuildServiceProvider();

IProcessInvoker processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();

ProcessConfiguration config = new ProcessConfiguration
{
    TargetFilePath = "Path/To/Exe",
    Arguments = "With/Arguments"
};

BufferedProcessResult result = await processInvoker.ExecuteBufferedAsync(config);
```

The legacy subclass equivalent, for existing users only: `PowershellProcessConfiguration.TargetFilePath` points to the installed copy of cross-platform PowerShell. Supported on the platforms that `pwsh` supports.

```csharp
using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Core.Extensibility;
using CliInvoke.Specializations.Configurations;

// DI setup omitted for clarity

IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();
IRunnerConfigurationFactory _runnerConfigurationFactory = serviceProvider.GetRequiredService<IRunnerConfigurationFactory>();

ProcessConfiguration runnerConfig = new PowershellProcessConfiguration("-Command Get-Process",
    false, true);

ProcessConfiguration config = new ProcessConfiguration("Path/To/Exe", "With/Arguments");
ProcessConfiguration processToRun = _runnerConfigurationFactory.CreateRunnerConfiguration(config, runnerConfig);

BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(processToRun);
```

### Dedicated invokers (removed)

`CmdProcessInvoker` and `PowershellProcessInvoker` were removed. Use `IProcessInvoker` with `UseCmd()` or `UsePowerShell()` middleware instead, as shown above.

## Licensing

CliInvoke and CliInvoke Specializations are licensed under the MPL 2.0 license.

If you use CliInvoke or CliInvoke.Specializations in your project, please make an exact copy of CliInvoke's LICENSE.txt
file available either in your third party licenses txt file or as a separate txt file.
