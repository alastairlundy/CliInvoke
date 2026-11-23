/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.ComponentModel;

using CliInvoke.Processes.Internal;
using CliInvoke.Processes.Internal.ControlAdapters;

namespace CliInvoke.Tests.Processes;

/// <summary>
///     Direct tests for <see cref="BaseProcessControlAdapter.ApplyPolicySetting"/>'s exit-race filter.
/// </summary>
public class ApplyPolicySettingTests
{
    /// <summary>The platform's benign exit-race code.</summary>
    private static int BenignErrorCode => OperatingSystem.IsWindows() ? 5 : 3;

    [Test]
    public async Task ApplyPolicySetting_BenignCode_ExitedChild_DoesNotThrow()
    {
        using ProcessWrapper process = ProcessTestHelper.CreateProcess(
            ProcessTestHelper.GetTargetFilePath(), ProcessTestHelper.GetExitingArguments());

        process.Start();
        process.WaitForExit();
        await Assert.That(process.HasExited).IsTrue();

        // Exited child: the benign exception is swallowed.
        await Assert.That(() => BaseProcessControlAdapter.ApplyPolicySetting(
                process, () => throw new Win32Exception(BenignErrorCode)))
            .ThrowsNothing();
    }

    [Test]
    public async Task ApplyPolicySetting_BenignCode_RunningChild_ThrowsWin32Exception()
    {
        // Long-lived child so the exit-status check reports it as running.
        ProcessWrapper process = ProcessTestHelper.CreateProcess(
            OperatingSystem.IsWindows() ? "cmd" : "sleep",
            OperatingSystem.IsWindows() ? "/c ping -n 30 127.0.0.1 > nul" : "30");

        try
        {
            process.Start();
            await Assert.That(process.HasStarted).IsTrue();

            // Live child: the benign code alone must not swallow the failure.
            await Assert.That(() => BaseProcessControlAdapter.ApplyPolicySetting(
                    process, () => throw new Win32Exception(BenignErrorCode)))
                .Throws<Win32Exception>();
        }
        finally
        {
            process.ForcefulExit();
            process.WaitForExit();
            process.Dispose();
        }
    }

    [Test]
    public async Task ApplyPolicySetting_NonBenignCode_ExitedChild_ThrowsWin32Exception()
    {
        // 87 never matches 5 (Windows) or 3 (Unix).
        const int nonBenignErrorCode = 87;

        using ProcessWrapper process = ProcessTestHelper.CreateProcess(
            ProcessTestHelper.GetTargetFilePath(), ProcessTestHelper.GetExitingArguments());

        process.Start();
        process.WaitForExit();
        await Assert.That(process.HasExited).IsTrue();

        // Wrong code: the filter short-circuits before the exit check.
        await Assert.That(() => BaseProcessControlAdapter.ApplyPolicySetting(
                process, () => throw new Win32Exception(nonBenignErrorCode)))
            .Throws<Win32Exception>();
    }

    [Test]
    public async Task
        ApplyPolicySetting_NonWin32Exception_ExitedChild_ThrowsInvalidOperationException()
    {
        using ProcessWrapper process = ProcessTestHelper.CreateProcess(
            ProcessTestHelper.GetTargetFilePath(), ProcessTestHelper.GetExitingArguments());

        process.Start();
        process.WaitForExit();
        await Assert.That(process.HasExited).IsTrue();

        // The filter only catches Win32Exception.
        await Assert.That(() => BaseProcessControlAdapter.ApplyPolicySetting(
                process,
                () => throw new InvalidOperationException("policy setter failed.")))
            .Throws<InvalidOperationException>();
    }
}
