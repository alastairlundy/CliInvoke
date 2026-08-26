/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

    Based on Tyrrrz's CliWrap CommandResult.cs
    https://github.com/Tyrrrz/CliWrap/blob/master/CliWrap/CommandResult.cs

     Constructor signature and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace CliInvoke.Core;

/// <summary>
///     A class that represents the results from an executed Process or Command.
/// </summary>
public class ProcessResult : IEquatable<ProcessResult>
{
    /// <summary>
    ///     Instantiates a ProcessResult with data about a Process' execution.
    /// </summary>
    /// <param name="executableFilePath">The file path of the file that was executed.</param>
    /// <param name="exitCode">The process' exit code.</param>
    /// <param name="processId"></param>
    /// <param name="startTime">The start time of the process.</param>
    /// <param name="exitTime">The exit time of the process.</param>
    /// <param name="canceled">
    ///     A value indicating whether the library terminated the process via its cancellation
    ///     machinery rather than the process exiting on its own.
    /// </param>
    /// <param name="signal">
    ///     The POSIX signal that terminated the process, or <c>null</c> when not applicable.
    /// </param>
    public ProcessResult(
        string executableFilePath,
        int exitCode,
        int processId,
        DateTime startTime,
        DateTime exitTime,
        bool canceled,
        PosixSignal? signal)
    {
        ExitCode = exitCode;
        ExecutedFilePath = executableFilePath;
        StartTime = startTime;
        ExitTime = exitTime;
        ProcessId = processId;
        Canceled = canceled;
#pragma warning disable CA1416
        Signal = signal;
#pragma warning restore CA1416
    }

    /// <summary>
    ///     The unique identifier assigned to the process when it was executed.
    /// </summary>
    public int ProcessId { get; }

    /// <summary>
    ///     The exit code from the Command that was executed.
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    ///     The file path of the file to be executed.
    /// </summary>
    public string ExecutedFilePath { get; }

    /// <summary>
    ///     The Date and Time that the Command's execution started.
    /// </summary>
    public DateTime StartTime { get; }

    /// <summary>
    ///     The Date and Time that the Command's execution finished.
    /// </summary>
    public DateTime ExitTime { get; }

    /// <summary>
    ///     A value indicating whether the library terminated the process via its cancellation
    ///     machinery, as opposed to the process exiting on its own.
    /// </summary>
    public bool Canceled { get; }

    /// <summary>
    ///     The POSIX signal that terminated the process, surfaced on Unix only.
    /// </summary>
    /// <remarks>
    ///     This is a best-effort heuristic derived from <see cref="ExitCode" />
    ///     (an <c>ExitCode &gt; 128</c> is interpreted as termination by signal
    ///     <c>ExitCode - 128</c>). It is not authoritative and is <c>null</c> on Windows.
    /// </remarks>
    [UnsupportedOSPlatform("windows")]
    public PosixSignal? Signal { get; }

    /// <summary>
    ///     How long the Command took to execute represented as a TimeSpan.
    /// </summary>
    public TimeSpan RuntimeDuration => ExitTime.Subtract(StartTime);

    /// <summary>
    ///     Determines whether the specified <see cref="ProcessResult" /> instance is equal to the current
    ///     instance.
    /// </summary>
    /// <param name="other">The <see cref="ProcessResult" /> instance to compare with the current instance.</param>
    /// <returns>
    ///     <c>True</c> if the specified <see cref="ProcessResult" /> is equal to the current instance;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(ProcessResult? other)
    {
        if (other is null)
            return false;

        if (other.GetType() != GetType())
            return false;

#pragma warning disable CA1416
        return ExitCode == other.ExitCode
               && ExecutedFilePath == other.ExecutedFilePath
               && ProcessId == other.ProcessId
               && StartTime == other.StartTime
               && ExitTime == other.ExitTime
               && Canceled == other.Canceled
               && Signal == other.Signal;
#pragma warning restore CA1416
    }

    /// <summary>
    ///     Determines whether this ProcessResult is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with this ProcessResult.</param>
    /// <returns>
    ///     <c>True</c> if the specified object is a ProcessResult and is equal to this instance;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (obj.GetType() != GetType())
            return false;

        return Equals((ProcessResult)obj);
    }

    /// <summary>
    ///     Returns the hash code for the current ProcessResult.
    /// </summary>
    /// <returns>The hash code for the current ProcessResult.</returns>
    public override int GetHashCode()
    {
#pragma warning disable CA1416
        return HashCode.Combine(ExitCode, ExecutedFilePath, ProcessId, StartTime, ExitTime, Canceled, Signal);
#pragma warning restore CA1416
    }

    /// <summary>
    ///     Determines whether two specified <see cref="ProcessResult" /> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessResult" /> instance to compare.</param>
    /// <param name="right">The second <see cref="ProcessResult" /> instance to compare.</param>
    /// <returns>
    ///     <c>true</c> if the specified <see cref="ProcessResult" /> instances are equal; otherwise,
    ///     <c>false</c>.
    /// </returns>
    public static bool Equals(ProcessResult? left, ProcessResult? right)
    {
        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    ///     Determines whether two specified <see cref="ProcessResult" /> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessResult" /> instance to compare.</param>
    /// <param name="right">The second <see cref="ProcessResult" /> instance to compare.</param>
    /// <returns>
    ///     <c>true</c> if the specified <see cref="ProcessResult" /> instances are equal; otherwise,
    ///     <c>false</c>.
    /// </returns>
    public static bool operator ==(ProcessResult left, ProcessResult? right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Determines whether two specified <see cref="ProcessResult" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessResult" /> instance to compare.</param>
    /// <param name="right">The second <see cref="ProcessResult" /> instance to compare.</param>
    /// <returns>
    ///     <c>true</c> if the specified <see cref="ProcessResult" /> instances are not equal; otherwise,
    ///     <c>false</c>.
    /// </returns>
    public static bool operator !=(ProcessResult left, ProcessResult? right)
    {
        return !left.Equals(right);
    }
}