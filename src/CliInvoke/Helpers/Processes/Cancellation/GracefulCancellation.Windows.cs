/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Runtime.InteropServices;
using System.Threading;

namespace CliInvoke.Helpers.Processes.Cancellation;

internal static partial class GracefulCancellation
{
    extension(Process process)
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeoutThreshold"></param>
        /// <param name="cancellationExceptionBehavior"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="PlatformNotSupportedException"></exception>
        [SupportedOSPlatform("windows")]
        internal async Task CancelWithInterruptOnWindows(TimeSpan timeoutThreshold,
            ProcessCancellationExceptionBehavior cancellationExceptionBehavior, CancellationToken cancellationToken)
        {
            if(!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException();

            await Task.Delay(timeoutThreshold, cancellationToken);

            if()
            
            await SendSignal(process.Id);
        }
    }
    
    private static Task SendSignal(int processId)
    {
        
    }

}