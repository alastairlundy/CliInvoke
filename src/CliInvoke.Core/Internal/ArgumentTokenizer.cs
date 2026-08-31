/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Text;

namespace CliInvoke.Core.Internal;

/// <summary>
///     Splits a single command-line argument string into discrete tokens using the
///     standard Windows/Unix whitespace-and-quote rules.
/// </summary>
/// <remarks>
///     The <c>RunnerConfigurationFactory</c> composes a wrapped command from the caller's
///     target and arguments. Rather than re-parsing one combined string at the OS
///     layer (where a stray quote can break tokenisation), the values are split once
///     here into separate tokens so each can be passed to the operating system
///     independently and unambiguously.
/// </remarks>
internal static class ArgumentTokenizer
{
    /// <summary>
    ///     Splits <paramref name="value" /> into tokens, honouring double-quoted spans
    ///     (a pair of double-quotes inside a quoted span is an escaped literal quote).
    /// </summary>
    /// <param name="value">The raw argument string to split.</param>
    /// <returns>The tokenised values; empty when <paramref name="value" /> is empty.</returns>
    internal static IReadOnlyList<string> Tokenize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        string text = value!;

        List<string> tokens = new();
        StringBuilder current = new();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '"')
            {
                // Inside a quoted span, two adjacent quotes represent a single
                // literal quote rather than the end of the span.
                if (inQuotes
                    && i + 1 < text.Length
                    && text[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && char.IsWhiteSpace(c))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens;
    }
}
