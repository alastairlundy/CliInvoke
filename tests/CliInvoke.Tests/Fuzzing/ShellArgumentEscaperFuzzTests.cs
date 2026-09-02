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
 
                // Verify each PowerShell metacharacter is properly escaped
                foreach (char metachar in PowerShellMetacharacters)
                {
                    if (value.Contains(metachar))
                    {
                        // The escaped sequence should be backtick followed by the metachar
                        string expectedEscaped = $"`{metachar}";
                        if (!escaped.Contains(expectedEscaped))
                            return false;
                    }
                }
 
                return true;
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
 
                // Verify each CMD metacharacter is properly escaped
                foreach (char metachar in CmdMetacharacters)
                {
                    if (value.Contains(metachar))
                    {
                        // The escaped sequence should be caret followed by the metachar
                        string expectedEscaped = $"^{metachar}";
                        if (!escaped.Contains(expectedEscaped))
                            return false;
                    }
                }
 
                // Also check for double-quoted escape of double quotes
                if (value.Contains('"'))
                {
                    if (!escaped.Contains("\"\""))
                        return false;
                }
 
                return true;
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
