/*
    CliInvoke.Extensibility
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

#pragma warning disable CA1416

using CliInvoke.Builders;
using CliInvoke.Core.Configuration;
using CliInvoke.Core.Extensibility;

namespace CliInvoke.Extensibility;

/// <summary>
///     A class to allow creating a ProcessConfiguration that can be run through another Process'
///     ProcessConfiguration.
/// </summary>
public class RunnerConfigurationFactory : IRunnerConfigurationFactory
{
    /// <summary>
    ///     Create the command to be run from the Command runner configuration and an input command.
    /// </summary>
    /// <param name="processConfigToBeRun">The command to be run by the Command Runner command.</param>
    /// <param name="runnerProcessConfig"></param>
    /// <returns></returns>
    [Pure]
    public ProcessConfiguration CreateRunnerConfiguration(
        ProcessConfiguration processConfigToBeRun,
        ProcessConfiguration runnerProcessConfig)
    {
        ArgumentNullException.ThrowIfNull(processConfigToBeRun);
        ArgumentNullException.ThrowIfNull(runnerProcessConfig);

        string combinedArgs =
            $"{runnerProcessConfig.Arguments} {processConfigToBeRun.TargetFilePath} {processConfigToBeRun.Arguments}"
                .Trim();

        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder(
                runnerProcessConfig.TargetFilePath
            )
            .SetArguments(combinedArgs)
            .ConfigureEnvironmentVariables(envSpec =>
            {
                envSpec.SetReadOnlyDictionary(processConfigToBeRun.EnvironmentVariables);
            })
            .ConfigureProcessResourcePolicy(resourceSpec =>
            {
                resourceSpec.SetPriorityClass(processConfigToBeRun.ResourcePolicy.PriorityClass);
                
                if(processConfigToBeRun.ResourcePolicy.MinWorkingSet is not null && processConfigToBeRun.ResourcePolicy.MaxWorkingSet is not null)
                    resourceSpec.SetWorkingSet((nint)processConfigToBeRun.ResourcePolicy.MinWorkingSet, (nint)processConfigToBeRun.ResourcePolicy.MaxWorkingSet);

                resourceSpec.ConfigurePriorityBoost(processConfigToBeRun.ResourcePolicy
                    .EnablePriorityBoost);
                
                if(processConfigToBeRun.ResourcePolicy.ProcessorAffinity is not null)
                    resourceSpec.SetProcessorAffinity((nint)processConfigToBeRun.ResourcePolicy.ProcessorAffinity);
            })
            .SetEncoding(processConfigToBeRun.StandardInputEncoding, processConfigToBeRun.StandardOutputEncoding, processConfigToBeRun.StandardErrorEncoding)
            .SetStandardInputPipe(processConfigToBeRun.StandardInput ?? StreamWriter.Null)
            .UseShellExecution(processConfigToBeRun.UseShellExecution)
            .EnableWindowCreation(processConfigToBeRun.WindowCreation);

        if (runnerProcessConfig.RequiresAdministrator)
            commandBuilder = new ProcessConfigurationBuilder(
                runnerProcessConfig.TargetFilePath
            ).RequireAdministratorPrivileges();

        return commandBuilder.Build();
    }
}