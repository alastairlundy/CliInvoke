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
///     Property-based fuzz tests for <see cref="ShellArgumentEscaper"/>.
/// </summary>
public class ShellArgumentEscaperFuzzTests
{
    private static readonly char[] PowerShellMetacharacters =
        ['`', '$', ';', '|', '&', '(', ')', '{', '}', '<', '>', '"', '\''];

    private static readonly char[] CmdMetacharacters =
        ['^', '&', '|', '<', '>', '%'];

    [Test]
    public void EscapeForPowerShell_NullOrEmpty_ReturnsEmpty()
    {
        Prop.ForAll<string?>(value =>
                {
                    if (value is not null and { Length: > 0 })
                        return true;

                    string result = ShellArgumentEscaper.EscapeForPowerShell(value);
                    return result == string.Empty;
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void EscapeForPowerShell_OutputContainsNoUnescapedMetacharacters()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value) || value.Contains('\n') || value.Contains('\r'))
                    return true;

                string escaped = ShellArgumentEscaper.EscapeForPowerShell(value);

                // Build expected output character-by-character, mirroring the escaper logic
                var expected = new System.Text.StringBuilder(value.Length + 16);
                foreach (char c in value)
                {
                    if (Array.IndexOf(PowerShellMetacharacters, c) >= 0)
                    {
                        expected.Append('`').Append(c);
                    }
                    else
                    {
                        expected.Append(c);
                    }
                }

                return escaped == expected.ToString();
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void EscapeForPowerShell_NonSpecialCharactersPassThroughUnchanged()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value) ||
                    Enumerable.Any(value, c => char.IsControl(c) || Array.IndexOf(PowerShellMetacharacters, c) >= 0))
                    return true;

                string escaped = ShellArgumentEscaper.EscapeForPowerShell(value);
                return escaped == value;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void EscapeForCmd_NullOrEmpty_ReturnsEmpty()
    {
        Prop.ForAll<string?>(value =>
                {
                    if (value is not null and { Length: > 0 })
                        return true;

                    string result = ShellArgumentEscaper.EscapeForCmd(value);
                    return result == string.Empty;
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void EscapeForCmd_OutputContainsNoUnescapedMetacharacters()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value) || value.Contains('\n') || value.Contains('\r'))
                    return true;

                string escaped = ShellArgumentEscaper.EscapeForCmd(value);

                // Build expected output character-by-character, mirroring the escaper logic
                var expected = new System.Text.StringBuilder(value.Length + 16);
                foreach (char c in value)
                {
                    if (Array.IndexOf(CmdMetacharacters, c) >= 0)
                    {
                        expected.Append('^').Append(c);
                    }
                    else if (c == '"')
                    {
                        expected.Append("\"\"");
                    }
                    else
                    {
                        expected.Append(c);
                    }
                }

                return escaped == expected.ToString();
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void EscapeForCmd_NonSpecialCharactersPassThroughUnchanged()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value) ||
                    Enumerable.Any(value, c => char.IsControl(c) || Array.IndexOf(CmdMetacharacters, c) >= 0 || c == '"'))
                    return true;

                string escaped = ShellArgumentEscaper.EscapeForCmd(value);
                return escaped == value;
            })
            .QuickCheckThrowOnFailure();
    }
}
