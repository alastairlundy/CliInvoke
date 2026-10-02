/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Builders;
using CliInvoke.Core.Extensibility.Factories;
using CliInvoke.Extensibility.Factories;
using Xunit;

namespace CliInvoke.Tests.Extensibility.Factories;

/// <summary>
///     Verifies that <see cref="RunnerProcessFactory"/> composes the wrapped command
///     from discrete argument tokens, so caller-supplied values (including quotes and
///     shell metacharacters) are passed through to the wrapped process as isolated
///     literals instead of being re-parsed into additional command-line tokens.
/// </summary>
public class RunnerProcessFactoryTests
{
    private static ProcessConfiguration BuildConfig(string targetFilePath, string arguments)
        => new ProcessConfigurationBuilder(targetFilePath).SetArguments(arguments).Build();

    [Fact]
    public void CreateRunnerConfiguration_KeepsTargetAndArgsAsDiscreteTokens()
    {
        IRunnerProcessFactory factory = new RunnerProcessFactory();

        ProcessConfiguration runner = BuildConfig("cmd.exe", "/c");
        ProcessConfiguration target = BuildConfig(@"C:\program files\app.exe", "arg one");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        // The runner flag, the target path (kept whole, even though it
        // contains a space) and each user argument are separate tokens rather than one
        // re-parsed string. A space-bearing target stays a single token.
        Assert.Contains("/c", result.ArgumentsList);
        Assert.Contains(@"C:\program files\app.exe", result.ArgumentsList);
        Assert.Contains("arg", result.ArgumentsList);
        Assert.Contains("one", result.ArgumentsList);
    }

    [Fact]
    public void CreateRunnerConfiguration_DoesNotMergeCallerQuoteWithWrapper()
    {
        IRunnerProcessFactory factory = new RunnerProcessFactory();

        // A quote inside the caller's target must remain an isolated literal token, not
        // be concatenated with the wrapper's quoting into a single broken token.
        ProcessConfiguration runner = BuildConfig("pwsh.exe", "-NoProfile -NonInteractive -Command");
        ProcessConfiguration target = BuildConfig("app\"evil.exe", "safe");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        Assert.Contains("app\"evil.exe", result.ArgumentsList);
        Assert.DoesNotContain("& \"app", result.ArgumentsList);
    }

    [Fact]
    public void CreateRunnerConfiguration_BenignTarget_DoesNotInsertTerminator()
    {
        IRunnerProcessFactory factory = new RunnerProcessFactory();

        ProcessConfiguration runner = BuildConfig("sudo", "-u root");
        ProcessConfiguration target = BuildConfig("app.exe", "--flag value");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        // Exact sequence: a target that does not start with a dash must gain no "--"
        // terminator, because some runners (e.g. Windows runas) treat the terminator
        // positionally and the command would stop meaning what the caller composed.
        string[] expected = ["-u", "root", "app.exe", "--flag", "value"];
        Assert.Equal(expected, result.ArgumentsList);
    }

    [Fact]
    public void CreateRunnerConfiguration_DashTarget_TerminatesRunnerOptionZone()
    {
        IRunnerProcessFactory factory = new RunnerProcessFactory();

        // An option-parsing runner (sudo on POSIX and Windows) would otherwise bind a
        // dash-prefixed target into its own option zone; the terminator must sit as a
        // discrete token between the runner's options and the target.
        ProcessConfiguration runner = BuildConfig("sudo", "-u root");
        ProcessConfiguration target = BuildConfig("-preserve-env=LD_PRELOAD", "/bin/sh");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        string[] expected = ["-u", "root", "--", "-preserve-env=LD_PRELOAD", "/bin/sh"];
        Assert.Equal(expected, result.ArgumentsList);
    }

    [Fact]
    public void CreateRunnerConfiguration_DashTarget_NoRunnerArgs_TerminatorIsFirstToken()
    {
        IRunnerProcessFactory factory = new RunnerProcessFactory();

        ProcessConfiguration runner = new ProcessConfigurationBuilder("sudo").Build();
        ProcessConfiguration target = BuildConfig("-preserve-env=LD_PRELOAD", "/bin/sh");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        string[] expected = ["--", "-preserve-env=LD_PRELOAD", "/bin/sh"];
        Assert.Equal(expected, result.ArgumentsList);
    }

    [Fact]
    public void CreateRunnerConfiguration_PowerShellRunner_DashTarget_NeverAddsTerminator()
    {
        IRunnerProcessFactory factory = new RunnerProcessFactory();

        // A PowerShell runner keeps its call operator and never receives a terminator:
        // the composed command is a PowerShell script, where "--" would be script text.
        ProcessConfiguration runner = BuildConfig("pwsh.exe", "-NoProfile -Command");
        ProcessConfiguration target = BuildConfig("-preserve-env=LD_PRELOAD", "/bin/sh");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        string[] expected = ["-NoProfile", "-Command", "&", "-preserve-env=LD_PRELOAD", "/bin/sh"];
        Assert.Equal(expected, result.ArgumentsList);
        Assert.DoesNotContain("--", result.ArgumentsList);
    }
}
