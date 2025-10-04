/*
    AlastairLundy.CliInvoke 
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
using AlastairLundy.DotExtensions.Dates;

namespace AlastairLundy.CliInvoke.Helpers.Processes;

/// <summary>
/// 
/// </summary>
internal static class ProcessCancellationExtensions
{

    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    internal static async Task WaitForExitOrTimeoutAsync(this Process process,
        ProcessExitConfiguration processExitConfiguration, CancellationToken cancellationToken = default)
    {
        switch (processExitConfiguration.TimeoutPolicy.CancellationMode)
        {
            case ProcessCancellationMode.None:
            {
                await process.WaitForExitAsync(cancellationToken);
                return;
            }
            case ProcessCancellationMode.Graceful:
            {
                await WaitForExitOrGracefulTimeoutAsync(process, processExitConfiguration.TimeoutPolicy.TimeoutThreshold,
                    processExitConfiguration.CancellationExceptionBehavior);
                return;
            }
            case ProcessCancellationMode.Forceful:
                process.Kill();
                return;
            default:
                throw new NotSupportedException();
        }
    }
    
    /// <summary>
    /// Asynchronously waits for the process to exit or for the <paramref name="timeoutThreshold"/> to be exceeded, whichever is sooner.
    /// </summary>
    /// <param name="process">The process to cancel.</param>
    /// <param name="timeoutThreshold">The delay to wait before requesting cancellation.</param>
    /// <param name="cancellationExceptionBehavior"></param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the timeout threshold is less than 0.</exception>
    /// <exception cref="NotSupportedException">Thrown if run on a remote computer or device.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    private static async Task WaitForExitOrGracefulTimeoutAsync(this Process process,TimeSpan timeoutThreshold, 
        ProcessCancellationExceptionBehavior cancellationExceptionBehavior)
    {
        if (timeoutThreshold < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException();
        
        DateTime expectedExitTime = DateTime.UtcNow.Add(timeoutThreshold);
        
        try
        {  
            CancellationTokenSource cts = new CancellationTokenSource();
        
            cts.CancelAfter(timeoutThreshold);
            
            await process.WaitForExitAsync(cts.Token);
        }
        catch (TaskCanceledException)
        {
            DateTime actualExitTime = DateTime.UtcNow;
            
            if (cancellationExceptionBehavior == ProcessCancellationExceptionBehavior.SuppressException)
            {
                return;
            }

            if (cancellationExceptionBehavior == ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected ||
                cancellationExceptionBehavior == ProcessCancellationExceptionBehavior.AllowException)
            {
                if (actualExitTime.Abs(expectedExitTime) > TimeSpan.FromSeconds(10) || 
                    cancellationExceptionBehavior == ProcessCancellationExceptionBehavior.AllowException)
                {
                    throw;
                }
            }
        }
        
    }
    
        
        
        await process.WaitForExitAsync(cts.Token);
    }
}