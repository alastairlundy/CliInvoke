/*
    CliInvoke.Extensibility
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using CliInvoke.Builders;
using CliInvoke.Core.Extensibility.Factories;
using CliInvoke.Helpers.Processes;

namespace CliInvoke.Extensibility.Factories;

/// <summary>
/// A class to allow creating a ProcessConfiguration that can be run through another Process' ProcessConfiguration.
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

        string combinedArgs = ComposeRunnerArguments(processConfigToBeRun, runnerProcessConfig);

        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder(
                runnerProcessConfig.TargetFilePath
            )
            .SetArguments(combinedArgs)
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

    /// <summary>
    /// Composes the argument string passed to the runner process so that the wrapped target and each of its
    /// arguments are delivered as discrete, correctly delimited tokens. This prevents a quote in the wrapped
    /// target or its arguments from breaking out of the intended token boundaries when the OS command-line
    /// parser re-tokenizes the runner's argument string.
    /// </summary>
    /// <param name="processConfigToBeRun">The command to be run by the runner process.</param>
    /// <param name="runnerProcessConfig">The runner process configuration.</param>
    /// <returns>The composed argument string for the runner process.</returns>
    private static string ComposeRunnerArguments(
        ProcessConfiguration processConfigToBeRun,
        ProcessConfiguration runnerProcessConfig)
    {
        string runnerArgs = runnerProcessConfig.Arguments ?? string.Empty;
        string target = processConfigToBeRun.TargetFilePath ?? string.Empty;
        string targetArgs = processConfigToBeRun.Arguments ?? string.Empty;

        IEnumerable<string> runnerTokens = ArgumentCompositionHelper.SplitArguments(runnerArgs);
        IEnumerable<string> targetArgTokens = ArgumentCompositionHelper.SplitArguments(targetArgs);

        List<string> segments = new();

        // Preserve any arguments the runner process itself declares (e.g. -Command, /c) verbatim.
        foreach (string token in runnerTokens)
            segments.Add(token);

        if (ArgumentCompositionHelper.IsPowerShell(runnerProcessConfig))
        {
            // Invoke the target via the call operator and keep its path in a quoted, escaped token.
            segments.Add("&");
            segments.Add(ArgumentCompositionHelper.QuoteArgument(target));
        }
        else
        {
            segments.Add(ArgumentCompositionHelper.QuoteArgument(target));
        }

        // The target path is treated as a single atomic token; only its arguments are tokenised,
        // and each resulting token is individually quoted/escaped so a quote or ampersand inside
        // cannot be re-interpreted as a command separator by the OS command-line parser.
        foreach (string token in targetArgTokens)
            segments.Add(ArgumentCompositionHelper.QuoteArgument(token));

        return string.Join(" ", segments);
    }
}
