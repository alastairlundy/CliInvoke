/*
    AlastairLundy.CliInvoke.Core 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.DotExtensions.Processes;

// ReSharper disable AsyncVoidLambda
// ReSharper disable RedundantJumpStatement

namespace AlastairLundy.CliInvoke.Internal;

/// <summary>
/// Waits for the specified process to exit within the given time span.
/// </summary>
internal static class ProcessWaitForExitAsyncExtensions
{
    /// <summary>
    /// Waits for the specified process to exit or for the timeout time, whichever is sooner.
    /// </summary>
    /// <param name="process">The process to wait for.</param>
    /// <param name="timeoutPolicy"></param>
    /// <param name="cancellationToken">A cancellation token that determines whether the operation should continue to run or be cancelled.</param>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    internal static async Task WaitForExitOrTimeoutAsync(this Process process,
        ProcessTimeoutPolicy timeoutPolicy,
        CancellationToken cancellationToken = default)
    {
        if (timeoutPolicy.Equals(ProcessTimeoutPolicy.None))
        {
            await process.WaitForExitAsync(cancellationToken);
            return;
        }
        
        Task processTask = new Task(async () =>
        {
            await process.WaitForExitAsync(cancellationToken);

            return;
        });
            
        processTask.Start();

        if (timeoutPolicy.CancellationMode == ProcessCancellationMode.None)
        {
            await processTask;
            return;
        }
        
        Task timeoutTask = new Task(() =>
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            stopWatch.Start();
            
            while (stopWatch.IsRunning && process.IsRunning())
            {
                if (stopWatch.Elapsed > timeoutPolicy.TimeoutThreshold)
                {
                    stopWatch.Stop();

                    if (timeoutPolicy.CancellationMode == ProcessCancellationMode.Forceful)
                    {

                        process.Kill(true);
                    }
                    else
                    {
                        process.CloseMainWindow();
                        cancellationToken = new CancellationToken(true);
                    }
                        
                    return;
                }

                if (timeoutPolicy.TimeoutThreshold.TotalMilliseconds >= 100)
                {
                    Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                else
                {
                    Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
        });
        
        timeoutTask.Start();
            
        await timeoutTask;
    }
}