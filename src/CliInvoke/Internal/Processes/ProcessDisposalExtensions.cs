

using System.Diagnostics;

namespace AlastairLundy.CliInvoke.Internal.Processes;

/// <summary>
/// 
/// </summary>
public static class ProcessDisposalExtensions
{
    /// <summary>
    /// Determines if a process has been disposed.
    /// </summary>
    /// <param name="process">The process to check if the process has been disposed of.</param>
    /// <returns>True if the process has been disposed of, false otherwise.</returns>
    internal static bool IsDisposed(this Process process)
    {
        try
        {
            return string.IsNullOrEmpty(process.Id.ToString());
        }
        catch
        {
            return true;
        }
    }

}