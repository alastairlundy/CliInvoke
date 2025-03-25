# CliInvoke.Specializations
This readme covers the **CliInvoke Specializations** library. Looking for the [CliInvoke Readme](https://github.com/alastairlundy/CliInvoke/blob/main/README.md)?

[![NuGet](https://img.shields.io/nuget/v/AlastairLundy.CliInvoke.Specializations.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Specializations/)
[![NuGet](https://img.shields.io/nuget/dt/AlastairLundy.CliInvoke.Specializations.svg)](https://www.nuget.org/packages/AlastairLundy.CliInvoke.Specializations/)

## Usage
CliInvoke.Specializations comes with 3 specializations as of 0.8.0: 
- [CmdCommandConfiguration](#cmdcommandconfiguration) - An easier way to execute processes and commands through cmd.exe (Only supported on Windows)
- [ClassicPowershellCommandConfiguration](#classicpowershellcommandconfiguration) - An easier way to execute processes and commands through Windows Powershell (Only supported on Windows)
- [PowershellCommandConfiguration](#powershellcommandconfiguration) - An easier way to execute processes and commands through the modern Cross-Platform open source Powershell (Powershell is not installed by CliInvoke and is expected to be installed if you plan to use it.)

All Command specialization classes come with an already configured TargetFilePath that points to the relevant executable.

### CmdCommandConfiguration
The CmdCommand's TargetFilePath points to Windows' copy of cmd.exe .

```csharp
using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Abstractions;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Builders.Abstractions;
using AlastairLundy.CliInvoke.Specializations.Configurations;
using AlastairLundy.CliInvoke.Specializations;

    // ServiceProvider and Dependency Injection code ommitted for clarity

  ICliCommandInvoker _commandInvoker = sp.GetRequiredService<ICliCommandInvoker>();

  //Build your command fluently
  ICliCommandConfigurationBuilder builder = new CliCommandConfigurationBuilder(
          new CmdCommandConfiguration("Your arguments go here"))
                .WithWorkingDirectory(Environment.SystemDirectory);
  
  CliCommandConfiguration commandConfig = builder.Build();
  
  var result = await _commandInvoker.ExecuteBufferedAsync(commandConfig);
```

If the result of the command being run is not of concern you can call ``ExecuteAsync()`` instead of ``ExecuteBufferedAsync()`` and ignore the returned CommandResult like so:
```csharp
using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Abstractions;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Builders.Abstractions;
using AlastairLundy.CliInvoke.Specializations.Configurations;

using AlastairLundy.CliInvoke.Specializations;

    // ServiceProvider and Dependency Injection code ommitted for clarity

  ICliCommandInvoker _commandInvoker = serviceProvider.GetRequiredService<ICliCommandInvoker>();

  //Build your command fluently
  ICliCommandConfigurationBuilder builder = new CliCommandConfigurationBuilder(
          new CmdCommandConfiguration("Your arguments go here"))
                .WithWorkingDirectory(Environment.SystemDirectory);
  
  CliCommandConfiguration commandConfig = builder.Build();
  
  var result = await _commandInvoker.ExecuteAsync(commandConfig);
```

### ClassicPowershellCommandConfiguration
The ClassicPowershellCommand is a specialized Command class with an already configured TargetFilePath that points to Windows' copy of powershell.exe .

```csharp
using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Abstractions;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Builders.Abstractions;
using AlastairLundy.CliInvoke.Specializations;

    // ServiceProvider and Dependency Injection code ommitted for clarity

  ICliCommandInvoker _commandInvoker = serviceProvider.GetRequiredService<ICliCommandInvoker>();

   //Build your command fluently
  ICliCommandConfigurationBuilder builder = new CliCommandConfigurationBuilder(
          new ClassicPowershellCommandConfiguration("Your arguments go here"))
                .WithWorkingDirectory(Environment.SystemDirectory);
  
  CliCommandConfiguration commandConfig = builder.Build();
  
 var result = await _commandInvoker.ExecuteBufferedAsync(commandConfig);
```

### PowershellCommandConfiguration
The PowershellCommand's TargetFilePath points to the installed copy of cross-platform Powershell if it is installed.

```csharp
using AlastairLundy.CliInvoke;
using AlastairLundy.CliInvoke.Abstractions;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Builders.Abstractions;
using AlastairLundy.CliInvoke.Specializations;

    // ServiceProvider and Dependency Injection code ommitted for clarity

  ICliCommandInvoker _commandInvoker = serviceProvider.GetRequiredService<ICliCommandInvoker>();

   //Build your command fluently
  ICliCommandConfigurationBuilder builder = new CliCommandConfigurationBuilder(
          new PowershellCommandConfiguration("Your arguments go here"))
                .WithWorkingDirectory(Environment.SystemDirectory);
  
  CliCommandConfiguration commandConfig = builder.Build();
  
 var result = await _commandInvoker.ExecuteBufferedAsync(commandConfig);
```

## Licensing
CliInvoke and CliInvoke Specializations are licensed under the MPL 2.0 license. If you modify any of CliInvoke's or CliInvoke.Specialization's files then the modified files must be licensed under the MPL 2.0 .

If you use CliInvoke or CliInvoke.Specializations in your project please make an exact copy of the contents of CliInvoke's LICENSE.txt file available either in your third party licenses txt file or as a separate txt file.
