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
///     transforms is a strict subset of the shell-layer escaping set (e.g.
///     <c>ShellRewriter.EscapeForCmd</c>).
///     <para>
///         On Windows the C-runtime argument parser treats a doubled quote
///         (<c>""</c>) inside a quoted token as a literal quote and gives backslashes
///         special meaning only immediately before a quote. So inner quotes are
///         doubled, runs of backslashes are doubled before an embedded quote and
///         before the closing quote, and bare newlines are dropped (they would
///         otherwise terminate the line). .NET's Unix argument parser applies the same
///         rule for the double-quoted context the caller supplies, so the POSIX branch
///         uses identical escaping (doubled backslashes before an embedded quote and
///         before the closing quote, embedded quotes written as <c>""</c>) and drops
///         bare newlines.
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
            // Escaping rules for the Windows C-runtime parser: documented in the class remarks.
            StringBuilder builder = new(argument.Length + 8);

            int backslashCount = 0;

            foreach (char c in argument)
            {
                if (c == '\\')
                {
                    backslashCount++;
                }
                else if (c == '"')
                {
                    // Double the preceding backslashes, then emit a doubled quote.
                    builder.Append('\\', backslashCount * 2);
                    builder.Append('"').Append('"');
                    backslashCount = 0;
                }
                else if (c is '\n' or '\r')
                {
                    // Dropped, so they cannot terminate the command line. The pending backslash
                    // run is NOT flushed: a dropped newline is not content, so the run still
                    // applies to whatever follows.
                }
                else
                {
                    builder.Append('\\', backslashCount);
                    builder.Append(c);
                    backslashCount = 0;
                }
            }

            // Double any trailing backslashes for the closing quote.
            builder.Append('\\', backslashCount * 2);

            return builder.ToString();
        }

        // POSIX: the surrounding double quotes are the caller's job. .NET's Unix parser applies
        // the same backslash rule as the Windows C-runtime, so the inner escaping below is
        // identical to the Windows branch.
        StringBuilder posixBuilder = new(argument.Length + 8);

        int posixBackslashCount = 0;

        foreach (char c in argument)
        {
            if (c == '\\')
            {
                posixBackslashCount++;
            }
            else if (c == '"')
            {
                // Double the preceding backslashes, then emit a doubled quote.
                posixBuilder.Append('\\', posixBackslashCount * 2);
                posixBuilder.Append('"').Append('"');
                posixBackslashCount = 0;
            }
            else if (c is '\n' or '\r')
            {
                // Dropped; the pending backslash run carries across the drop, as in the
                // Windows branch above.
            }
            else
            {
                posixBuilder.Append('\\', posixBackslashCount);
                posixBuilder.Append(c);
                posixBackslashCount = 0;
            }
        }

        // Double any trailing backslashes for the closing quote.
        posixBuilder.Append('\\', posixBackslashCount * 2);

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

        // Same check as Windows: the escaped content is emitted in a double-quoted context,
        // so the same set of characters forces a wrap.
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
}
