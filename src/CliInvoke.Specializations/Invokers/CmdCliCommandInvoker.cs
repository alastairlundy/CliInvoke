/*
    CliInvoke Specializations
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Runtime.Versioning;
using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Abstractions;
using AlastairLundy.CliInvoke.Core.Abstractions.Builders;
using AlastairLundy.CliInvoke.Core.Primitives;

using AlastairLundy.CliInvoke.Extensibility.Abstractions.Invokers;

using AlastairLundy.CliInvoke.Specializations.Configurations;
using AlastairLundy.CliInvoke.Specializations.Internal.Localizations;

namespace AlastairLundy.CliInvoke.Specializations.Invokers;

/// <summary>
/// Run commands through CMD with ease.
/// </summary>
public class CmdCliCommandInvoker : SpecializedCliCommandInvoker, ISpecializedCliCommandInvoker
{
    
    /// <summary>
    /// Instantiates the Cmd Cli command invoker
    /// </summary>
    /// <remarks>Only supported on Windows based operating systems.</remarks>
    /// <param name="commandInvoker">The cli command invoker service to be used to run commands.</param>
    #if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("freebsd")]
    #endif
    public CmdCliCommandInvoker(ICliCommandInvoker commandInvoker) : base(commandInvoker, new CmdCommandConfiguration())
    {
        
    }

    public override CliCommandConfiguration CreateRunnerCommand(CliCommandConfiguration inputCommand)
    {
        ICliCommandConfigurationBuilder configurationBuilder =
            new CliCommandConfigurationBuilder(new CmdCommandConfiguration())
                .WithArguments($"{inputCommand.TargetFilePath} {inputCommand.Arguments}")
                .WithWorkingDirectory(inputCommand.WorkingDirectoryPath)
                .WithValidation(inputCommand.ResultValidation)
                .WithStandardInputPipe(inputCommand.StandardInput)
                .WithStandardOutputPipe(inputCommand.StandardOutput)
                .WithStandardErrorPipe(inputCommand.StandardError)
                .WithUserCredential(inputCommand.Credential)
                .WithWindowCreation(inputCommand.WindowCreation)
                .WithAdministratorPrivileges(inputCommand.RequiresAdministrator)
                .WithEncoding(inputCommand.StandardInputEncoding, inputCommand.StandardOutputEncoding,
                    inputCommand.StandardErrorEncoding)
                .WithEnvironmentVariables(inputCommand.EnvironmentVariables)
                .WithProcessResourcePolicy(inputCommand.ResourcePolicy);
        
        CliCommandConfiguration runnerCommand = configurationBuilder.Build();
        return runnerCommand;

    }
}