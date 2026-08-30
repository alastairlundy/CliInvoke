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
}
