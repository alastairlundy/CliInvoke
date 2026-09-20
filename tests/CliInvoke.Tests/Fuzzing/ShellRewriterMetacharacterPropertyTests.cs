/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Linq;
using CliInvoke.Internal;
using FsCheck;
using FsCheck.Fluent;

namespace CliInvoke.Tests.Fuzzing;

/// <summary>
///     Property-based tests proving that, for every shell kind, metacharacters in
///     target paths or arguments never produce a second command, and that the
///     caller-owned shell switches are delivered so the wrapped command actually runs.
///     Every generated input is forced to contain all declared shell metacharacters
///     and the rewritten command is compared against independently computed
///     shell-escaped text.
/// </summary>
public class ShellRewriterMetacharacterPropertyTests
{
    private static readonly char[] PowerShellMetacharacters =
        ['`', '$', ';', '|', '&', '(', ')', '{', '}', '<', '>', '"', '\''];

    private static readonly char[] CmdMetacharacters =
        ['^', '&', '|', '<', '>', '%'];

    private static readonly char[] PosixShellMetacharacters =
        ['\\', '$', '`', '"', '\'', '!', '&', '|', ';', '(', ')',
         '<', '>', '{', '}', '[', ']', '~', '#', '*', '?'];

    private static readonly string PowerShellMetaString = new(PowerShellMetacharacters);

    private static readonly string CmdMetaString = new(CmdMetacharacters);

    private static readonly string PosixMetaString = new(PosixShellMetacharacters);

    // Independent character-by-character mirrors of ShellRewriter's escaping and
    // quoting semantics, used to compute the full expected rewritten command
    // without calling the code under test.

