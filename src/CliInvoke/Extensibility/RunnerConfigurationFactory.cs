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

        // The runner is one of three categories, and each demands a different delivery
        // shape — the right choice depends on what the runner does with its arguments:
        //
        //   - PowerShell runner (-Command): argv to pwsh is
        //         [pwsh, -NoProfile, ..., -Command, <script>]
        //     The user-supplied target + args must be composed into ONE shell-escaped
        //     script token so pwsh sees a single argv entry after -Command. .NET's
        //     ProcessStartInfo.ArgumentList quoting matches CommandLineToArgvW rules
        //     that pwsh's argv parser also uses, so a single ArgumentList entry is the
        //     safe delivery shape.
        //
        //   - cmd.exe runner (/c): cmd's own command-line parser does NOT match
        //     CommandLineToArgvW quoting the way .NET applies to ArgumentList — the
        //     escaping .NET adds for embedded double quotes (backslash-escape) is
        //     NOT what cmd /c's quote-stripping rules expect, and benign targets end
        //     up unrunnable. The reliable delivery for cmd is therefore the legacy
        //     single-string Arguments, composed with the cmd escaper, which cmd's
        //     parser handles correctly.
        //
        //   - Non-shell runner (sudo, runas, etc.): the runner does not re-parse its
        //     arguments; it simply exec's the target. Each value is therefore passed
        //     through as a discrete token with no escaping required.
        bool runnerIsPowerShell =
            !string.IsNullOrEmpty(runnerProcessConfig.TargetFilePath)
            && (runnerProcessConfig.TargetFilePath.IndexOf("pwsh", StringComparison.OrdinalIgnoreCase) >= 0
                || runnerProcessConfig.TargetFilePath.IndexOf("powershell", StringComparison.OrdinalIgnoreCase) >= 0);

        bool runnerIsCmd =
            !runnerIsPowerShell
            && !string.IsNullOrEmpty(runnerProcessConfig.TargetFilePath)
            && runnerProcessConfig.TargetFilePath.IndexOf("cmd", StringComparison.OrdinalIgnoreCase) >= 0;

        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder(
                runnerProcessConfig.TargetFilePath
            );

        if (runnerIsPowerShell)
        {
            // pwsh -Command delivery via single ArgumentList entry containing the
            // shell-escaped script. .NET's ArgumentList quoting wraps the entry per
            // CommandLineToArgvW rules; pwsh's argv parser unquotes the same way, so
            // the script arrives at pwsh verbatim and is parsed once.
            IReadOnlyList<string> runnerArgs = !string.IsNullOrWhiteSpace(runnerProcessConfig.Arguments)
                ? ArgumentTokenizer.Tokenize(runnerProcessConfig.Arguments)
                : Array.Empty<string>();

            string safePath = ShellArgumentEscaper.EscapeForPowerShell(processConfigToBeRun.TargetFilePath);
            string safeArgs = ShellArgumentEscaper.EscapeForPowerShell(processConfigToBeRun.Arguments);
            string script = string.IsNullOrWhiteSpace(safeArgs)
                ? $"& \"{safePath}\""
                : $"& \"{safePath}\" {safeArgs}";

            List<string> argumentList = new(runnerArgs.Count + 1);
            argumentList.AddRange(runnerArgs);
            argumentList.Add(script);

            commandBuilder.SetArgumentList(argumentList);
            commandBuilder.SetArguments(string.Empty);
        }
        else if (runnerIsCmd)
        {
            // cmd /c delivery via single Arguments string composed with the cmd escaper.
            // .NET passes Arguments verbatim to the raw command line; cmd's parser applies
            // its own quote-stripping rules, which match the unquoted / cmd-escaped form
            // produced here.
            string safePath = ShellArgumentEscaper.EscapeForCmd(processConfigToBeRun.TargetFilePath);
            string safeArgs = ShellArgumentEscaper.EscapeForCmd(processConfigToBeRun.Arguments);
            string innerCommand = string.IsNullOrWhiteSpace(safeArgs)
                ? $"\"{safePath}\""
                : $"\"{safePath}\" {safeArgs}";

            string runnerArgs = runnerProcessConfig.Arguments ?? string.Empty;
            string arguments = string.IsNullOrWhiteSpace(runnerArgs)
                ? innerCommand
                : $"{runnerArgs} {innerCommand}";

            commandBuilder.SetArguments(arguments);
        }
        else
        {
            // Non-shell runner: each value delivered as a discrete token. No escape is
            // needed because the runner does not re-parse the command; it just exec's
            // the target.
            List<string> argumentList = new();

            if (!string.IsNullOrWhiteSpace(runnerProcessConfig.Arguments))
                argumentList.AddRange(ArgumentTokenizer.Tokenize(runnerProcessConfig.Arguments));

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

                resourceSpec.SetMinWorkingSet(processConfigToBeRun.ResourcePolicy.MinWorkingSet);
                resourceSpec.SetMaxWorkingSet(processConfigToBeRun.ResourcePolicy.MaxWorkingSet);

                resourceSpec.ConfigurePriorityBoost(processConfigToBeRun.ResourcePolicy
                    .EnablePriorityBoost);

                resourceSpec.SetProcessorAffinity(processConfigToBeRun.ResourcePolicy.ProcessorAffinity ??
                                                  (nint)ProcessResourcePolicy.Default.ProcessorAffinity);
            })
            .SetEncoding(processConfigToBeRun.StandardInputEncoding, processConfigToBeRun.StandardOutputEncoding, processConfigToBeRun.StandardErrorEncoding)
            .SetStandardInputPipe(processConfigToBeRun.StandardInput ?? StreamWriter.Null)
            .SetOutputRedirection(processConfigToBeRun.OutputRedirection)
            .UseShellExecution(processConfigToBeRun.UseShellExecution)
            .EnableWindowCreation(processConfigToBeRun.WindowCreation);

        if (runnerProcessConfig.RequiresAdministrator)
            commandBuilder.RequireAdministratorPrivileges();

        ProcessConfiguration result = commandBuilder.Build();

        // Mirror the pre-tokenized form onto the mutable ArgumentsList so consumers that
        // construct a ProcessConfiguration without going through the builder (and therefore
        // have no read-only ArgumentList) can still bypass OS-level re-parsing. The
        // adapter honours ArgumentsList as a fallback for exactly this reason.
        //
        // Only mirror when the canonical delivery is ArgumentList-based; mirroring the
        // single-string cmd delivery would force it back through ProcessStartInfo.ArgumentList
        // quoting and re-introduce the cmd.exe quoting mismatch this design is avoiding.
        if (result.ArgumentList.Count > 0)
            result.ArgumentsList = result.ArgumentList.ToList();

        return result;
    }
}
