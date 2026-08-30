/*
    CliInvoke.Extensibility
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Text;

using CliInvoke.Builders;
using CliInvoke.Core.Extensibility.Factories;

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

        string innerCommand;
        // If the runner process is PowerShell, invoke the target via the call operator (&) and quote the path.
        if (!string.IsNullOrEmpty(runnerProcessConfig.TargetFilePath) &&
            (runnerProcessConfig.TargetFilePath.IndexOf("pwsh", StringComparison.OrdinalIgnoreCase) >= 0 ||
             runnerProcessConfig.TargetFilePath.IndexOf("powershell", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            // The runner's own arguments (e.g. -NoProfile -NonInteractive -Command) stay as separate
            // tokens; only the dynamic target + target arguments are wrapped as a single safe token.
            string escapedTarget = processConfigToBeRun.TargetFilePath.Replace("'", "''");
            string targetExpression = $"& '{escapedTarget}'";
            string targetArguments = string.IsNullOrWhiteSpace(processConfigToBeRun.Arguments)
                ? string.Empty
                : processConfigToBeRun.Arguments;
            string commandBody = $"{targetExpression} {targetArguments}".Trim();
            string wrappedBody = MakeShellSafe(commandBody, true);

            innerCommand = string.IsNullOrWhiteSpace(runnerProcessConfig.Arguments)
                ? wrappedBody
                : $"{runnerProcessConfig.Arguments.Trim()} {wrappedBody}";
        }
        else
        {
            string targetArguments = string.IsNullOrWhiteSpace(processConfigToBeRun.Arguments)
                ? string.Empty
                : processConfigToBeRun.Arguments;
            string commandBody = $"{processConfigToBeRun.TargetFilePath} {targetArguments}".Trim();
            string wrappedBody = MakeShellSafe(commandBody, false);

            innerCommand = string.IsNullOrWhiteSpace(runnerProcessConfig.Arguments)
                ? wrappedBody
                : $"{runnerProcessConfig.Arguments.Trim()} {wrappedBody}";
        }

        IProcessConfigurationBuilder commandBuilder = new ProcessConfigurationBuilder(
                runnerProcessConfig.TargetFilePath
            )
            .SetArguments(innerCommand)
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
            commandBuilder = commandBuilder.RequireAdministratorPrivileges();

        return commandBuilder.Build();
    }

    /// <summary>
    /// Produces a shell-safe, single-token representation of a composed command so that the
    /// operating-system command-line parser cannot re-tokenize the inner contents and let the
    /// wrapped shell reassemble a second command.
    /// </summary>
    /// <param name="command">The composed command (runner arguments, target, and target arguments).</param>
    /// <param name="forPowerShell">True to wrap for a PowerShell <c>-Command</c> invocation; false for cmd <c>/c</c>.</param>
    /// <returns>A single double-quoted token safe to pass as the runner process' arguments.</returns>
    private static string MakeShellSafe(string command, bool forPowerShell)
    {
        if (string.IsNullOrWhiteSpace(command))
            return "\"\"";

        // Strip control characters that could otherwise break out of the quoting wrapper.
        StringBuilder sanitized = new(command.Length);
        foreach (char c in command)
        {
            if (c < 0x20 && c != '\t' && c != '\n' && c != '\r')
                continue;
            sanitized.Append(c);
        }

        if (forPowerShell)
        {
            // Escape any backticks, double quotes and dollar signs, then wrap in a double-quoted
            // literal so the OS parser passes the whole command to PowerShell as a single token.
            // Escaping '$' prevents $(...) subexpression execution inside the wrapper.
            string escaped = sanitized.ToString()
                .Replace("`", "``")
                .Replace("\"", "`\"")
                .Replace("$", "`$");
            return $"\"{escaped}\"";
        }

        // cmd: caret-escape the shell metacharacters (including '%' to block variable expansion),
        // then wrap the result in a single double-quoted token.
        StringBuilder escapedCmd = new(sanitized.Length + 8);
        foreach (char c in sanitized.ToString())
        {
            switch (c)
            {
                case '"': escapedCmd.Append("^\""); break;
                case '&': escapedCmd.Append("^&"); break;
                case '|': escapedCmd.Append("^|"); break;
                case '<': escapedCmd.Append("^<"); break;
                case '>': escapedCmd.Append("^>"); break;
                case '^': escapedCmd.Append("^^"); break;
                case '(': escapedCmd.Append("^("); break;
                case ')': escapedCmd.Append("^)"); break;
                case '%': escapedCmd.Append("^%"); break;
                default: escapedCmd.Append(c); break;
            }
        }

        return $"\"{escapedCmd}\"";
    }
}
