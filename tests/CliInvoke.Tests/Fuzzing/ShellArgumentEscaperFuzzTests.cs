/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Linq;
using System.Text;
using CliInvoke.Internal;

namespace CliInvoke.Tests.Fuzzing;

/// <summary>
///     Property-based fuzz tests for <see cref="ShellRewriter"/> shell escaping methods
///     and composition paths.
/// </summary>
public class ShellRewriterFuzzTests
{
    private static readonly char[] PowerShellMetacharacters =
        ['`', '$', ';', '|', '&', '(', ')', '{', '}', '<', '>', '"', '\''];

    private static readonly char[] CmdMetacharacters =
        ['^', '&', '|', '<', '>', '%'];

    private static readonly char[] PosixShellMetacharacters =
        ['\\', '$', '`', '"', '\'', '!', '&', '|', ';', '(', ')',
         '<', '>', '{', '}', '[', ']', '~', '#', '*', '?'];

    [Test]
    public void EscapeForPosixShell_NullOrEmpty_ReturnsEmpty()
    {
        Prop.ForAll<string?>(value =>
                {
                    if (value is not null and { Length: > 0 })
                        return true;

                    string result = ShellRewriter.EscapeForPosixShell(value);
                    return result == string.Empty;
                })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void EscapeForPosixShell_OutputContainsNoUnescapedMetacharacters()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value) || value.Contains('\n') || value.Contains('\r'))
                    return true;

                string escaped = ShellRewriter.EscapeForPosixShell(value);

                // Build expected output character-by-character, mirroring the escaper logic
                StringBuilder expected = new System.Text.StringBuilder(value.Length + 16);
                foreach (char c in value)
                {
                    if (Array.IndexOf(PosixShellMetacharacters, c) >= 0)
                    {
                        expected.Append('\\').Append(c);
                    }
                    else
                    {
                        expected.Append(c);
                    }
                }

                return string.Equals(escaped, expected.ToString(), StringComparison.Ordinal);
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void EscapeForPosixShell_NonSpecialCharactersPassThroughUnchanged()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value) ||
                    value.Any(c => char.IsControl(c) || Array.IndexOf(PosixShellMetacharacters, c) >= 0))
                    return true;

                string escaped = ShellRewriter.EscapeForPosixShell(value);
                return string.Equals(escaped, value, StringComparison.Ordinal);
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void EscapeForPowerShell_NullOrEmpty_ReturnsEmpty()
    {
        Prop.ForAll<string?>(value =>
                {
                    if (value is not null and { Length: > 0 })
                        return true;

                    string result = ShellRewriter.EscapeForPowerShell(value);
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

                string escaped = ShellRewriter.EscapeForPowerShell(value);

                // Build expected output character-by-character, mirroring the escaper logic
                StringBuilder expected = new System.Text.StringBuilder(value.Length + 16);
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

                return string.Equals(escaped, expected.ToString(), StringComparison.Ordinal);
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void EscapeForPowerShell_NonSpecialCharactersPassThroughUnchanged()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value) ||
                    value.Any(c => char.IsControl(c) || Array.IndexOf(PowerShellMetacharacters, c) >= 0))
                    return true;

                string escaped = ShellRewriter.EscapeForPowerShell(value);
                return string.Equals(escaped, value, StringComparison.Ordinal);
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

                    string result = ShellRewriter.EscapeForCmd(value);
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

                string escaped = ShellRewriter.EscapeForCmd(value);

                // Build expected output character-by-character, mirroring the escaper logic
                StringBuilder expected = new System.Text.StringBuilder(value.Length + 16);
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

                return string.Equals(escaped, expected.ToString(), StringComparison.Ordinal);
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void EscapeForCmd_NonSpecialCharactersPassThroughUnchanged()
    {
        Prop.ForAll<string>(value =>
            {
                if (string.IsNullOrEmpty(value) ||
                    value.Any(c => char.IsControl(c) || Array.IndexOf(CmdMetacharacters, c) >= 0 || c == '"'))
                    return true;

                string escaped = ShellRewriter.EscapeForCmd(value);
                return string.Equals(escaped, value, StringComparison.Ordinal);
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Rewrite_PowerShell_ArgumentListDelivery_ShellPathIsSetCorrectly()
    {
        Prop.ForAll<string>(targetPath =>
            {
                if (string.IsNullOrEmpty(targetPath) || targetPath.Contains('\n') || targetPath.Contains('\r'))
                    return true;

                ProcessConfiguration source = new(targetPath, "arg1 arg2");
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "pwsh.exe",
                    runnerArgs: "-NoProfile -NonInteractive -Command",
                    kind: ShellKind.PowerShell,
                    windowCreation: false,
                    useShellExecution: false);

                IReadOnlyList<string> argumentList = rewritten.ArgumentList;
                return string.Equals(rewritten.TargetFilePath, "pwsh.exe", StringComparison.Ordinal)
                       && argumentList.Count == 4
                       && string.Equals(argumentList[2], "-Command", StringComparison.Ordinal)
                       && argumentList[^1].Contains("& \"");
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Rewrite_Cmd_ArgumentsDelivery_ShellPathIsSetCorrectly()
    {
        Prop.ForAll<string>(targetPath =>
            {
                if (string.IsNullOrEmpty(targetPath) || targetPath.Contains('\n') || targetPath.Contains('\r'))
                    return true;

                ProcessConfiguration source = new(targetPath, "arg1 arg2");
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "cmd.exe",
                    runnerArgs: "/c",
                    kind: ShellKind.Cmd,
                    windowCreation: false,
                    useShellExecution: false);

                return string.Equals(rewritten.TargetFilePath, "cmd.exe", StringComparison.Ordinal)
                       && rewritten.ArgumentList.Count == 0
                       && !string.IsNullOrEmpty(rewritten.Arguments);
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Rewrite_Posix_ArgumentListDelivery_ShellPathIsSetCorrectly()
    {
        Prop.ForAll<string>(targetPath =>
            {
                if (string.IsNullOrEmpty(targetPath) || targetPath.Contains('\n') || targetPath.Contains('\r'))
                    return true;

                ProcessConfiguration source = new(targetPath, "arg1 arg2");
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "/bin/sh",
                    runnerArgs: "-c",
                    kind: ShellKind.Posix,
                    windowCreation: false,
                    useShellExecution: false);

                IReadOnlyList<string> argumentList = rewritten.ArgumentList;
                return string.Equals(rewritten.TargetFilePath, "/bin/sh", StringComparison.Ordinal)
                       && argumentList.Count == 2
                       && string.Equals(argumentList[0], "-c", StringComparison.Ordinal)
                       && rewritten.Arguments == string.Empty;
            })
            .QuickCheckThrowOnFailure();
    }
}
