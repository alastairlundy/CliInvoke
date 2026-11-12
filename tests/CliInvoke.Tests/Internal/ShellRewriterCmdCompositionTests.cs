/*
    CliInvoke.Tests
    Copyright (C) 2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Runtime.Versioning;
using CliInvoke.Core.Extensibility;
using CliInvoke.Factories;
using CliInvoke.Internal;

using TUnit.Core.Exceptions;

namespace CliInvoke.Tests.Internal;

/// <summary>
///     Behaviour tests for the cmd composition <see cref="ShellRewriter"/> produces for
///     the shell middleware path: a plain-executable child receives the composed
///     command with the control characters kept as literal data (verified end-to-end
///     against real cmd.exe with a raw single-string calibration alongside), while
///     cmd.exe and batch children are covered by the factory-level rejection tests in
///     <c>RunnerConfigurationFactoryArgumentValidationTests</c> — compositions those
///     children would re-parse are refused before any process starts.
/// </summary>
public class ShellRewriterCmdCompositionTests
{
    private static ProcessConfiguration Compose(string targetFilePath, string arguments)
    {
        ProcessConfiguration source = new(targetFilePath, arguments);

        return ShellRewriter.Rewrite(
            source,
            shellTargetPath: "cmd.exe",
            runnerArgs: "/c",
            kind: ShellKind.Cmd,
            windowCreation: false,
            useShellExecution: false);
    }

    /// <summary>
    ///     Runs cmd.exe with <paramref name="arguments"/> as a single verbatim
    ///     <see cref="System.Diagnostics.ProcessStartInfo.Arguments"/> string. Used only
    ///     as a CALIBRATION control — proves the input shape fires when delivered raw,
    ///     so the composition assertion below has meaning.
    /// </summary>
    private static void RunRawSingleString(string arguments)
    {
        using System.Diagnostics.Process process = new()
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe")
            {
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        process.Start();
        process.WaitForExit(30000);
    }

    private static async Task RunComposedAsync(ProcessConfiguration composed)
    {
        IProcessInvoker invoker = new ProcessInvoker(new ExternalProcessFactory());
        try
        {
            _ = await invoker.ExecuteBufferedAsync(
                composed,
                ProcessExitConfiguration.CreateGraceful());
        }
        catch
        {
            // Missing-target errors are expected; the property under test is whether
            // the marker file was created.
        }
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task Rewrite_Cmd_ExeChildComposedArguments_DoNotProduceSecondCommand()
    {
        if (!OperatingSystem.IsWindows())
            throw new SkipTestException("This test requires Windows (cmd.exe).");

        string marker = Path.Combine(
            Path.GetTempPath(),
            "CliInvoke.ShellRewriterCmdComposition." + Guid.NewGuid().ToString("N") + ".txt");

        try
        {
            // Calibration: the raw single-string delivery of the same input shape
            // creates the marker, so the harness is capable of detecting it.
            RunRawSingleString(
                $"/c C:\\nonexistent\\app.exe x & echo pwned>\"{marker}\"");
            bool calibrationFired = File.Exists(marker);
            if (calibrationFired) File.Delete(marker);

            await Assert.That(calibrationFired).IsTrue();

            // The composed delivery of the same input through the cmd composition:
            // the exe child must receive the control characters as literal data and
            // no second command may run.
            ProcessConfiguration composed = Compose(
                @"C:\nonexistent\app.exe",
                "x & echo pwned>\"" + marker + "\"");

            await Assert.That(composed.Arguments).Contains("^&");

            await RunComposedAsync(composed);

            await Assert.That(File.Exists(marker)).IsFalse();
        }
        finally
        {
            if (File.Exists(marker)) File.Delete(marker);
        }
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task Rewrite_Cmd_ExeChildComposedExpansionValue_DoNotProduceSecondCommand()
    {
        if (!OperatingSystem.IsWindows())
            throw new SkipTestException("This test requires Windows (cmd.exe).");

        string marker = Path.Combine(
            Path.GetTempPath(),
            "CliInvoke.ShellRewriterCmdComposition." + Guid.NewGuid().ToString("N") + ".txt");

        // A benign environment value standing in for caller-supplied environment data:
        // the composed command must keep the reference inert for the exe child.
        Environment.SetEnvironmentVariable("CLIINVOKE_SPIKE_VALUE", $"ignored & echo pwned>{marker}");

        try
        {
            ProcessConfiguration composed = Compose(
                @"C:\nonexistent\app.exe",
                "%CLIINVOKE_SPIKE_VALUE%");

            await Assert.That(composed.Arguments).Contains("^%CLIINVOKE_SPIKE_VALUE^%");

            await RunComposedAsync(composed);

            await Assert.That(File.Exists(marker)).IsFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLIINVOKE_SPIKE_VALUE", null);
            if (File.Exists(marker)) File.Delete(marker);
        }
    }

    [Test]
    public async Task Rewrite_Cmd_ComposedShape_QuotesTargetAndEscapesArguments()
    {
        // Structural pin for the composed shape: the target token is quoted (the
        // containment half) and the argument text is caret-escaped, with the whole
        // inner command wrapped in the outer quoted region the /c parse strips.
        ProcessConfiguration composed = Compose(@"C:\program files\app.exe", "arg one & more");

        await Assert.That(composed.ArgumentList.Count).IsEqualTo(0);
        await Assert.That(composed.Arguments).StartsWith("/c \"");
        await Assert.That(composed.Arguments).EndsWith("\"");
        await Assert.That(composed.Arguments).Contains("\"C:\\program files\\app.exe\"");
        await Assert.That(composed.Arguments).Contains("^&");
    }
}
