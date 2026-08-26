/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Text;

namespace CliInvoke.Core.Internal;

/// <summary>
///     Escapes argument strings so they are passed to a wrapped shell command as
///     literal data rather than being re-interpreted as shell syntax.
/// </summary>
/// <remarks>
///     The <c>Cmd</c> and <c>PowerShell</c> specializations rebuild the caller's
///     <c>TargetFilePath</c> + <c>Arguments</c> into a single shell command line
///     (e.g. <c>cmd /c "target" args</c> or
///     <c>pwsh -Command &amp; "target" args</c>). The shell then re-parses that line,
///     so any shell metacharacters in the original arguments would execute as
///     additional shell commands (command injection). These helpers neutralise the
///     dangerous metacharacters for each shell before the command is rebuilt.
/// </remarks>
internal static class ShellArgumentEscaper
{
    /// <summary>
    ///     Escapes a value so it is treated as literal data inside a PowerShell
    ///     <c>-Command</c> string. Prevents command chaining (<c>;</c>, <c>|</c>,
    ///     <c>&amp;</c>), subexpression/ variable expansion (<c>$(...)</c>, <c>$var</c>),
    ///     grouping, redirection, and quoting escapes.
    /// </summary>
    /// <param name="value">The raw argument value to escape.</param>
    /// <returns>The escaped value, safe to embed in a PowerShell command.</returns>
    public static string EscapeForPowerShell(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        StringBuilder builder = new(value.Length + 16);

        foreach (char c in value)
        {
            switch (c)
            {
                // PowerShell escape character is the backtick.
                case '`':
                case '$':
                case ';':
                case '|':
                case '&':
                case '(':
                case ')':
                case '{':
                case '}':
                case '<':
                case '>':
                case '"':
                case '\'':
                    builder.Append('`').Append(c);
                    break;
                case '\n':
                    builder.Append('`').Append('n');
                    break;
                case '\r':
                    // Drop bare carriage returns; they would otherwise terminate the line.
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    ///     Escapes a value so it is treated as literal data on the
    ///     <c>cmd.exe /c</c> command line. Prevents command chaining
    ///     (<c>&amp;</c>, <c>|</c>), redirection (<c>&lt;</c>, <c>&gt;</c>),
    ///     environment-variable expansion (<c>%VAR%</c>), and quoting breaks.
    /// </summary>
    /// <param name="value">The raw argument value to escape.</param>
    /// <returns>The escaped value, safe to embed in a cmd command.</returns>
    public static string EscapeForCmd(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        StringBuilder builder = new(value.Length + 16);

        foreach (char c in value)
        {
            switch (c)
            {
                // cmd.exe escape character is the caret.
                case '^':
                case '&':
                case '|':
                case '<':
                case '>':
                case '%':
                    builder.Append('^').Append(c);
                    break;
                case '"':
                    // A literal quote inside a quoted cmd token is written as "".
                    builder.Append('"').Append('"');
                    break;
                case '\n':
                case '\r':
                    // A bare newline would terminate the cmd command; drop it.
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        return builder.ToString();
    }
}
