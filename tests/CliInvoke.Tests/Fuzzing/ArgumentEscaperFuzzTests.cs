/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Linq;
using CliInvoke.Core.Internal;
using FsCheck;
using FsCheck.Fluent;

namespace CliInvoke.Tests.Fuzzing;

/// <summary>
///     Property-based fuzz tests for <see cref="ArgumentEscaper"/>.
/// </summary>
public class ArgumentEscaperFuzzTests
{
    [Test]
    public void EscapeInner_NullOrEmpty_ReturnsEmpty()
    {
        Prop.ForAll<string?>(value =>
                {
                    if (value is not null and { Length: > 0 })
                        return true;

                    string result = ArgumentEscaper.EscapeInner(value);
                    return result == string.Empty;
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void EscapeInner_PlainAlphanumeric_IsUnchanged()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value) ||
                    Enumerable.Any(value, c => !(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')))
                    return true;

                string escaped = ArgumentEscaper.EscapeInner(value);
                return escaped == value;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void EscapeInner_DoubledQuotesForEmbeddedQuotes()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value) || !value.Contains('"'))
                    return true;

                string escaped = ArgumentEscaper.EscapeInner(value);

                int quoteCount = 0;
                foreach (char c in escaped)
                {
                    if (c == '"')
                        quoteCount++;
                }

                return quoteCount % 2 == 0;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void EscapeInner_BackslashesBeforeQuoteAreDoubled()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value))
                    return true;

                string escaped = ArgumentEscaper.EscapeInner(value);

                for (int i = 0; i < escaped.Length; i++)
                {
                    if (escaped[i] == '"' && i > 0)
                    {
                        int bsCount = 0;
                        int j = i - 1;
                        while (j >= 0 && escaped[j] == '\\')
                        {
                            bsCount++;
                            j--;
                        }

                        if (bsCount % 2 != 0)
                            return false;
                    }
                }

                return true;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void EscapeInner_TrailingBackslashesAreDoubled()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value))
                    return true;

                string escaped = ArgumentEscaper.EscapeInner(value);

                int trailingBs = 0;
                for (int i = escaped.Length - 1; i >= 0 && escaped[i] == '\\'; i--)
                    trailingBs++;

                return trailingBs % 2 == 0;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void EscapeInner_NewlinesAreDropped()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value))
                    return true;

                string escaped = ArgumentEscaper.EscapeInner(value);
                return !escaped.Contains('\n') && !escaped.Contains('\r');
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void NeedsQuoting_NullOrEmpty_ReturnsFalse()
    {
        Prop.ForAll<string?>(value =>
                {
                    if (value is not null and { Length: > 0 })
                        return true;

                    return !ArgumentEscaper.NeedsQuoting(value);
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void NeedsQuoting_PlainAlphanumeric_ReturnsFalse()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value) ||
                    Enumerable.Any(value, c => !(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')))
                    return true;

                return !ArgumentEscaper.NeedsQuoting(value);
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void NeedsQuoting_Whitespace_ReturnsTrue()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value) || !Enumerable.Any(value, char.IsWhiteSpace))
                    return true;

                return ArgumentEscaper.NeedsQuoting(value);
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void NeedsQuoting_SpecialChars_ReturnsTrue()
    {
        char[] specialChars = ['"', '\\', '&', '|', '<', '>', '^', '%'];

        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value) || !Enumerable.Any(value, c => specialChars.Contains(c)))
                    return true;

                return ArgumentEscaper.NeedsQuoting(value);
            })
            .QuickCheckThrowOnFailure();
    }
}
