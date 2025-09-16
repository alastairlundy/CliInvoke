# CliInvoke.Specializations
This readme covers the **CliInvoke Specializations** library. Looking for the [CliInvoke Readme](https://github.com/alastairlundy/CliInvoke/blob/main/README.md)?

[![NuGet](https://img.shields.io/nuget/v/AlastairLundy.CliInvoke.Specializations.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Specializations/)
[![NuGet](https://img.shields.io/nuget/dt/AlastairLundy.CliInvoke.Specializations.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Specializations/)

## Usage
CliInvoke.Specializations comes with 3 specializations as of 1.0.0: 
- [CmdProcessConfiguration](#cmdprocessconfiguration) - An easier way to execute processes and commands through cmd.exe (Only supported on Windows)
- [ClassicPowershellProcessConfiguration](#classicpowershellprocessconfiguration) - An easier way to execute processes and commands through Windows Powershell (Only supported on Windows)
- [PowershellProcessConfiguration](#powershellprocessconfiguration) - An easier way to execute processes and commands through the modern Cross-Platform open source Powershell (Powershell is not installed by CliInvoke and is expected to be installed if you plan to use it.)

All Command specialization classes come with an already configured TargetFilePath that points to the relevant executable.

### CmdProcessConfiguration
The CmdProcessConfiguration TargetFilePath points to Windows' copy of cmd.exe .

```csharp
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Extensibility;

using AlastairLundy.CliInvoke.Specializations.Configurations;
using AlastairLundy.CliInvoke.Specializations;

    // ServiceProvider and Dependency Injection code ommitted for clarity
    
    IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();
  IRunnerProcessCreator _runnerProcessCreator = serviceProvider.GetRequiredService<IRunnerProcessCreator>();
  
  //Build your command fluently
  IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder(
          new CmdProcessConfiguration("Your arguments go here"))
                .WithWorkingDirectory(Environment.SystemDirectory);
  
  ProcessConfiguration config = builder.Build();
  
  ProcessConfiguration processToRun = _runnerProcessCreator.CreateRunnerProcess(config);
  
  BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(processToRun);
```

If the result of the command being run is not of concern you can call ``ExecuteAsync()`` instead of ``ExecuteBufferedAsync()`` and ignore the returned ProcessResult like so:
```csharp
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Extensibility;

using AlastairLundy.CliInvoke.Specializations.Configurations;
using AlastairLundy.CliInvoke.Specializations;

    // ServiceProvider and Dependency Injection code ommitted for clarity
    
    IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();
  IRunnerProcessCreator _runnerProcessCreator = serviceProvider.GetRequiredService<IRunnerProcessCreator>();
  
  //Build your command fluently
  IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder(
          new CmdProcessConfiguration("Your arguments go here"))
                .WithWorkingDirectory(Environment.SystemDirectory);
  
  ProcessConfiguration config = builder.Build();
  
  ProcessConfiguration processToRun = _runnerProcessCreator.CreateRunnerProcess(config);
  
  ProcessResult result = await _processInvoker.ExecuteAsync(processToRun);
```

### ClassicPowershellProcessConfiguration
The ClassicPowershellCommand is a specialized Command class with an already configured TargetFilePath that points to Windows' copy of powershell.exe .

```csharp
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Extensibility;

using AlastairLundy.CliInvoke.Specializations.Configurations;
using AlastairLundy.CliInvoke.Specializations;

    // ServiceProvider and Dependency Injection code ommitted for clarity
    
    IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();
  IRunnerProcessCreator _runnerProcessCreator = serviceProvider.GetRequiredService<IRunnerProcessCreator>();
  
  //Build your command fluently
  IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder(
          new ClassicPowershellProcessConfiguration("Your arguments go here"))
                .WithWorkingDirectory(Environment.SystemDirectory);
  
  ProcessConfiguration config = builder.Build();
  
  ProcessConfiguration processToRun = _runnerProcessCreator.CreateRunnerProcess(config);
  
  BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(processToRun);
```

### PowershellProcessConfiguration
The PowershellProcessConfiguration's TargetFilePath points to the installed copy of cross-platform Powershell if it is installed.

```csharp
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Extensibility;

using AlastairLundy.CliInvoke.Specializations.Configurations;
using AlastairLundy.CliInvoke.Specializations;

    // ServiceProvider and Dependency Injection code ommitted for clarity
    
    IProcessInvoker _processInvoker = serviceProvider.GetRequiredService<IProcessInvoker>();
  IRunnerProcessCreator _runnerProcessCreator = serviceProvider.GetRequiredService<IRunnerProcessCreator>();
  
  //Build your command fluently
  IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder(
          new PowershellProcessConfiguration("Your arguments go here"))
                .WithWorkingDirectory(Environment.SystemDirectory);
  
  ProcessConfiguration config = builder.Build();
  
  ProcessConfiguration processToRun = _runnerProcessCreator.CreateRunnerProcess(config);
  
  BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(processToRun);
```

## Licensing
CliInvoke and CliInvoke Specializations are licensed under the MPL 2.0 license. If you modify any of CliInvoke's or CliInvoke.Specialization's files then the modified files must be licensed under the MPL 2.0 .

If you use CliInvoke or CliInvoke.Specializations in your project please make an exact copy of the contents of CliInvoke's LICENSE.txt file available either in your third party licenses txt file or as a separate txt file.
