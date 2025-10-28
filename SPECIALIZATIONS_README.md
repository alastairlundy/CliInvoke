# CliInvoke.Specializations
This readme covers the **CliInvoke Specializations** library. 

Looking for the [CliInvoke Readme](https://github.com/alastairlundy/CliInvoke/blob/main/README.md)?


<!-- Badges -->
[![Latest NuGet](https://img.shields.io/nuget/v/AlastairLundy.CliInvoke.Specializations.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Specializations/)
[![Latest Pre-release NuGet](https://img.shields.io/nuget/vpre/AlastairLundy.CliInvoke.Specializations.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Specializations/)
[![Downloads](https://img.shields.io/nuget/dt/AlastairLundy.CliInvoke.Specializations.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Specializations/)
![License](https://img.shields.io/github/license/alastairlundy/CliInvoke)

## Usage
CliInvoke.Specializations comes with three specializations as of 1.0.0: 
- [CmdProcessConfiguration](#cmdprocessconfiguration) — An easier way to execute processes and commands through cmd.exe (Only supported on Windows)
- [ClassicPowershellProcessConfiguration](#classicpowershellprocessconfiguration) — An easier way to execute processes and commands through Windows PowerShell (Only supported on Windows)
- [PowershellProcessConfiguration](#powershellprocessconfiguration) — An easier way to execute processes and commands through the modern Cross-Platform open source PowerShell (PowerShell is not installed by CliInvoke and is expected to be installed if you plan to use it.)

All Command specialization classes come with an already configured TargetFilePath that points to the relevant executable.

### CmdProcessConfiguration
The CmdProcessConfiguration TargetFilePath points to Windows' copy of cmd.exe.

```csharp
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Extensibility.Factories;

using AlastairLundy.CliInvoke.Specializations.Configurations;
using AlastairLundy.CliInvoke.Specializations;

    // ServiceProvider and Dependency Injection code ommitted for clarity
    
    IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();
  IRunnerProcessFactory _runnerProcessFactory = serviceProvider.GetRequiredService<IRunnerProcessFactory>();
  
  //Create your runner configuration.
         ProcessConfiguration runnerConfig = new CmdProcessConfiguration("Your arguments go here",
                    // Set standard input, output, and error
          false, true, true, Environment.SystemDirectory);
  
  // Create your configuration to be run.
  ProcessConfiguration config = new ProcessConfiguration("Path/To/Exe",
  false, true, true, "With/Arguments")
  
  // Creates a ProcessConfiguration that will use the runner configuration to run the desired configuration.
  ProcessConfiguration processToRun = _runnerProcessFactory.CreateRunnerConfiguration(config, runnerConfig);
  
  BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(processToRun);
```

If the result of the command being run is not of concern you can call ``ExecuteAsync()`` instead of ``ExecuteBufferedAsync()`` and ignore the returned ProcessResult like so:
```csharp
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Extensibility.Factories;

using AlastairLundy.CliInvoke.Specializations.Configurations;
using AlastairLundy.CliInvoke.Specializations;

    // ServiceProvider and Dependency Injection code ommitted for clarity
    
    IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();
  IRunnerProcessFactory _runnerProcessFactory = serviceProvider.GetRequiredService<IRunnerProcessFactory>();
  
  //Create your runner configuration.
         ProcessConfiguration runnerConfig = new CmdProcessConfiguration("Your arguments go here",
                    // Set standard input, output, and error
          false, true, true, Environment.SystemDirectory);
  
  // Create your configuration to be run.
  ProcessConfiguration config = new ProcessConfiguration("Path/To/Exe",
  false, true, true, "With/Arguments")
  
  // Creates a ProcessConfiguration that will use the runner configuration to run the desired configuration.
  ProcessConfiguration processToRun = _runnerProcessFactory.CreateRunnerConfiguration(config, runnerConfig);
  
  ProcessResult result = await _processInvoker.ExecuteAsync(processToRun);
```

### ClassicPowershellProcessConfiguration
The ClassicPowershellCommand is a specialized Command class with an already configured TargetFilePath that points to Windows' copy of powershell.exe.

```csharp
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Extensibility.Factories;

using AlastairLundy.CliInvoke.Specializations.Configurations;
using AlastairLundy.CliInvoke.Specializations;

    // ServiceProvider and Dependency Injection code ommitted for clarity
    
    IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();
  IRunnerProcessFactory _runnerProcessFactory = serviceProvider.GetRequiredService<IRunnerProcessFactory>();
  
  //Create your runner configuration.
         ProcessConfiguration runnerConfig = new CmdProcessConfiguration("Your arguments go here",
                    // Set standard input, output, and error
          false, true, true, Environment.SystemDirectory);
  
  // Create your configuration to be run.
  ProcessConfiguration config = new ProcessConfiguration("Path/To/Exe",
  false, true, true, "With/Arguments")
  
  // Creates a ProcessConfiguration that will use the runner configuration to run the desired configuration.
  ProcessConfiguration processToRun = _runnerProcessFactory.CreateRunnerConfiguration(config, runnerConfig);
  
  BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(processToRun);
```

### PowershellProcessConfiguration
The PowershellProcessConfiguration's TargetFilePath points to the installed copy of cross-platform PowerShell if it is installed.

```csharp
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Extensibility.Factories;

using AlastairLundy.CliInvoke.Specializations.Configurations;
using AlastairLundy.CliInvoke.Specializations;

    // ServiceProvider and Dependency Injection code ommitted for clarity
    
    IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();
  IRunnerProcessFactory _runnerProcessFactory = serviceProvider.GetRequiredService<IRunnerProcessFactory>();
  
  //Create your runner configuration.
         ProcessConfiguration runnerConfig = new CmdProcessConfiguration("Your arguments go here",
                    // Set standard input, output, and error
          false, true, true, Environment.SystemDirectory);
  
  // Create your configuration to be run.
  ProcessConfiguration config = new ProcessConfiguration("Path/To/Exe",
  false, true, true, "With/Arguments")
  
  // Creates a ProcessConfiguration that will use the runner configuration to run the desired configuration.
  ProcessConfiguration processToRun = _runnerProcessFactory.CreateRunnerConfiguration(config, runnerConfig);
  
  BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(processToRun);
```

## Licensing
CliInvoke and CliInvoke Specializations are licensed under the MPL 2.0 license.

If you use CliInvoke or CliInvoke.Specializations in your project, please make an exact copy of CliInvoke's LICENSE.txt file available either in your third party licenses txt file or as a separate txt file.
