/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Diagnostics;
using System.Text;

namespace CliInvoke.Tests.Helpers;

/// <summary>
///     Test-only helper that captures a point-in-time snapshot of child processes
///     by name, including their thread states. Used to diagnose CI flakiness
///     (e.g., distinguishing a cold-start stall from a suspended-start race or
///     a pipe deadlock) when a test's external guard timeout fires.
/// </summary>
public static class ChildProcessStateDiagnostics
{
    /// <summary>
    ///     Captures a diagnostic snapshot of all running processes whose name
    ///     contains <paramref name="processNameSubstring"/> (case-insensitive).
    /// </summary>
    /// <param name="processNameSubstring">
    ///     A substring to match against process names (e.g. "powershell", "pwsh").
    /// </param>
    /// <returns>
    ///     A multi-line string listing each matching process's PID, thread count,
    ///     suspended/running state, and elapsed time since start.
    /// </returns>
    public static string Capture(string processNameSubstring)
    {
        ArgumentNullException.ThrowIfNull(processNameSubstring);

        StringBuilder sb = new();
        sb.AppendLine($"Child process diagnostic (filter: \"{processNameSubstring}\") captured at {DateTime.UtcNow:O}");

        Process[] processes;
        try
        {
            processes = Process.GetProcesses();
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  Failed to enumerate processes: {ex.Message}");
            return sb.ToString();
        }

        int matchCount = 0;
        foreach (Process process in processes)
        {
            try
            {
                if (process.ProcessName.IndexOf(processNameSubstring, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
            }
            catch
            {
                continue;
            }

            matchCount++;
            try
            {
                int pid = process.Id;
                int threadCount = 0;
                int suspendedCount = 0;
                bool allSuspended = true;
                bool anySuspended = false;

                // Computed before thread inspection so the failure path below can still report it.
                string elapsedText;
                try
                {
                    TimeSpan elapsed = DateTime.UtcNow - process.StartTime.ToUniversalTime();
                    elapsedText = $"{elapsed.TotalSeconds:F1}s";
                }
                catch
                {
                    // StartTime can throw (exited or access-restricted process); an
                    // explicit unknown beats a fake elapsed=0.0s in CI logs.
                    elapsedText = "unknown";
                }

                try
                {
                    foreach (ProcessThread thread in process.Threads)
                    {
                        threadCount++;
                        if (thread.ThreadState == System.Diagnostics.ThreadState.Wait
                            && thread.WaitReason == ThreadWaitReason.Suspended)
                        {
                            suspendedCount++;
                            anySuspended = true;
                        }
                        else
                        {
                            allSuspended = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"  PID {pid} ({process.ProcessName}): thread inspection failed: {ex.Message}, elapsed={elapsedText}");
                    continue;
                }

                string state = threadCount == 0
                    ? "no-threads"
                    : allSuspended
                        ? "ALL-SUSPENDED"
                        : anySuspended
                            ? $"PARTIAL ({suspendedCount}/{threadCount} suspended)"
                            : "running";

                sb.AppendLine($"  PID {pid} ({process.ProcessName}): threads={threadCount}, suspended={suspendedCount}, state={state}, elapsed={elapsedText}");
            }
            catch (Exception ex)
            {
                try { sb.AppendLine($"  PID {process.Id} ({process.ProcessName}): inspection failed: {ex.Message}"); }
                catch { sb.AppendLine($"  (process inspection failed: {ex.Message})"); }
            }
            finally
            {
                try { process.Dispose(); }
                catch { /* best effort */ }
            }
        }

        if (matchCount == 0)
            sb.AppendLine("  (no matching processes found)");

        return sb.ToString();
    }

    /// <summary>
    ///     Captures diagnostics for both <c>powershell</c> and <c>pwsh</c> processes.
    /// </summary>
    /// <returns>A combined diagnostic string.</returns>
    public static string CapturePowerShell()
    {
        StringBuilder sb = new();
        sb.Append(Capture("powershell"));
        sb.AppendLine();
        sb.Append(Capture("pwsh"));
        return sb.ToString();
    }
}