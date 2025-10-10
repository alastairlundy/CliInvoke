


using System;
using System.Diagnostics;
using System.Runtime.Versioning;

using AlastairLundy.CliInvoke.Internal.Localizations;

namespace AlastairLundy.CliInvoke.Helpers.Processes;

internal static class ProcessHasStartedOrExitedExtensions
{
    /// <summary>
    /// Determines if a process has started.
    /// </summary>
    /// <param name="process">The process to be checked.</param>
    /// <returns>True if it has started; false otherwise.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    internal static bool HasStarted(this Process process)
    {
        if (process.IsProcessOnRemoteDevice())
            throw new NotSupportedException(Resources.Exceptions_Processes_NotSupportedOnRemoteProcess);
        
        try
        {
            return process.StartTime.ToUniversalTime() <= DateTime.UtcNow;
        }
        catch(InvalidOperationException)
        {
            return false;
        }
    }

    
    /*
     /// <summary>
       /// Determines if a process has exited.
       /// </summary>
       /// <remarks>This extension method exists because accessing the Exited property on a Process can cause an exception to be thrown.</remarks>
       /// <param name="process">The process to be checked.</param>
       /// <returns>True if it has exited; false if it is still running.</returns>
       /// <exception cref="NotSupportedException">Thrown if checking whether a Process has exited on a remote device.</exception>
      
     [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    internal static bool HasExited(this Process process)
    {
        if (process.IsProcessOnRemoteDevice())
            throw new NotSupportedException(Resources.Exceptions_Processes_NotSupportedOnRemoteProcess);
        
        /*try
        {#1#
        //return process.ExitTime.ToUniversalTime() <= DateTime.UtcNow;

        /*}
        catch
        {
            return false;
        }#1#
    }*/
}