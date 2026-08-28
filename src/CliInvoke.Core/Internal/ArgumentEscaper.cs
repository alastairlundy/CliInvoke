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
///     A platform-aware argument escaper for general command-line argument building.
/// </summary>
/// <remarks>
///     This escaper only performs the minimal, platform-appropriate quoting for the
///     underlying C-runtime / POSIX shell argument parser. It deliberately does NOT
///     neutralise shell metacharacters (<c>&amp;</c>, <c>|</c>, <c>&lt;</c>, <c>&gt;</c>,
///     <c>%</c>, <c>^</c>); that is the shell layer's concern and is handled by the
///     cmd/PowerShell specializations. As a result the character set this escaper
///     transforms is a strict subset of <c>ShellArgumentEscaper.EscapeForCmd</c>'s set.
///     <para>
///         On Windows the C-runtime argument parser treats a doubled quote
///         (<c>""</c>) inside a quoted token as a literal quote, so inner quotes are
///         doubled and bare newlines are dropped (they would otherwise terminate the
///         line). On Unix the value is wrapped in single quotes, escaping any embedded
///         single quote as the <c>'\''</c> sequence.
///     </para>
/// </remarks>
internal static class ArgumentEscaper
{
    /// <summary>
    ///     Escapes the inner content of an argument for the current operating system,
    ///     without adding any outer wrapping.
    /// </summary>
    /// <param name="argument">The raw argument value to escape.</param>
    /// <returns>The escaped inner content.</returns>
    public static string EscapeInner(string? argument)
    {
        if (argument is null)
            return string.Empty;

        if (argument.Length == 0)
            return string.Empty;

        if (OperatingSystem.IsWindows())
        {
            // Windows C-runtime argument parsing: a literal " inside a quoted token is
            // written as "". Newlines would terminate the command line, so they are
            // dropped. Shell metacharacters are intentionally left untouched here.
            StringBuilder builder = new(argument.Length + 8);

            foreach (char c in argument)
            {
                if (c == '"')
                {
                    builder.Append('"').Append('"');
                }
                else if (c is '\n' or '\r')
                {
                    // Drop bare newlines so they cannot terminate the command line.
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        // POSIX shell: wrap the whole value in single quotes, escaping embedded
        // single quotes as the '\'' sequence.
        StringBuilder posixBuilder = new(argument.Length + 8);
        posixBuilder.Append('\'');

        foreach (char c in argument)
        {
            if (c == '\'')
            {
                posixBuilder.Append("'\\''");
            }
            else if (c is '\n' or '\r')
            {
                // Drop bare newlines.
            }
            else
            {
                posixBuilder.Append(c);
            }
        }

        posixBuilder.Append('\'');
        return posixBuilder.ToString();
    }

    /// <summary>
    ///     Determines whether <paramref name="argument"/> must be wrapped in double
    ///     quotes when emitted on the current operating system.
    /// </summary>
    /// <param name="argument">The argument value to inspect.</param>
    /// <returns>
    ///     <see langword="true"/> if the argument contains characters that require
    ///     double-quote wrapping on the current OS; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool NeedsQuoting(string? argument)
    {
        if (argument is null || argument.Length == 0)
            return false;

        if (OperatingSystem.IsWindows())
        {
            // The C-runtime argument parser treats whitespace, quotes, backslashes and
            // shell metacharacters as significant, so such values must be quoted.
            foreach (char c in argument)
            {
                if (char.IsWhiteSpace(c)
                    || c is '"' or '\\'
                    || c is '&' or '|' or '<' or '>' or '^' or '%')
                {
                    return true;
                }
            }

            return false;
        }

        // On POSIX the inner value is already single-quoted by EscapeInner, so no
        // additional outer double-quote wrapping is required.
        return false;
    }
}
