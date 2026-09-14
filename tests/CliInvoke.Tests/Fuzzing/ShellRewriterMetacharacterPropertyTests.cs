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
///     Property-based tests proving that metacharacters in target paths or arguments
///     never produce a second command for every shell kind × delivery combination.
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
    public void PowerShell_ArgumentList_MetacharactersInTarget_NeverProduceSecondCommand()
    {
        Prop.ForAll<string>(targetPath =>
            {
                if (string.IsNullOrEmpty(targetPath) || targetPath.Contains('\n') || targetPath.Contains('\r'))
                    return true;

                ProcessConfiguration source = new(targetPath, "arg1");
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "pwsh.exe",
                    runnerArgs: string.Empty,
                    kind: ShellKind.PowerShell,
                    delivery: ShellDelivery.ArgumentList,
                    windowCreation: false,
                    useShellExecution: false);

                // The -Command argument should be a single element with the escaped path
                string command = rewritten.ArgumentList[3];
                return command.Contains("& \"") &&
                       rewritten.ArgumentList.Count == 4 &&
                       rewritten.ArgumentList[0] == "-NoProfile" &&
                       rewritten.ArgumentList[1] == "-NonInteractive" &&
                       rewritten.ArgumentList[2] == "-Command";
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
                    runnerArgs: string.Empty,
                    kind: ShellKind.PowerShell,
                    delivery: ShellDelivery.ArgumentList,
                    windowCreation: false,
                    useShellExecution: false);

                // The -Command argument should contain the escaped arguments
                string command = rewritten.ArgumentList[3];
                return command.Contains("& \"prog.exe\"") &&
                       rewritten.ArgumentList.Count == 4 &&
                       rewritten.ArgumentList[0] == "-NoProfile" &&
                       rewritten.ArgumentList[1] == "-NonInteractive" &&
                       rewritten.ArgumentList[2] == "-Command";
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Cmd_Arguments_MetacharactersInTarget_NeverProduceSecondCommand()
    {
        Prop.ForAll<string>(targetPath =>
            {
                if (string.IsNullOrEmpty(targetPath) || targetPath.Contains('\n') || targetPath.Contains('\r'))
                    return true;

                ProcessConfiguration source = new(targetPath, "arg1");
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "cmd.exe",
                    runnerArgs: string.Empty,
                    kind: ShellKind.Cmd,
                    delivery: ShellDelivery.Arguments,
                    windowCreation: false,
                    useShellExecution: false);

                // The Arguments string should contain the escaped path and no ArgumentList
                return rewritten.Arguments.Contains("\"") &&
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
                    runnerArgs: string.Empty,
                    kind: ShellKind.Cmd,
                    delivery: ShellDelivery.Arguments,
                    windowCreation: false,
                    useShellExecution: false);

                // The Arguments string should contain the escaped arguments and no ArgumentList
                return rewritten.Arguments.Contains("\"prog.exe\"") &&
                       rewritten.ArgumentList.Count == 0;
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    public void Posix_ArgumentList_MetacharactersInTarget_NeverProduceSecondCommand()
    {
        Prop.ForAll<string>(targetPath =>
            {
                if (string.IsNullOrEmpty(targetPath) || targetPath.Contains('\n') || targetPath.Contains('\r'))
                    return true;

                ProcessConfiguration source = new(targetPath, "arg1");
                ProcessConfiguration rewritten = ShellRewriter.Rewrite(
                    source,
                    shellTargetPath: "/bin/sh",
                    runnerArgs: string.Empty,
                    kind: ShellKind.Posix,
                    delivery: ShellDelivery.ArgumentList,
                    windowCreation: false,
                    useShellExecution: false);

                // The script argument should contain the escaped path and be a single element
                string script = rewritten.ArgumentList[0];
                return script.Contains("& \"") &&
                       rewritten.ArgumentList.Count == 1 &&
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
                    runnerArgs: string.Empty,
                    kind: ShellKind.Posix,
                    delivery: ShellDelivery.ArgumentList,
                    windowCreation: false,
                    useShellExecution: false);

                // The script argument should contain the escaped arguments and be a single element
                string script = rewritten.ArgumentList[0];
                return script.Contains("& \"prog.exe\"") &&
                       rewritten.ArgumentList.Count == 1 &&
                       rewritten.Arguments == string.Empty;
            })
            .QuickCheckThrowOnFailure();
    }
}