    private static string QuotePowerShellPath(string path)
    {
        System.Text.StringBuilder builder = new(path.Length + 2);

        builder.Append('"');

        foreach (char c in path)
        {
            switch (c)
            {
                case '`':
                case '"':
                case '$':
                    builder.Append('`').Append(c);
                    break;
                case '\n':
                case '\r':
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        builder.Append('"');

        return builder.ToString();
    }

    private static string EscapePowerShellArg(string value)
    {
        System.Text.StringBuilder builder = new(value.Length + 16);

        foreach (char c in value)
        {
            if (Array.IndexOf(PowerShellMetacharacters, c) >= 0)
            {
                builder.Append('`').Append(c);
            }
            else if (c == '\n')
            {
                builder.Append('`').Append('n');
            }
            else if (c == '\r')
            {
                // Dropped, mirroring the implementation.
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static string QuoteCmdPath(string path)
    {
        System.Text.StringBuilder builder = new(path.Length + 2 + 16);

        builder.Append('"');

        foreach (char c in path)
        {
            switch (c)
            {
                case '"':
                    builder.Append('"').Append('"');
                    break;
                case '^':
                case '&':
                case '|':
                case '<':
                case '>':
                case '(':
                case ')':
                case '%':
                case '!':
                    builder.Append('^').Append(c);
                    break;
                case '\n':
                case '\r':
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        builder.Append('"');

        return builder.ToString();
    }

    private static string EscapeCmdArg(string value)
    {
        System.Text.StringBuilder builder = new(value.Length + 16);

        foreach (char c in value)
        {
            if (Array.IndexOf(CmdMetacharacters, c) >= 0)
            {
                builder.Append('^').Append(c);
            }
            else if (c == '"')
            {
                builder.Append('"').Append('"');
            }
            else if (c == '\n' || c == '\r')
            {
                // Dropped, mirroring the implementation.
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static string QuotePosixPath(string path)
    {
        return "'" + path.Replace("'", "'\\''") + "'";
    }

    private static string EscapePosixArg(string value)
    {
        System.Text.StringBuilder builder = new(value.Length + 16);

        foreach (char c in value)
        {
            if (Array.IndexOf(PosixShellMetacharacters, c) >= 0)
            {
                builder.Append('\\').Append(c);
            }
            else if (c == '\n' || c == '\r')
            {
                // Dropped, mirroring the implementation.
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    [Test]
    public void PowerShell_ArgumentList_RunnerSwitchesAppearBeforeScript()
    {
        Prop.ForAll<string>(targetPath =>
            {
                if (string.IsNullOrEmpty(targetPath) || targetPath.Contains('\n') || targetPath.Contains('\r'))
                    return true;

                // Force every declared PowerShell metacharacter into the target path.
                string metaPath = targetPath + PowerShellMetaString;

                ProcessConfiguration source = new(metaPath, "arg1");
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "pwsh.exe",
                    runnerArgs: "-NoProfile -NonInteractive -Command",
                    kind: ShellKind.PowerShell,
                    windowCreation: false,
                    useShellExecution: false);

                string expectedScript = $"& {QuotePowerShellPath(metaPath)} arg1";

                // The middleware-owned switches must be discrete entries preceding the
                // script entry, and the script must be a single escaped command element.
                return rewritten.ArgumentList.Count == 4 &&
                       rewritten.ArgumentList[0] == "-NoProfile" &&
                       rewritten.ArgumentList[1] == "-NonInteractive" &&
                       rewritten.ArgumentList[2] == "-Command" &&
                       rewritten.ArgumentList[3] == expectedScript &&
                       rewritten.Arguments == string.Empty;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void PowerShell_ArgumentList_MetacharactersInArguments_NeverProduceSecondCommand()
    {
        Prop.ForAll<string>(arguments =>
            {
                if (string.IsNullOrEmpty(arguments) || arguments.Contains('\n') || arguments.Contains('\r'))
                    return true;

                // Force every declared PowerShell metacharacter into the arguments.
                string metaArguments = arguments + PowerShellMetaString;

                ProcessConfiguration source = new("prog.exe", metaArguments);
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "pwsh.exe",
                    runnerArgs: "-NoProfile -NonInteractive -Command",
                    kind: ShellKind.PowerShell,
                    windowCreation: false,
                    useShellExecution: false);

                string expectedScript = $"& {QuotePowerShellPath("prog.exe")} {EscapePowerShellArg(metaArguments)}";

                string script = rewritten.ArgumentList[3];
                return script == expectedScript &&
                       rewritten.ArgumentList.Count == 4 &&
                       rewritten.ArgumentList[2] == "-Command";
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Cmd_Arguments_StartsWithCSwitch()
    {
        Prop.ForAll<string>(targetPath =>
            {
                if (string.IsNullOrEmpty(targetPath) || targetPath.Contains('\n') || targetPath.Contains('\r'))
                    return true;

                // Force every declared Cmd metacharacter into the target path.
                string metaPath = targetPath + CmdMetaString;

                ProcessConfiguration source = new(metaPath, "arg1");
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "cmd.exe",
                    runnerArgs: "/c",
                    kind: ShellKind.Cmd,
                    windowCreation: false,
                    useShellExecution: false);

                // Without the leading /c, cmd.exe starts interactively and never runs
                // the wrapped command; assert the switch is present and first, and that
                // the target path is emitted as the expected quoted, caret-escaped token
                // wrapped in double quotes after the /c switch.
                string expectedArguments = $"/c \"{QuoteCmdPath(metaPath)} arg1\"";
                return rewritten.Arguments == expectedArguments &&
                       rewritten.ArgumentList.Count == 0;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Cmd_Arguments_MetacharactersInArguments_NeverProduceSecondCommand()
    {
        Prop.ForAll<string>(arguments =>
            {
                // Force every declared Cmd metacharacter into the arguments; the meta
                // suffix guarantees the composed argument text is never whitespace-only.
                string metaArguments = arguments + CmdMetaString;

                ProcessConfiguration source = new("prog.exe", metaArguments);
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "cmd.exe",
                    runnerArgs: "/c",
                    kind: ShellKind.Cmd,
                    windowCreation: false,
                    useShellExecution: false);

                string expectedCommand = $"{QuoteCmdPath("prog.exe")} {EscapeCmdArg(metaArguments)}";
                // The rewritten arguments are in the format /c "innerCommand".
                // Strip the /c prefix and the wrapping double quotes to extract the inner command.
                string innerPrefix = "/c \"";
                string args = rewritten.Arguments;
                string command = args.Substring(innerPrefix.Length, args.Length - innerPrefix.Length - 1);
                return command == expectedCommand &&
                       !command.Contains("&&") &&
                       rewritten.ArgumentList.Count == 0;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Posix_ArgumentList_CSwitchPrecedesScriptWithoutCallOperator()
    {
        Prop.ForAll<string>(targetPath =>
            {
                if (string.IsNullOrEmpty(targetPath) || targetPath.Contains('\n') || targetPath.Contains('\r'))
                    return true;

                // Force every declared POSIX metacharacter into the target path.
                string metaPath = targetPath + PosixMetaString;

                ProcessConfiguration source = new(metaPath, "arg1");
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "/bin/sh",
                    runnerArgs: "-c",
                    kind: ShellKind.Posix,
                    windowCreation: false,
                    useShellExecution: false);

                // Without -c the shell would treat the script as a file path, and the
                // script must not use the PowerShell & call operator (POSIX syntax error).
                // The target path is single-quoted so every metacharacter is literal data.
                string expectedScript = $"{QuotePosixPath(metaPath)} arg1";
                return rewritten.ArgumentList.Count == 2 &&
                       rewritten.ArgumentList[0] == "-c" &&
                       rewritten.ArgumentList[1] == expectedScript &&
                       !rewritten.ArgumentList[1].StartsWith("& ") &&
                       rewritten.Arguments == string.Empty;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Posix_ArgumentList_MetacharactersInArguments_NeverProduceSecondCommand()
    {
        Prop.ForAll<string>(arguments =>
            {
                // Force every declared POSIX metacharacter into the arguments; the meta
                // suffix guarantees the composed argument text is never whitespace-only.
                string metaArguments = arguments + PosixMetaString;

                ProcessConfiguration source = new("prog.exe", metaArguments);
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "/bin/sh",
                    runnerArgs: "-c",
                    kind: ShellKind.Posix,
                    windowCreation: false,
                    useShellExecution: false);

                string expectedScript = $"{QuotePosixPath("prog.exe")} {EscapePosixArg(metaArguments)}";

                string script = rewritten.ArgumentList[1];
                return script == expectedScript &&
                       !script.StartsWith("& ") &&
                       rewritten.ArgumentList.Count == 2;
            })
            .QuickCheckThrowOnFailure();
    }
}
