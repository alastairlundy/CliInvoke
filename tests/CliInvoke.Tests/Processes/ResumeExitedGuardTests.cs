/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Processes.Internal;

namespace CliInvoke.Tests.Processes;

/// <summary>
///     Pins the resume guard contract behind the permanent-zombie fix. The mid-termination
///     window itself needs stall injection to reproduce.
/// </summary>
public class ResumeExitedGuardTests
{
    [Test]
    public async Task ResumeProcess_ExitedChild_DefaultGuard_ThrowsInvalidOperationException()
    {
        ProcessWrapper process = StartQuietExitedChild();
        try
        {
            await Assert.That(process.HasExited).IsTrue();

            // Default callers keep the documented exited guard.
            await Assert.That(() => process.ResumeProcess())
                .Throws<InvalidOperationException>();
        }
        finally
        {
            Cleanup(process);
        }
    }

    [Test]
    public async Task ResumeProcess_ExitedChild_SkipExitedGuard_DoesNotThrow()
    {
        ProcessWrapper process = StartQuietExitedChild();
        try
        {
            await Assert.That(process.HasExited).IsTrue();

            // OnStarted's flag must reach the adapter instead of the guard.
            await Assert.That(() => process.ResumeProcess(skipExitedGuard: true))
                .ThrowsNothing();
        }
        finally
        {
            Cleanup(process);
        }
    }

    [Test]
    public async Task ForcefulExit_ExitedChild_SkipExitedGuard_DoesNotThrow()
    {
        ProcessWrapper process = StartQuietExitedChild();
        try
        {
            await Assert.That(process.HasExited).IsTrue();

            // The finally's kill path must run even when HasExited is true.
            await Assert.That(() => process.ForcefulExit(skipExitedGuard: true))
                .ThrowsNothing();
        }
        finally
        {
            Cleanup(process);
        }
    }

    [Test]
    public async Task ForcefulExit_ExitedChild_DefaultGuard_IsNoOp()
    {
        ProcessWrapper process = StartQuietExitedChild();
        try
        {
            await Assert.That(process.HasExited).IsTrue();

            // Default callers keep the guard: an exited child is not killed again.
            await Assert.That(() => process.ForcefulExit())
                .ThrowsNothing();
        }
        finally
        {
            Cleanup(process);
        }
    }

    /// <summary>Starts a quiet child and waits for it to exit.</summary>
    private static ProcessWrapper StartQuietExitedChild()
    {
        (string targetFilePath, string arguments) = ProcessTestHelper.GetQuietChildCommand();
        ProcessWrapper process = ProcessTestHelper.CreateProcess(targetFilePath, arguments);

        process.Start();
        process.WaitForExit();
        return process;
    }

    private static void Cleanup(ProcessWrapper process)
    {
        if (process.HasStarted && !process.HasExited)
        {
            try { process.Kill(true); } catch { process.Kill(); }
        }

        process.Dispose();
    }
}
