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
    /// <param name="processConfigTarget">The command to be run by the Command Runner command.</param>
    /// <param name="runnerProcessConfig">The configuration of the runner that will execute the target command.</param>
    /// <returns>The configuration the runner should be invoked with.</returns>
    /// <remarks>
    ///     <para>
    ///         Runners are identified by file name. Invoke shell runners by their canonical
    ///         names (<c>cmd.exe</c>, <c>pwsh</c>/<c>powershell</c>). A renamed shell binary is
    ///         treated as an ordinary target and receives tokenized delivery; shell-specific
    ///         composition and its validation do not apply.
    ///     </para>
    /// </remarks>
    [Pure]
    public ProcessConfiguration CreateRunnerConfiguration(
        ProcessConfiguration processConfigTarget, ProcessConfiguration runnerProcessConfig)
    {
        ArgumentNullException.ThrowIfNull(processConfigTarget);
        ArgumentNullException.ThrowIfNull(runnerProcessConfig);

        // cmd.exe parses the whole tail of its /c command line with its own rules, and
        // batch files reparse their arguments the same way: separators, redirection,
        // escape and expansion characters keep their cmd.exe meaning even when they
        // arrive inside a discrete argument token, so launch-time quoting cannot keep
        // them literal. The tokens that cmd.exe will parse are validated here; values
        // containing those characters are rejected rather than escaped, and callers who
        // own their quoting can compose a raw command line through
        // CmdProcessConfiguration.
        if (CmdCompositionArgumentValidator.IsCmdRunner(runnerProcessConfig.TargetFilePath))
        {
            // cmd.exe parses its whole /c tail: the runner arguments, the target path
            // and the wrapped arguments are all validated. The runner and legacy
            // argument strings flow into the composed command line as whole spans, so
            // they are validated as whole spans — tokenising first would drop quote
            // characters that still reach cmd.exe's parse.
            if (!string.IsNullOrWhiteSpace(runnerProcessConfig.Arguments))
            {
                CmdCompositionArgumentValidator.Validate(
                    [runnerProcessConfig.Arguments],
                    nameof(runnerProcessConfig.Arguments));
            }

            CmdCompositionArgumentValidator.ValidateCompositionSource(processConfigTarget);
        }
        else if (CmdCompositionArgumentValidator.IsCmdOrBatchTarget(
                     processConfigTarget.TargetFilePath))
        {
            // Only the wrapped arguments are re-parsed by cmd.exe/batch rules here;
            // the runner parses its own arguments before the target is reached. This
            // composition delivers tokenised, so the tokens are validated.
            CmdCompositionArgumentValidator.Validate(
                TokenisedWrappedArguments(processConfigTarget),
                nameof(processConfigTarget.Arguments));
        }

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

        bool runnerIsCmd = !runnerIsPowerShell && CmdCompositionArgumentValidator.IsCmdRunner(runnerProcessConfig.TargetFilePath);

        ProcessConfigurationBuilder commandBuilder = new(runnerProcessConfig.TargetFilePath);

        if (runnerIsPowerShell)
        {
            // ShellRewriter composes the script; delivery is a single ArgumentList entry.
            ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                processConfigTarget,
                runnerProcessConfig.TargetFilePath,
                runnerProcessConfig.Arguments,
                ShellKind.PowerShell,
                windowCreation: processConfigTarget.WindowCreation,
                useShellExecution: processConfigTarget.UseShellExecution);

            commandBuilder.SetArgumentList(rewritten.ArgumentList);
            commandBuilder.SetArguments(string.Empty);
        }
        else if (runnerIsCmd)
        {
            // ShellRewriter composes the command; delivery is the legacy Arguments string.
            ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                processConfigTarget,
                runnerProcessConfig.TargetFilePath,
                runnerProcessConfig.Arguments,
                ShellKind.Cmd,
                windowCreation: processConfigTarget.WindowCreation,
                useShellExecution: processConfigTarget.UseShellExecution);

            commandBuilder.SetArguments(rewritten.Arguments);
        }
        else
        {
            // Discrete tokens: the runner executes the target without re-parsing.
            List<string> argumentList = [];

            if (!string.IsNullOrWhiteSpace(runnerProcessConfig.Arguments))
                argumentList.AddRange(ArgumentTokenizer.Tokenize(runnerProcessConfig.Arguments));

            // End the option zone before a dash-prefixed target (POSIX/Windows sudo).
            if (processConfigTarget.TargetFilePath.StartsWith('-', StringComparison.OrdinalIgnoreCase))
                argumentList.Add("--");

            argumentList.Add(processConfigTarget.TargetFilePath);
            if (!string.IsNullOrWhiteSpace(processConfigTarget.Arguments))
                argumentList.AddRange(ArgumentTokenizer.Tokenize(processConfigTarget.Arguments));

            commandBuilder.SetArgumentList(argumentList);
            commandBuilder.SetArguments(string.Empty);
        }

        commandBuilder
            .ConfigureEnvironmentVariables(envSpec =>
            {
                envSpec.SetReadOnlyDictionary(processConfigTarget.EnvironmentVariables);
            })
            .ConfigureProcessResourcePolicy(resourceSpec =>
            {
                resourceSpec.SetPriorityClass(processConfigTarget.ResourcePolicy.PriorityClass);

                if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ||
                    OperatingSystem.IsFreeBSD())
                {
                    resourceSpec.SetMinWorkingSet(processConfigTarget.ResourcePolicy.MinWorkingSet);
                    resourceSpec.SetMaxWorkingSet(processConfigTarget.ResourcePolicy.MaxWorkingSet);
                }

                if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
                {
                    resourceSpec.SetProcessorAffinity(processConfigTarget.ResourcePolicy.ProcessorAffinity ??
                                                      ProcessResourcePolicy.DefaultProcessorAffinity);
                }

                resourceSpec.ConfigurePriorityBoost(processConfigTarget.ResourcePolicy
                    .EnablePriorityBoost);
            })
            .SetEncoding(processConfigTarget.StandardInputEncoding, processConfigTarget.StandardOutputEncoding, processConfigTarget.StandardErrorEncoding)
            .SetStandardInputPipe(processConfigTarget.StandardInput ?? StreamWriter.Null)
            .SetOutputRedirection(processConfigTarget.OutputRedirection)
            .UseShellExecution(processConfigTarget.UseShellExecution)
            .EnableWindowCreation(processConfigTarget.WindowCreation);

        if (runnerProcessConfig.RequiresAdministrator)
            commandBuilder.RequireAdministratorPrivileges();

        ProcessConfiguration result = commandBuilder.Build();

        return result;
    }

    /// <summary>
    ///     The wrapped command's arguments in the shape the non-shell composition
    ///     delivers them. The verbatim <see cref="ProcessConfiguration.ArgumentList"/>
    ///     entries are used when present. Otherwise, the legacy
    ///     <see cref="ProcessConfiguration.Arguments"/> string is split into discrete
    ///     tokens.
    /// </summary>
    /// <param name="configuration">The wrapped command configuration.</param>
    /// <returns>The argument tokens the non-shell composition will deliver.</returns>
    private static IEnumerable<string> TokenisedWrappedArguments(ProcessConfiguration configuration)
    {
        if (configuration.ArgumentList.Count > 0)
            return configuration.ArgumentList;

        if (string.IsNullOrWhiteSpace(configuration.Arguments))
            return [];

        return ArgumentTokenizer.Tokenize(configuration.Arguments);
    }
}