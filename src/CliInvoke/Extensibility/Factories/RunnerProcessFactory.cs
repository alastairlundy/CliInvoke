/*
    CliInvoke.Extensibility
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Builders;
using CliInvoke.Core.Extensibility.Factories;

namespace CliInvoke.Extensibility.Factories;

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
    public ProcessConfiguration CreateRunnerConfiguration(
        ProcessConfiguration processConfigToBeRun,
        ProcessConfiguration runnerProcessConfig
    )
    {
        ArgumentNullException.ThrowIfNull(processConfigToBeRun);
        ArgumentNullException.ThrowIfNull(runnerProcessConfig);
        
        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder(
            runnerProcessConfig.TargetFilePath
        )
            .SetArguments(
                processConfigToBeRun.TargetFilePath + " " + processConfigToBeRun.Arguments
            )
            .SetEnvironmentVariables(processConfigToBeRun.EnvironmentVariables)
            .SetProcessResourcePolicy(processConfigToBeRun.ResourcePolicy)
            .SetStandardInputEncoding(processConfigToBeRun.StandardInputEncoding)
            .SetStandardOutputEncoding(processConfigToBeRun.StandardOutputEncoding)
            .SetStandardErrorEncoding(processConfigToBeRun.StandardErrorEncoding)
            .SetStandardInputPipe(processConfigToBeRun.StandardInput ?? StreamWriter.Null)
            .SetStandardOutputPipe(processConfigToBeRun.StandardOutput ?? StreamReader.Null)
            .SetStandardErrorPipe(processConfigToBeRun.StandardError ?? StreamReader.Null)
            .SetUserCredential(processConfigToBeRun.Credential)
            .ConfigureShellExecution(processConfigToBeRun.UseShellExecution)
            .RedirectStandardInput(processConfigToBeRun.RedirectStandardInput)
            .RedirectStandardOutput(processConfigToBeRun.RedirectStandardOutput)
            .RedirectStandardError(processConfigToBeRun.RedirectStandardError)
            .ConfigureWindowCreation(processConfigToBeRun.WindowCreation);

        if (runnerProcessConfig.RequiresAdministrator)
            commandBuilder = new ProcessConfigurationBuilder(
                runnerProcessConfig.TargetFilePath
            ).RequireAdministratorPrivileges();

        return commandBuilder.Build();
    }
}
