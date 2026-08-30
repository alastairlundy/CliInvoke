/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Text;

namespace CliInvoke.Helpers.Processes;

/// <summary>
/// Helpers for safely composing the argument strings that runner processes
/// (such as PowerShell or the Windows command interpreter) receive, so that
/// paths and argument values are passed as discrete, correctly delimited tokens.
/// </summary>
internal static class ArgumentCompositionHelper
{
    /// <summary>
    /// Determines whether the supplied runner process is a Windows command interpreter (cmd).
    /// </summary>
    /// <param name="runnerProcessConfig">The runner process configuration to inspect.</param>
    /// <returns><see langword="true"/> if the runner is cmd; otherwise <see langword="false"/>.</returns>
    internal static bool IsCommandInterpreter(ProcessConfiguration runnerProcessConfig)
    {
        string? target = runnerProcessConfig.TargetFilePath;
        if (string.IsNullOrEmpty(target))
            return false;

        string fileName = Path.GetFileName(target).Trim();
        return fileName.Equals("cmd.exe", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("cmd", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the supplied runner process is a PowerShell host.
    /// </summary>
    /// <param name="runnerProcessConfig">The runner process configuration to inspect.</param>
    /// <returns><see langword="true"/> if the runner is PowerShell; otherwise <see langword="false"/>.</returns>
    internal static bool IsPowerShell(ProcessConfiguration runnerProcessConfig)
    {
        string? target = runnerProcessConfig.TargetFilePath;
        if (string.IsNullOrEmpty(target))
            return false;

        string fileName = Path.GetFileName(target).Trim();
        return fileName.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("powershell", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("pwsh.exe", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("pwsh", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Wraps a single argument value in OS-appropriate quoting so that it is preserved as one
    /// token by the command-line parser, escaping any characters that would otherwise terminate
    /// the quoted region.
    /// </summary>
    /// <param name="value">The argument value to quote and escape.</param>
    /// <returns>A quoted, escaped representation of <paramref name="value"/>.</returns>
    internal static string QuoteArgument(string value)
    {
        if (value is null)
            return string.Empty;

        if (value.Length == 0)
            return "\"\"";

        if (OperatingSystem.IsWindows())
            return QuoteWindowsArgument(value);

        return QuoteUnixArgument(value);
    }

    private static string QuoteWindowsArgument(string value)
    {
        // Backslash sequences preceding a quote must be doubled; the closing quote is escaped with a backslash.
        StringBuilder builder = new(value.Length + 2);
        builder.Append('"');

        int slashCount = 0;
        foreach (char c in value)
        {
            if (c == '\\')
            {
                slashCount++;
                continue;
            }

            if (c == '"')
            {
                for (int i = 0; i < slashCount; i++)
                    builder.Append("\\\\");

                builder.Append("\\\"");
                slashCount = 0;
                continue;
            }

            for (int i = 0; i < slashCount; i++)
                builder.Append('\\');

            slashCount = 0;
            builder.Append(c);
        }

        for (int i = 0; i < slashCount; i++)
            builder.Append("\\\\");

        builder.Append('"');
        return builder.ToString();
    }

    private static string QuoteUnixArgument(string value)
    {
        if (!value.Contains('\'') && !value.Contains('"') && !value.Contains('\\') &&
            !value.Contains(' ') && !value.Contains('\t'))
            return value;

        // Use single quotes where possible; escape embedded single quotes per POSIX shell rules.
        if (!value.Contains('\''))
            return $"'{value}'";

        StringBuilder builder = new(value.Length + 2);
        builder.Append('"');
        foreach (char c in value)
        {
            if (c is '"' or '\\' or '$' or '`')
                builder.Append('\\');

            builder.Append(c);
        }

        builder.Append('"');
        return builder.ToString();
    }

    /// <summary>
    /// Splits a raw argument string into individual tokens, using the same whitespace
    /// rules as the OS command-line parser, so each token can be independently quoted.
    /// </summary>
    /// <param name="arguments">The raw argument string to split.</param>
    /// <returns>The individual argument tokens.</returns>
    internal static IEnumerable<string> SplitArguments(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            yield break;

        if (OperatingSystem.IsWindows())
        {
            foreach (string token in SplitWindowsArguments(arguments!))
                yield return token;
        }
        else
        {
            foreach (string token in SplitUnixArguments(arguments!))
                yield return token;
        }
    }

    private static IEnumerable<string> SplitWindowsArguments(string arguments)
    {
        StringBuilder current = new();
        bool inQuotes = false;
        bool hasToken = false;

        for (int i = 0; i < arguments.Length; i++)
        {
            char c = arguments[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                hasToken = true;
                continue;
            }

            if (!inQuotes && char.IsWhiteSpace(c))
            {
                if (hasToken)
                {
                    yield return current.ToString();
                    current.Clear();
                    hasToken = false;
                }

                continue;
            }

            current.Append(c);
            hasToken = true;
        }

        if (hasToken)
            yield return current.ToString();
    }

    private static IEnumerable<string> SplitUnixArguments(string arguments)
    {
        StringBuilder current = new();
        bool inSingle = false;
        bool inDouble = false;
        bool hasToken = false;

        for (int i = 0; i < arguments.Length; i++)
        {
            char c = arguments[i];

            if (c == '\'' && !inDouble)
            {
                inSingle = !inSingle;
                hasToken = true;
                continue;
            }

            if (c == '"' && !inSingle)
            {
                inDouble = !inDouble;
                hasToken = true;
                continue;
            }

            if (!inSingle && !inDouble && char.IsWhiteSpace(c))
            {
                if (hasToken)
                {
                    yield return current.ToString();
                    current.Clear();
                    hasToken = false;
                }

                continue;
            }

            current.Append(c);
            hasToken = true;
        }

        if (hasToken)
            yield return current.ToString();
    }
}
