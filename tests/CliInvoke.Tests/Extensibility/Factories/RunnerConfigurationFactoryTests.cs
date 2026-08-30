/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Core.Extensibility;
using CliInvoke.Extensibility;

namespace CliInvoke.Tests.Extensibility.Factories;

/// <summary>
///     Verifies that <see cref="RunnerConfigurationFactory"/> composes the wrapped command
///     from discrete argument tokens, so caller-supplied values (including quotes and
///     shell metacharacters) are passed through to the wrapped process as isolated
///     literals instead of being re-parsed into additional command-line tokens.
/// </summary>
public class RunnerConfigurationFactoryTests
{
    private static ProcessConfiguration BuildConfig(string targetFilePath, string arguments)
        => new ProcessConfigurationBuilder(targetFilePath).SetArguments(arguments).Build();

    [Test]
    public async Task CreateRunnerConfiguration_KeepsTargetAndArgsAsDiscreteTokens()
    {
        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();

        ProcessConfiguration runner = BuildConfig("cmd.exe", "/c");
        ProcessConfiguration target = BuildConfig(@"C:\program files\app.exe", "arg one");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        // The runner flag, the target path (kept whole, even though it
        // contains a space) and each user argument are separate tokens rather than one
        // re-parsed string. A space-bearing target stays a single token.
        // Note: "&" is only added for PowerShell runners, not cmd.exe.
        await Assert.That(result.ArgumentsList).Contains("/c");
        await Assert.That(result.ArgumentsList).Contains(@"C:\program files\app.exe");
        await Assert.That(result.ArgumentsList).Contains("arg");
        await Assert.That(result.ArgumentsList).Contains("one");
    }

    [Test]
    public async Task CreateRunnerConfiguration_PowerShellRunner_AddsCallOperator()
    {
        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();

        ProcessConfiguration runner = BuildConfig("pwsh.exe", "-NoProfile -NonInteractive -Command");
        ProcessConfiguration target = BuildConfig("app.exe", "arg1");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        // PowerShell requires the & call operator to invoke a target.
        await Assert.That(result.ArgumentsList).Contains("&");
        await Assert.That(result.ArgumentsList).Contains("app.exe");
    }

    [Test]
    public async Task CreateRunnerConfiguration_DoesNotMergeCallerQuoteWithWrapper()
    {
        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();

        // A quote inside the caller's target must remain an isolated literal token, not
        // be concatenated with the wrapper's quoting into a single broken token.
        ProcessConfiguration runner = BuildConfig("pwsh.exe", "-NoProfile -NonInteractive -Command");
        ProcessConfiguration target = BuildConfig("app\"evil.exe", "safe");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        await Assert.That(result.ArgumentsList).Contains("app\"evil.exe");
        await Assert.That(result.ArgumentsList).DoesNotContain("& \"app");
    }
}
