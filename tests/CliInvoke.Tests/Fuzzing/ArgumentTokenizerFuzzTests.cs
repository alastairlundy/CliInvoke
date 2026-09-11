/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Linq;
using CliInvoke.Core.Internal;
using FsCheck;
using FsCheck.Fluent;

namespace CliInvoke.Tests.Fuzzing;

/// <summary>
///     Property-based fuzz tests for <see cref="ArgumentTokenizer"/>.
/// </summary>
public class ArgumentTokenizerFuzzTests
{
    [Test]
    public void Tokenize_NullOrEmptyOrWhitespace_ReturnsEmpty()
    {
        Prop.ForAll<string?>(value =>
                {
                    if (!string.IsNullOrWhiteSpace(value))
                        return true;

                    IReadOnlyList<string> tokens = ArgumentTokenizer.Tokenize(value);
                    return tokens.Count == 0;
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Tokenize_SingleWord_ReturnsSingleToken()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsWhiteSpace) || value.Contains('"'))
                    return true;

                IReadOnlyList<string> tokens = ArgumentTokenizer.Tokenize(value);
                return tokens.Count == 1 && tokens[0] == value;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Tokenize_SpaceSeparatedWords_ProducesCorrectCount()
    {
        Prop.ForAll<string>(word1 =>
            {
                return Prop.ForAll<string>(word2 =>
                    {
                        if (string.IsNullOrWhiteSpace(word1) || string.IsNullOrWhiteSpace(word2) ||
                            word1.Any(char.IsWhiteSpace) || word2.Any(char.IsWhiteSpace) ||
                            word1.Contains('"') || word2.Contains('"'))
                            return true;

                        string input = word1 + " " + word2;
                        IReadOnlyList<string> tokens = ArgumentTokenizer.Tokenize(input);
                        return tokens.Count == 2 && tokens[0] == word1 && tokens[1] == word2;
                    });
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Tokenize_QuotedSpan_PreservesInternalContent()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrWhiteSpace(value) || value.Contains('"'))
                    return true;

                string input = "\"" + value + "\"";
                IReadOnlyList<string> tokens = ArgumentTokenizer.Tokenize(input);
                return tokens.Count == 1 && tokens[0] == value;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Tokenize_DoubleQuoteInsideQuotes_ProducesLiteralQuote()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrWhiteSpace(value) || value.Contains('"'))
                    return true;

                string input = "\"" + value + "\"\"" + value + "\"";
                IReadOnlyList<string> tokens = ArgumentTokenizer.Tokenize(input);
                return tokens.Count == 1 && tokens[0] == value + "\"" + value;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Tokenize_NoTokensAreEmpty()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrWhiteSpace(value))
                    return true;

                IReadOnlyList<string> tokens = ArgumentTokenizer.Tokenize(value);

                foreach (string token in tokens)
                {
                    if (string.IsNullOrEmpty(token))
                        return false;
                }

                return true;
            })
            .QuickCheckThrowOnFailure();
    }
}
