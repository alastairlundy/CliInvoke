/*
    CliInvoke.Extensibility
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */


using CliInvoke.Builders;

namespace CliInvoke.Extensibility;

/// <summary>
///     Creates a <see cref="ProcessConfiguration"/> that wraps another process's configuration
///     for runner-based execution.
/// </summary>
public class RunnerConfigurationFactory : IRunnerConfigurationFactory
{
    /// <summary>
    ///     Create the command to be run from the Command runner configuration and an input command.
    /// </summary>
    /// <param name="processConfigToBeRun">The command to be run by the Command Runner command.</param>
    /// <param name="runnerProcessConfig">The configuration of the runner that will execute the target command.</param>
    /// <returns>The configuration the runner should be invoked with.</returns>
    [Pure]
    public ProcessConfiguration CreateRunnerConfiguration(
        ProcessConfiguration processConfigToBeRun,
        ProcessConfiguration runnerProcessConfig)
    {
        ArgumentNullException.ThrowIfNull(processConfigToBeRun);
        ArgumentNullException.ThrowIfNull(runnerProcessConfig);

        /*
            Three runner categories, three delivery shapes:
              - PowerShell (-Command): ShellRewriter composes target + args into one shell-escaped script, delivered as a single ArgumentList entry.
              - cmd (/c): cmd's quote-stripping doesn't match .NET's ArgumentList escaping, so this delivers the legacy Arguments string (cmd escaper).
              - Non-shell (sudo, runas): Discrete tokens are handled without escaping, the runner executes the target as-is. 
              Dash-prefixed targets get a "--" terminator first so option-parsing runners (POSIX/Windows sudo) don't
                bind them as options; the terminator is conditional on a "-" prefix because positional program slots (runas) must not receive one.
        */
        bool runnerIsPowerShell =
            !string.IsNullOrEmpty(runnerProcessConfig.TargetFilePath)
            && (runnerProcessConfig.TargetFilePath.Contains("pwsh", StringComparison.OrdinalIgnoreCase)
                || runnerProcessConfig.TargetFilePath.Contains("powershell", StringComparison.OrdinalIgnoreCase));

        bool runnerIsCmd =
            !runnerIsPowerShell
            && !string.IsNullOrEmpty(runnerProcessConfig.TargetFilePath)
            && runnerProcessConfig.TargetFilePath.Contains("cmd", StringComparison.OrdinalIgnoreCase);

        ProcessConfigurationBuilder commandBuilder = new(runnerProcessConfig.TargetFilePath);

        if (runnerIsPowerShell)
        {
            // ShellRewriter composes the script; delivery is a single ArgumentList entry.
            ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                processConfigToBeRun,
                runnerProcessConfig.TargetFilePath,
                runnerProcessConfig.Arguments,
                ShellKind.PowerShell,
                windowCreation: processConfigToBeRun.WindowCreation,
                useShellExecution: processConfigToBeRun.UseShellExecution);

            commandBuilder.SetArgumentList(rewritten.ArgumentList);
            commandBuilder.SetArguments(string.Empty);
        }
        else if (runnerIsCmd)
        {
            // ShellRewriter composes the command; delivery is the legacy Arguments string.
            ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                processConfigToBeRun,
                runnerProcessConfig.TargetFilePath,
                runnerProcessConfig.Arguments,
                ShellKind.Cmd,
                windowCreation: processConfigToBeRun.WindowCreation,
                useShellExecution: processConfigToBeRun.UseShellExecution);

            commandBuilder.SetArguments(rewritten.Arguments);
        }
        else
        {
            // Discrete tokens: the runner executes the target without re-parsing.
            List<string> argumentList = [];

            if (!string.IsNullOrWhiteSpace(runnerProcessConfig.Arguments))
                argumentList.AddRange(ArgumentTokenizer.Tokenize(runnerProcessConfig.Arguments));

            // End the option zone before a dash-prefixed target (POSIX/Windows sudo).
            if (processConfigToBeRun.TargetFilePath.StartsWith('-', StringComparison.OrdinalIgnoreCase))
                argumentList.Add("--");

            argumentList.Add(processConfigToBeRun.TargetFilePath);
            if (!string.IsNullOrWhiteSpace(processConfigToBeRun.Arguments))
                argumentList.AddRange(ArgumentTokenizer.Tokenize(processConfigToBeRun.Arguments));

            commandBuilder.SetArgumentList(argumentList);
            commandBuilder.SetArguments(string.Empty);
        }

        commandBuilder
            .ConfigureEnvironmentVariables(envSpec =>
            {
                envSpec.SetReadOnlyDictionary(processConfigToBeRun.EnvironmentVariables);
            })
            .ConfigureProcessResourcePolicy(resourceSpec =>
            {
                resourceSpec.SetPriorityClass(processConfigToBeRun.ResourcePolicy.PriorityClass);

                if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ||
                    OperatingSystem.IsFreeBSD())
                {
                    resourceSpec.SetMinWorkingSet(processConfigToBeRun.ResourcePolicy.MinWorkingSet);
                    resourceSpec.SetMaxWorkingSet(processConfigToBeRun.ResourcePolicy.MaxWorkingSet);
                }

                if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
                {
                    resourceSpec.SetProcessorAffinity(processConfigToBeRun.ResourcePolicy.ProcessorAffinity ??
                                                      ProcessResourcePolicy.DefaultProcessorAffinity);
                }

                resourceSpec.ConfigurePriorityBoost(processConfigToBeRun.ResourcePolicy
                    .EnablePriorityBoost);
            })
            .SetEncoding(processConfigToBeRun.StandardInputEncoding, processConfigToBeRun.StandardOutputEncoding, processConfigToBeRun.StandardErrorEncoding)
            .SetStandardInputPipe(processConfigToBeRun.StandardInput ?? StreamWriter.Null)
            .SetOutputRedirection(processConfigToBeRun.OutputRedirection)
            .UseShellExecution(processConfigToBeRun.UseShellExecution)
            .EnableWindowCreation(processConfigToBeRun.WindowCreation);

        if (runnerProcessConfig.RequiresAdministrator)
            commandBuilder.RequireAdministratorPrivileges();

        ProcessConfiguration result = commandBuilder.Build();

        return result;
    }
}