/*
    CliInvoke.Extensibility
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

#pragma warning disable CA1416

using System.Collections.Generic;
using System.Linq;

using CliInvoke.Builders;
using CliInvoke.Core.Internal;

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

        // Compose the wrapped command as discrete tokens. Delivering the target and
        // the caller's arguments as separate tokens (rather than one re-parsed string)
        // means the operating system tokenises each value independently, so a quote or
        // other special character inside a caller-supplied value cannot alter how the
        // wrapped command is split.
        List<string> commandTokens = new();

        if (!string.IsNullOrWhiteSpace(runnerProcessConfig.Arguments))
            commandTokens.AddRange(ArgumentTokenizer.Tokenize(runnerProcessConfig.Arguments));

        // PowerShell requires the call operator (&) to invoke a target whose path is
        // quoted/contains spaces; cmd runs the target directly, so it is omitted there.
        bool runnerIsPowerShell =
            !string.IsNullOrEmpty(runnerProcessConfig.TargetFilePath)
            && (runnerProcessConfig.TargetFilePath.IndexOf("pwsh", StringComparison.OrdinalIgnoreCase) >= 0
                || runnerProcessConfig.TargetFilePath.IndexOf("powershell", StringComparison.OrdinalIgnoreCase) >= 0);

        if (runnerIsPowerShell)
            commandTokens.Add("&");

        // Pass the target path and its arguments as literal tokens.
        commandTokens.Add(processConfigToBeRun.TargetFilePath);

        if (!string.IsNullOrWhiteSpace(processConfigToBeRun.Arguments))
            commandTokens.AddRange(ArgumentTokenizer.Tokenize(processConfigToBeRun.Arguments));

        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder(
                runnerProcessConfig.TargetFilePath
            )
            .SetArguments(commandTokens, escapeArguments: false)
            .ConfigureEnvironmentVariables(envSpec =>
            {
                envSpec.SetReadOnlyDictionary(processConfigToBeRun.EnvironmentVariables);
            })
            .ConfigureProcessResourcePolicy(resourceSpec =>
            {
                resourceSpec.SetPriorityClass(processConfigToBeRun.ResourcePolicy.PriorityClass);
                
                resourceSpec.SetMinWorkingSet(processConfigToBeRun.ResourcePolicy.MinWorkingSet);
                resourceSpec.SetMaxWorkingSet(processConfigToBeRun.ResourcePolicy.MaxWorkingSet);

                resourceSpec.ConfigurePriorityBoost(processConfigToBeRun.ResourcePolicy
                    .EnablePriorityBoost);
                
                resourceSpec.SetProcessorAffinity(processConfigToBeRun.ResourcePolicy.ProcessorAffinity ??
                                                  (nint)ProcessResourcePolicy.Default.ProcessorAffinity);
            })
            .SetEncoding(processConfigToBeRun.StandardInputEncoding, processConfigToBeRun.StandardOutputEncoding, processConfigToBeRun.StandardErrorEncoding)
            .SetStandardInputPipe(processConfigToBeRun.StandardInput ?? StreamWriter.Null)
            .UseShellExecution(processConfigToBeRun.UseShellExecution)
            .EnableWindowCreation(processConfigToBeRun.WindowCreation);

        if (runnerProcessConfig.RequiresAdministrator)
            commandBuilder.RequireAdministratorPrivileges();

        ProcessConfiguration result = commandBuilder.Build();

        // Expose the pre-tokenized form so hosts can bypass OS-level re-parsing of the
        // combined argument string. Set it directly to preserve tokens that contain spaces.
        result.ArgumentsList = commandTokens;

        return result;
    }
}