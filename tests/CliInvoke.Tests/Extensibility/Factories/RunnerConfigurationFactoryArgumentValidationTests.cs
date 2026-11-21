/*
    CliInvoke.Tests
    Copyright (C) 2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Linq;
using CliInvoke.Core.Extensibility;
using CliInvoke.Extensibility;

namespace CliInvoke.Tests.Extensibility.Factories;

/// <summary>
///     Regression tests for the argument validation applied to compositions cmd.exe
///     parses itself: when the runner is cmd.exe, the runner arguments, the target
///     path and the wrapped arguments are checked; when only the wrapped target is
///     cmd.exe or a .bat/.cmd file, the wrapped argument tokens are checked. Values
///     containing a character cmd.exe interprets itself are rejected with a
///     fail-closed <see cref="ArgumentException"/> naming the token and character —
///     never stripped or escaped — while compositions cmd.exe never parses pass the
///     same values through unchanged.
/// </summary>
public class RunnerConfigurationFactoryArgumentValidationTests
{
    private static ProcessConfiguration BuildConfig(string targetFilePath, string arguments)
        => new ProcessConfigurationBuilder(targetFilePath)
            .SetArguments(arguments)
            .Build();

    private static IRunnerConfigurationFactory CreateFactory()
        => new RunnerConfigurationFactory();

    [Test]
    public async Task CmdRunner_BenignArguments_AreAcceptedAndComposed()
    {
        IRunnerConfigurationFactory factory = CreateFactory();

        ProcessConfiguration runner = BuildConfig("cmd.exe", "/c");
        ProcessConfiguration target = BuildConfig(@"C:\Windows\System32\robocopy.exe", @"C:\src C:\dst /mir");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        // The composition stays the single escaped Arguments string cmd /c expects.
        await Assert.That(result.ArgumentList.Count).IsEqualTo(0);
        await Assert.That(result.Arguments).StartsWith("/c ");
        await Assert.That(result.Arguments).Contains("robocopy.exe");
        await Assert.That(result.Arguments).Contains("/mir");
    }

    [Test]
    [Arguments("&", "&")]
    [Arguments("x&echo more", "&")]
    [Arguments("<", "<")]
    [Arguments("x<echo more", "<")]
    [Arguments(">", ">")]
    [Arguments("|", "|")]
    [Arguments("^", "^")]
    [Arguments("%TMP%", "%")]
    [Arguments("\"quoted\"", "\"")]
    [Arguments("!delayed!", "!")]
    public async Task CmdRunner_RunnerArgumentsWithControlCharacter_Throws(
        string arguments, string expectedCharacter)
    {
        IRunnerConfigurationFactory factory = CreateFactory();

        ProcessConfiguration runner = BuildConfig("cmd.exe", "/c " + arguments);
        ProcessConfiguration target = BuildConfig("app.exe", "safe");

        ArgumentException exception = await Assert
            .That(() => factory.CreateRunnerConfiguration(target, runner))
            .Throws<ArgumentException>();

        await Assert.That(exception.ParamName).IsEqualTo("Arguments");
        await Assert.That(exception.Message).Contains(arguments);
        await Assert.That(exception.Message).Contains($"contains \"{expectedCharacter}\"");
    }

    [Test]
    [Arguments("&", "&")]
    [Arguments("x&echo more", "&")]
    [Arguments("<", "<")]
    [Arguments("x<echo more", "<")]
    [Arguments(">", ">")]
    [Arguments("|", "|")]
    [Arguments("^", "^")]
    [Arguments("%TMP%", "%")]
    [Arguments("\"quoted\"", "\"")]
    [Arguments("!delayed!", "!")]
    public async Task CmdRunner_WrappedArgumentsWithControlCharacter_Throws(
        string arguments, string expectedCharacter)
    {
        IRunnerConfigurationFactory factory = CreateFactory();

        ProcessConfiguration runner = BuildConfig("cmd.exe", "/c");
        ProcessConfiguration target = BuildConfig("app.exe", arguments);

        ArgumentException exception = await Assert
            .That(() => factory.CreateRunnerConfiguration(target, runner))
            .Throws<ArgumentException>();

        await Assert.That(exception.ParamName).IsEqualTo("Arguments");
        await Assert.That(exception.Message).Contains(arguments);
        await Assert.That(exception.Message).Contains($"contains \"{expectedCharacter}\"");
    }

    [Test]
    public async Task CmdRunner_WrappedArgumentListEntryWithControlCharacter_Throws()
    {
        IRunnerConfigurationFactory factory = CreateFactory();

        // The sanctioned verbatim argument list is checked the same way when cmd.exe
        // will parse the composed command line.
        ProcessConfiguration runner = BuildConfig("cmd.exe", "/c");
        ProcessConfiguration target = new ProcessConfiguration("app.exe", "ignored")
        {
            ArgumentList = ["--flag", "x&echo more"]
        };

        ArgumentException exception = await Assert
            .That(() => factory.CreateRunnerConfiguration(target, runner))
            .Throws<ArgumentException>();

        await Assert.That(exception.ParamName).IsEqualTo("Arguments");
        await Assert.That(exception.Message).Contains("x&echo more");
        await Assert.That(exception.Message).Contains("&");
        await Assert.That(exception.Message).Contains("contains \"&\"");
    }

    [Test]
    public async Task CmdRunner_TargetPathWithSeparator_Throws()
    {
        IRunnerConfigurationFactory factory = CreateFactory();

        // When the runner is cmd.exe it parses the whole composed tail itself, so the
        // target path is validated alongside the argument values.
        ProcessConfiguration runner = BuildConfig("cmd.exe", "/c");
        ProcessConfiguration target = BuildConfig(@"C:\tools\a & b.exe", "safe");

        ArgumentException exception = await Assert
            .That(() => factory.CreateRunnerConfiguration(target, runner))
            .Throws<ArgumentException>();

        await Assert.That(exception.ParamName).IsEqualTo("TargetFilePath");
        await Assert.That(exception.Message).Contains("&");
        await Assert.That(exception.Message).Contains("contains \"&\"");
    }

    [Test]
    [Arguments(@"C:/mytools/mycmd.exe")]
    [Arguments(@"C:/cmd tools/runner.exe")]
    public async Task RunnerPathContainingCmdWithoutCmdFileName_IsNotTreatedAsCmdRunner(
        string runnerTarget)
    {
        IRunnerConfigurationFactory factory = CreateFactory();

        // A runner whose path contains "cmd" but whose file name is neither cmd nor
        // cmd.exe keeps tokenized delivery; its argument values are not validated as
        // cmd.exe input, because cmd.exe will not parse the runner's own arguments.
        ProcessConfiguration runner = BuildConfig(runnerTarget, "/c echo BASE & echo X");
        ProcessConfiguration target = BuildConfig(@"C:\Windows\System32\robocopy.exe", @"C:\src C:\dst /mir");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        // The non-shell branch delivers discrete tokens; the separator character stays
        // ordinary argument data for the runner.
        await Assert.That(result.ArgumentList.Contains("&")).IsTrue();
        await Assert.That(result.ArgumentList.Count).IsGreaterThan(0);
        await Assert.That(result.Arguments).IsEqualTo(string.Empty);
    }

    [Test]
    [Arguments("run.exe")]
    [Arguments("pwsh.exe")]
    public async Task NonCmdRunner_BatchTarget_WrappedArgumentsWithControlCharacter_Throws(
        string runnerTarget)
    {
        IRunnerConfigurationFactory factory = CreateFactory();

        // A batch file re-parses its arguments with cmd.exe's rules even when the
        // runner is not cmd.exe, so the wrapped argument tokens are validated.
        ProcessConfiguration runner = BuildConfig(runnerTarget, "-flag");
        ProcessConfiguration target = BuildConfig(@"C:/scripts/task.bat", "x&echo more");

        ArgumentException exception = await Assert
            .That(() => factory.CreateRunnerConfiguration(target, runner))
            .Throws<ArgumentException>();

        await Assert.That(exception.ParamName).IsEqualTo("Arguments");
        await Assert.That(exception.Message).Contains("&");
        await Assert.That(exception.Message).Contains("contains \"&\"");
    }

    [Test]
    [Arguments("cmd.exe")]
    [Arguments("task.bat")]
    [Arguments("task.cmd")]
    public async Task NonCmdRunner_CmdOrBatchTarget_WrappedArgumentsWithControlCharacter_Throws(
        string targetFileName)
    {
        IRunnerConfigurationFactory factory = CreateFactory();

        ProcessConfiguration runner = BuildConfig("run.exe", "-flag");
        // Platform-neutral separators: the validation matches targets by file
        // name and extension on every platform.
        ProcessConfiguration target = BuildConfig($@"C:/tools/{targetFileName}", "safe & echo more");

        ArgumentException exception = await Assert
            .That(() => factory.CreateRunnerConfiguration(target, runner))
            .Throws<ArgumentException>();

        await Assert.That(exception.ParamName).IsEqualTo("Arguments");
        await Assert.That(exception.Message).Contains("&");
        await Assert.That(exception.Message).Contains("contains \"&\"");
    }

    [Test]
    public async Task NonCmdRunner_NonCmdTarget_ControlCharacterArguments_AreAccepted()
    {
        IRunnerConfigurationFactory factory = CreateFactory();

        // Compositions cmd.exe never parses are untouched: a discrete token is
        // delivered literally to the target process.
        ProcessConfiguration runner = BuildConfig("sudo", "-u root");
        ProcessConfiguration target = BuildConfig("app.exe", "value&name");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        await Assert.That(result.ArgumentList).Contains("value&name");
    }

    [Test]
    public async Task PowerShellRunner_ControlCharacters_AreAccepted()
    {
        IRunnerConfigurationFactory factory = CreateFactory();

        // PowerShell parses its own command tail; validation must not block
        // compositions cmd.exe never parses.
        ProcessConfiguration runner = BuildConfig("pwsh.exe", "-NoProfile -Command &");
        ProcessConfiguration target = BuildConfig("app.exe", "value&name");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        await Assert.That(result.ArgumentList).Contains("&");
        await Assert.That(result.ArgumentList[^1]).Contains("app.exe");
    }
}
