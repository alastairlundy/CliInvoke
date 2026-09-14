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

    [Test]
    public void PowerShell_ArgumentList_RunnerSwitchesAppearBeforeScript()
    {
        Prop.ForAll<string>(targetPath =>
            {
                if (string.IsNullOrEmpty(targetPath) || targetPath.Contains('\n') || targetPath.Contains('\r'))
                    return true;

                ProcessConfiguration source = new(targetPath, "arg1");
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "pwsh.exe",
                    runnerArgs: "-NoProfile -NonInteractive -Command",
                    kind: ShellKind.PowerShell,
                    windowCreation: false,
                    useShellExecution: false);

                // The middleware-owned switches must be discrete entries preceding the
                // script entry, and the script must be a single escaped command element.
                return rewritten.ArgumentList.Count == 4 &&
                       rewritten.ArgumentList[0] == "-NoProfile" &&
                       rewritten.ArgumentList[1] == "-NonInteractive" &&
                       rewritten.ArgumentList[2] == "-Command" &&
                       rewritten.ArgumentList[3].Contains("& \"") &&
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

                ProcessConfiguration source = new("prog.exe", arguments);
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "pwsh.exe",
                    runnerArgs: "-NoProfile -NonInteractive -Command",
                    kind: ShellKind.PowerShell,
                    windowCreation: false,
                    useShellExecution: false);

                string script = rewritten.ArgumentList[3];
                return script.Contains("& \"prog.exe\"") &&
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

                ProcessConfiguration source = new(targetPath, "arg1");
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "cmd.exe",
                    runnerArgs: "/c",
                    kind: ShellKind.Cmd,
                    windowCreation: false,
                    useShellExecution: false);

                // Without the leading /c, cmd.exe starts interactively and never runs
                // the wrapped command; assert the switch is present and first.
                return rewritten.Arguments.StartsWith("/c ") &&
                       rewritten.Arguments.Contains("\"") &&
                       rewritten.ArgumentList.Count == 0;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Cmd_Arguments_MetacharactersInArguments_NeverProduceSecondCommand()
    {
        Prop.ForAll<string>(arguments =>
            {
                if (string.IsNullOrEmpty(arguments) || arguments.Contains('\n') || arguments.Contains('\r'))
                    return true;

                ProcessConfiguration source = new("prog.exe", arguments);
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "cmd.exe",
                    runnerArgs: "/c",
                    kind: ShellKind.Cmd,
                    windowCreation: false,
                    useShellExecution: false);

                string command = rewritten.Arguments.Substring("/c ".Length);
                return command.StartsWith("\"prog.exe\" ") &&
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

                ProcessConfiguration source = new(targetPath, "arg1");
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "/bin/sh",
                    runnerArgs: "-c",
                    kind: ShellKind.Posix,
                    windowCreation: false,
                    useShellExecution: false);

                // Without -c the shell would treat the script as a file path, and the
                // script must not use the PowerShell & call operator (POSIX syntax error).
                return rewritten.ArgumentList.Count == 2 &&
                       rewritten.ArgumentList[0] == "-c" &&
                       rewritten.ArgumentList[1].StartsWith("\"") &&
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
                if (string.IsNullOrEmpty(arguments) || arguments.Contains('\n') || arguments.Contains('\r'))
                    return true;

                ProcessConfiguration source = new("prog.exe", arguments);
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "/bin/sh",
                    runnerArgs: "-c",
                    kind: ShellKind.Posix,
                    windowCreation: false,
                    useShellExecution: false);

                string script = rewritten.ArgumentList[1];
                return script.StartsWith("\"prog.exe\" ") &&
                       !script.StartsWith("& ") &&
                       rewritten.ArgumentList.Count == 2;
            })
            .QuickCheckThrowOnFailure();
    }
}
