/*
    CliInvoke.Extensibility
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO;

using AlastairLundy.CliInvoke.Builders;

using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Extensibility;

using AlastairLundy.CliInvoke.Core.Primitives;

// ReSharper disable MemberCanBePrivate.Global

namespace AlastairLundy.CliInvoke.Extensibility;

/// <summary>
/// A class to allow creating ProcessPrimitives that can be run through other ProcessPrimitives.
/// </summary>
public class RunnerProcessCreator : IRunnerProcessCreator
{
    private readonly ProcessConfiguration _commandRunnerConfiguration;
    
    /// <summary>
    /// Instantiates the Command Running configuration and the CommandRunner.
    /// </summary>
    /// <param name="commandRunnerConfiguration">The command running configuration to use for the Command that will run other Commands.</param>
    public RunnerProcessCreator(ProcessConfiguration commandRunnerConfiguration)
    {
        _commandRunnerConfiguration = commandRunnerConfiguration;
    }
    
    /// <summary>
    /// Create the command to be run from the Command runner configuration and an input command.
    /// </summary>
    /// <param name="inputProcess">The command to be run by the Command Runner command.</param>
    /// <returns>The Process Configuration to be run.</returns>
    public ProcessConfiguration CreateRunnerProcess(ProcessConfiguration inputProcess)
    {
        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder(_commandRunnerConfiguration)
            .WithArguments(inputProcess.TargetFilePath + " " + inputProcess.Arguments)
            .WithEnvironmentVariables(inputProcess.EnvironmentVariables)
            .WithProcessResourcePolicy(inputProcess.ResourcePolicy ?? ProcessResourcePolicy.Default)
            .WithEncoding(inputProcess.StandardInputEncoding,
                inputProcess.StandardOutputEncoding,
                inputProcess.StandardErrorEncoding)
            .WithStandardInputPipe(inputProcess.StandardInput ?? StreamWriter.Null)
            .WithStandardOutputPipe(inputProcess.StandardOutput ?? StreamReader.Null)
            .WithStandardErrorPipe(inputProcess.StandardError ?? StreamReader.Null)
            .WithUserCredential(inputProcess.Credential ?? UserCredential.Null)
            .WithAdministratorPrivileges(inputProcess.RequiresAdministrator)
            .WithShellExecution(inputProcess.UseShellExecution)
            .WithWindowCreation(inputProcess.WindowCreation);
        
        return commandBuilder.Build();
    }
}