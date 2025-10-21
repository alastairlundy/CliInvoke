/*
    CliInvoke.Extensibility
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO;

using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Builders;

using AlastairLundy.CliInvoke.Core.Extensibility.Factories;

// ReSharper disable MemberCanBePrivate.Global

namespace AlastairLundy.CliInvoke.Extensibility;

/// <summary>
/// A class to allow creating a ProcessConfiguration that can be run through other Process' ProcessConfiguration.
/// </summary>
public class RunnerProcessFactory : IRunnerProcessFactory
{

    /// <summary>
    /// Create the command to be run from the Command runner configuration and an input command.
    /// </summary>
    /// <param name="processConfigToBeRun">The command to be run by the Command Runner command.</param>
    /// <param name="runnerProcessConfig"></param>
    /// <returns></returns>
    public ProcessConfiguration CreateRunnerConfiguration(ProcessConfiguration processConfigToBeRun,
        ProcessConfiguration runnerProcessConfig)
    {
        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder(runnerProcessConfig.TargetFilePath)
            .WithArguments(processConfigToBeRun.TargetFilePath + " " + processConfigToBeRun.Arguments)
            .WithEnvironmentVariables(processConfigToBeRun.EnvironmentVariables)
            .WithProcessResourcePolicy(processConfigToBeRun.ResourcePolicy)
            .WithStandardInputEncoding(processConfigToBeRun.StandardInputEncoding)
            .WithStandardOutputEncoding(processConfigToBeRun.StandardOutputEncoding)
            .WithStandardErrorEncoding(processConfigToBeRun.StandardErrorEncoding)
            .WithStandardInputPipe(processConfigToBeRun.StandardInput ?? StreamWriter.Null)
            .WithStandardOutputPipe(processConfigToBeRun.StandardOutput ?? StreamReader.Null)
            .WithStandardErrorPipe(processConfigToBeRun.StandardError ?? StreamReader.Null)
            .WithUserCredential(processConfigToBeRun.Credential)
            .WithAdministratorPrivileges(runnerProcessConfig.RequiresAdministrator)
            .WithShellExecution(processConfigToBeRun.UseShellExecution)
            .RedirectStandardInput(processConfigToBeRun.RedirectStandardInput)
            .RedirectStandardOutput(processConfigToBeRun.RedirectStandardOutput)
            .RedirectStandardError(processConfigToBeRun.RedirectStandardError)
            .WithWindowCreation(processConfigToBeRun.WindowCreation);
        
        return commandBuilder.Build();
    }
}