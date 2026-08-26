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
///     Verifies that <see cref="RunnerProcessFactory"/> escapes the caller's
///     <c>TargetFilePath</c> and <c>Arguments</c> before embedding them in the
///     wrapped shell command line, neutralising command-injection metacharacters.
/// </summary>
public class RunnerProcessFactoryTests
{
    private static ProcessConfiguration BuildConfig(string targetFilePath, string arguments)
        => new ProcessConfigurationBuilder(targetFilePath).SetArguments(arguments).Build();

    [Fact]
    public void CreateRunnerConfiguration_EscapesCmdMetacharacters()
    {
        IRunnerProcessFactory factory = new RunnerProcessFactory();

        ProcessConfiguration runner = BuildConfig("cmd.exe", "/c ");
        ProcessConfiguration target = BuildConfig(@"C:\program files\app.exe", "echo hi & del c:\\temp ^ > nul");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        // The ampersand/redirection/caret must be escaped with carets so cmd.exe
        // does not re-interpret them as additional commands or redirection.
        Assert.Contains("^& del", result.Arguments);
        Assert.Contains("^>", result.Arguments);
        Assert.Contains("^^", result.Arguments);
        // The target path is quoted and the metacharacters are escaped, never left bare.
        Assert.Contains("\"C:\\program files\\app.exe\"", result.Arguments);
    }

    [Fact]
    public void CreateRunnerConfiguration_EscapesPowerShellMetacharacters()
    {
        IRunnerProcessFactory factory = new RunnerProcessFactory();

        ProcessConfiguration runner = BuildConfig("pwsh.exe", "-NoProfile -NonInteractive -Command");
        ProcessConfiguration target = BuildConfig("app.exe", "Get-Process ; echo hi & evil");

        ProcessConfiguration result = factory.CreateRunnerConfiguration(target, runner);

        // Semicolon and ampersand must be escaped with backticks so PowerShell does
        // not re-interpret them as additional statements/commands. (The leading `&` is
        // PowerShell's own call operator for the target and is intentionally not escaped.)
        Assert.Contains("`; echo", result.Arguments);
        Assert.Contains("`& evil", result.Arguments);
    }
}
