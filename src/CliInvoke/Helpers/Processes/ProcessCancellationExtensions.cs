/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Threading;

using CliInvoke.Helpers.Processes.Cancellation;

namespace CliInvoke.Helpers.Processes;

/// <summary>
///
/// </summary>
internal static class ProcessCancellationExtensions
{
    /// <param name="process">The process to cancel.</param>
    extension(Process process)
    {
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
        [SupportedOSPlatform("android")]
        internal async Task WaitForExitOrTimeoutAsync(ProcessExitConfiguration processExitConfiguration,
            CancellationToken cancellationToken = default)
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
                    await process.WaitForExitOrGracefulTimeoutAsync(processExitConfiguration.TimeoutPolicy.TimeoutThreshold,
                        processExitConfiguration.CancellationExceptionBehavior, cancellationToken);
                    return;
                }
                case ProcessCancellationMode.Forceful:
                    await process.WaitForExitOrForcefulTimeoutAsync(processExitConfiguration.TimeoutPolicy.TimeoutThreshold,
                        processExitConfiguration.CancellationExceptionBehavior, cancellationToken);
                    return;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
