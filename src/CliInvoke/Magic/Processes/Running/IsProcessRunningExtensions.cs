using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;

// ReSharper disable RedundantBoolCompare

namespace AlastairLundy.CliInvoke.Magic.Processes;

public static class IsProcessRunningExtensions
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
    public static bool IsRunning(this Process process) => 
        process.HasStarted() && process.HasExited() == false;

    /// <summary>
    /// Detects whether a process is running on a remote device.
    /// </summary>
    /// <param name="process"></param>
    /// <returns>True if the process is running on a remote device, false otherwise.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the process has been disposed of.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public static bool IsRunningOnRemoteDevice(this Process process)
    {
        if (process.IsRunning() == false)
            return false;

        if (process.IsDisposed())
        {
            throw new InvalidOperationException();
        }
        
        return Process.GetProcesses().All(x => x.Id != process.Id) && 
               process.MachineName.Equals(Environment.MachineName) == false;
    }
}