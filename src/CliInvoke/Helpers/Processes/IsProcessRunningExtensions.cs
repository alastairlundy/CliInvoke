
using System;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace AlastairLundy.CliInvoke.Helpers.Processes;

internal static class IsProcessRunningExtensions
{
    /// <summary>
    /// Check to see if a specified process is running or not.
    /// </summary>
    /// <param name="process">The process to be checked.</param>
    /// <returns>True if the specified process is running; returns false otherwise.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    internal static bool IsRunning(this Process process) => 
        DotExtensions.Processes.ProcessHasStartedOrExitedExtensions.HasStarted(process) && DotExtensions.Processes.ProcessHasStartedOrExitedExtensions.HasExited(process) == false;
    
    /// <summary>
    /// Determines whether a process exists on a remote device or locally.
    /// </summary>
    /// <param name="process"></param>
    /// <returns>True if the process exists on a remote device, false if it exists locally.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the process has been disposed of.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    internal static bool IsProcessOnRemoteDevice(this Process process)
    {
        if (process.IsDisposed())
            throw new InvalidOperationException();

        try
        {
            bool hasExited = process.HasExited;

            if (hasExited)
                return false;
            
            return hasExited;
        }
        catch (NotSupportedException exception)
        {
            return true;
        }
    }
}