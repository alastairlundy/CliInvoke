

using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace AlastairLundy.CliInvoke.Magic.Processes;

/// <summary>
/// 
/// </summary>
internal static class ProcessCancellationExtensions
{

    /// <summary>
    /// Asynchronously waits for the process to exit or for the <paramref name="timeoutThreshold"/> to be exceeded, whichever is sooner.
    /// </summary>
    /// <param name="process">The process to cancel.</param>
    /// <param name="timeoutThreshold">The delay to wait before requesting cancellation.</param>
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
    internal static async Task WaitForExitOrTimeoutAsync(this Process process,TimeSpan timeoutThreshold)
    {
        if (timeoutThreshold < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException();
        
        if (process.IsRunningOnRemoteDevice())
            throw new NotSupportedException();
        
        CancellationTokenSource cts = new CancellationTokenSource();
        
        cts.CancelAfter(timeoutThreshold);
        
        await process.WaitForExitAsync(cts.Token);
    }
}