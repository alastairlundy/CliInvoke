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
///     cmd/PowerShell specialisations. As a result the character set this escaper
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
        if (string.IsNullOrEmpty(argument))
            return string.Empty;
        
        return OperatingSystem.IsWindows() ? EscapeInnerWindows(argument) : EscapeInnerUnix(argument);
    }

    private static string EscapeInnerUnix(string argument)
    {
        // POSIX: the surrounding double quotes are the caller's job. .NET's Unix parser applies the same backslash
        // rule as the Windows C-runtime, so the inner escaping is currently identical to the Windows branch. The
        // two branches are kept separate so any future per-platform divergence lands in the right place.
        return EscapeInnerQuoted(argument);
    }

    private static string EscapeInnerWindows(string argument)
    {
        // Escaping rules for the Windows C-runtime parser: documented in the class remarks.
        return EscapeInnerQuoted(argument);
    }

    // Common implementation for the double-quoted context both branches target. Assumes the caller
    // supplies the surrounding quotes.
    private static string EscapeInnerQuoted(string argument)
    {
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
                // run is NOT flushed: a dropped newline is not content, so the run still applies to whatever follows.
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
        if (string.IsNullOrEmpty(argument))
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
