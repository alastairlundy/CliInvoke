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
///         (<c>""</c>) inside a quoted token as a literal quote and gives backslashes
///         special meaning only immediately before a quote. So inner quotes are
///         doubled, runs of backslashes are doubled before an embedded quote and
///         before the closing quote, and bare newlines are dropped (they would
///         otherwise terminate the line). On Unix the inner content is escaped for the
///         double-quoted context the caller supplies: an embedded double quote is
///         escaped as <c>\"</c> (so it is not treated as a delimiter) while backslashes
///         are emitted literally — .NET's Unix argument parser treats a backslash as
///         literal except immediately before a double quote — and bare newlines are dropped.
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
            // written as "". Backslashes only gain special meaning immediately before a
            // quote, so a run of N backslashes is doubled to 2N before an embedded quote
            // and before the (implicit) closing quote the caller adds. Newlines would
            // terminate the command line, so they are dropped. Shell metacharacters are
            // intentionally left untouched here.
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
                    // Drop bare newlines so they cannot terminate the command line,
                    // flushing any preceding backslashes first.
                    builder.Append('\\', backslashCount);
                    backslashCount = 0;
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

        // POSIX: the caller supplies the surrounding double quotes (AddEnumerable always
        // wraps; Add wraps when NeedsQuoting). .NET's Unix argument parser (used when launching
        // the child) treats a backslash as literal except immediately before a double quote,
        // where it escapes the quote. So backslashes are emitted literally and only an embedded
        // double quote is escaped as \" so it is not treated as a delimiter. Bare newlines are
        // dropped. No outer single quotes are added here.
        StringBuilder posixBuilder = new(argument.Length + 8);

        foreach (char c in argument)
        {
            if (c == '"')
            {
                posixBuilder.Append('\\').Append('"');
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

        // On POSIX, EscapeInner returns content escaped for a double-quoted context
        // (backslashes and embedded double quotes are backslash-escaped; the caller adds
        // the surrounding double quotes). A value must still be wrapped when it contains
        // whitespace (otherwise the OS would split it into multiple tokens), a double
        // quote, a backslash, or a shell metacharacter that is literal inside double
        // quotes — this keeps the single-value Add path consistent with AddEnumerable's
        // double-quote wrapping.
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
