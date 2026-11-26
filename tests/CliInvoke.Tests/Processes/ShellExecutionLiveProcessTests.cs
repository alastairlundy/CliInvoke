/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Runtime.Versioning;

using CliInvoke.Processes.Internal;
using TUnit.Core.Exceptions;

namespace CliInvoke.Tests.Processes;

/// <summary>
///     Covers a live shell-executed start through <see cref="ProcessWrapper.Start"/>. Shell
///     execution cannot be combined with redirected streams, so this path skips the stream
///     caching in <c>Start</c> but still runs the OnStarted suspend/apply/resume cycle against
///     the held process handle.
/// </summary>
public class ShellExecutionLiveProcessTests
{
    [Test]
    [SupportedOSPlatform("windows")]
    public async Task Start_LiveShellExecutedChild_RemainsRunning()
    {
        SkipTestIfNotWindows();

        // cmd's own >nul keeps the ping output off the test host's console; the ping keeps the
        // child alive long enough to observe it. OutputRedirection and RedirectStandardInput
        // must both stay off: Process.Start rejects redirected streams with UseShellExecute.
        FileInfo resolvedCmd = new FilePathResolver().ResolveFilePath("cmd.exe");
        ProcessConfiguration configuration = new(resolvedCmd.FullName,
            "/c ping -n 30 127.0.0.1 >nul", outputRedirection: false)
        {
            UseShellExecution = true,
            RedirectStandardInput = false,
        };

        ProcessWrapper process = new(configuration, resolvedCmd);
        try
        {
            // OnStarted runs synchronously inside Start. A resume failure must neither
            // forceful-exit the child nor surface out of Start for a healthy child.
            bool started = process.Start();

            await Assert.That(started).IsTrue();
            await Assert.That(process.HasStarted).IsTrue();

            // Give a child killed by the resume path time to be reaped.
            await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(true);

            await Assert.That(process.HasExited).IsFalse();
        }
        finally
        {
            if (process.HasStarted && !process.HasExited)
            {
                try { process.Kill(true); }
                catch { try { process.Kill(); } catch { /* Already gone. */ } }
            }

            process.Dispose();
        }
    }

    private static void SkipTestIfNotWindows()
    {
        if (!OperatingSystem.IsWindows())
            throw new SkipTestException(
                "UseShellExecute for a live executable is only reliably supported on Windows; " +
                "the shell-executed resume path under test is in WindowsProcessControlAdapter.");
    }
}
