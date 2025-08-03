/*
        MIT License
       
       Copyright (c) 2025 Alastair Lundy
       
       Permission is hereby granted, free of charge, to any person obtaining a copy
       of this software and associated documentation files (the "Software"), to deal
       in the Software without restriction, including without limitation the rights
       to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
       copies of the Software, and to permit persons to whom the Software is
       furnished to do so, subject to the following conditions:
       
       The above copyright notice and this permission notice shall be included in all
       copies or substantial portions of the Software.
       
       THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
       IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
       FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
       AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
       LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
       OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
       SOFTWARE.
   */

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Core.Primitives;

using AlastairLundy.DotExtensions.Processes;

// ReSharper disable AsyncVoidLambda
// ReSharper disable RedundantJumpStatement

namespace AlastairLundy.CliInvoke.Extensions.Internal;

/// <summary>
/// Waits for the specified process to exit within the given time span.
/// </summary>
internal static class ProcessWaitForExitAsyncExtensions
{
    /// <summary>
    /// Waits for the specified process to exit or for the timeout time, whichever is sooner.
    /// </summary>
    /// <param name="process">The process to wait for.</param>
    /// <param name="timeout">The timeout timespan to wait for before cancelling.</param>
    /// <param name="cancellationMode">The cancellation mode to use in case the Process hasn't exited before the timeout time.</param>
    /// <param name="cancellationToken">A cancellation token that determines whether the operation should continue to run or be cancelled.</param>
    internal static async Task WaitForExitAsync(this Process process,
        TimeSpan timeout,
        ProcessCancellationMode cancellationMode,
        CancellationToken cancellationToken = default)
    {
        Task processTask = new Task(async () =>
        {
            await process.WaitForExitAsync(cancellationToken);

            return;
        });
            
        processTask.Start();

        if (cancellationMode == ProcessCancellationMode.None)
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
                if (stopWatch.Elapsed > timeout)
                {
                    stopWatch.Stop();

                    if (cancellationMode == ProcessCancellationMode.Forceful)
                    {
#if NET5_0_OR_GREATER
                        process.Kill(true);
#else
                        process.Kill();
#endif
                    }
                    else
                    {
                        process.CloseMainWindow();
                        cancellationToken = new CancellationToken(true);
                    }
                        
                    return;
                }

                if (timeout.TotalMilliseconds >= 100)
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

    /// <summary>
    /// Waits for the specified process to exit or for the ProcessTimeoutPolicy's timeout time, whichever is sooner.
    /// </summary>
    /// <param name="process">The process to wait for.</param>
    /// <param name="timeoutPolicy">The ProcessTimeoutPolicy to use for the process.</param>
    /// <param name="cancellationToken">A cancellation token that determines whether the operation
    /// should continue to run or be cancelled.</param>
    internal static async Task WaitForExitAsync(this Process process,
        ProcessTimeoutPolicy timeoutPolicy,
        CancellationToken cancellationToken = default)
    {
        await WaitForExitAsync(process, timeoutPolicy.TimeoutThreshold, timeoutPolicy.CancellationMode, cancellationToken);
    }
}