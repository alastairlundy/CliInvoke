/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Runtime.InteropServices;

namespace CliInvoke.Core;

/// <summary>
///     A Piped ProcessResult containing a Process's or Command's StandardOutput and StandardError
///     information.
/// </summary>
public class PipedProcessResult
    : ProcessResult,
        IEquatable<PipedProcessResult>,
        IDisposable,
        IAsyncDisposable
{
    /// <summary>
    ///     Initialises the PipedProcessResult with process information.
    /// </summary>
    /// <param name="executableFilePath">The file path of the file that was executed.</param>
    /// <param name="exitCode">The process' exit code.</param>
    /// <param name="processId"></param>
    /// <param name="startTime">The start time of the process.</param>
    /// <param name="exitTime">The exit time of the process.</param>
    /// <param name="standardOutput">The process' standard output.</param>
    /// <param name="standardError">The process' standard error.</param>
    /// <param name="canceled">
    ///     A value indicating whether the library terminated the process via its cancellation
    ///     machinery rather than the process exiting on its own.
    /// </param>
    /// <param name="signal">
    ///     The POSIX signal that terminated the process, or <c>null</c> when not applicable.
    /// </param>
    public PipedProcessResult(
        string executableFilePath,
        int exitCode,
        int processId,
        DateTime startTime,
        DateTime exitTime,
        Stream standardOutput,
        Stream standardError,
        bool canceled,
        PosixSignal? signal
    )
        : base(executableFilePath, exitCode, processId, startTime, exitTime, canceled, signal)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableFilePath);

        ArgumentNullException.ThrowIfNull(standardOutput);
        ArgumentNullException.ThrowIfNull(standardError);

        StandardOutput = standardOutput;
        StandardError = standardError;
    }

    /// <summary>
    ///     The Standard Output from a Process or Command represented as a Pipe.
    /// </summary>
    public Stream StandardOutput { get; }

    /// <summary>
    ///     The Standard Error from a Process or Command represented as a Pipe.
    /// </summary>
    public Stream StandardError { get; }

    /// <summary>
    ///     Disposes of the <see cref="StandardOutput" /> and <see cref="StandardError" /> streams
    ///     asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await StandardOutput.DisposeAsync();
        await StandardError.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Disposes of the <see cref="StandardOutput" /> and <see cref="StandardError" /> streams.
    /// </summary>
    public void Dispose()
    {
        StandardOutput.Dispose();
        StandardError.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Determines whether this PipedProcessResult object is equal to another PipedProcessResult
    ///     object.
    /// </summary>
    /// <remarks>
    ///     All fields including StartTime and ExitTime are considered for equality comparison,
    ///     consistent with <see cref="GetHashCode"/>.
    /// </remarks>
    /// <param name="other">The other PipedProcessResult to compare.</param>
    /// <returns>True if this PipedProcessResult is equal to the other PipedProcessResult; false otherwise.</returns>
    public bool Equals(PipedProcessResult? other)
    {
        if (other is null)
            return false;

#pragma warning disable CA1416
        return ExecutedFilePath == other.ExecutedFilePath &&
               StandardOutput.Equals(other.StandardOutput)
               && StandardError.Equals(other.StandardError)
               && ExitCode.Equals(other.ExitCode)
               && StartTime.Equals(other.StartTime)
               && ExitTime.Equals(other.ExitTime)
               && Canceled.Equals(other.Canceled)
               && Signal.Equals(other.Signal);
#pragma warning restore CA1416
    }

    /// <summary>
    ///     Determines whether this PipedProcessResult object is equal to another object.
    /// </summary>
    /// <param name="obj">The other object to compare.</param>
    /// <returns>
    ///     True if the other object is a PipedProcessResult and is equal to this PipedProcessResult;
    ///     false otherwise.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (obj.GetType() != typeof(PipedProcessResult))
            return false;

        if (obj is PipedProcessResult pipedProcessResult)
            return Equals(pipedProcessResult);

        return false;
    }

    /// <summary>
    ///     Returns the hash code for the current PipedProcessResult.
    /// </summary>
    /// <returns>The hash code for the current PipedProcessResult.</returns>
    public override int GetHashCode()
    {
#pragma warning disable CA1416
        return HashCode.Combine(ExecutedFilePath, ExitCode, StartTime, ExitTime, StandardOutput,
            StandardError, Canceled, Signal);
#pragma warning restore CA1416
    }

    /// <summary>
    ///     Determines whether two PipedProcessResults are equal.
    /// </summary>
    /// <param name="left">The first PipedProcessResult to compare.</param>
    /// <param name="right">The second PipedProcessResult to compare.</param>
    /// <returns>True if the two PipedProcessResult objects are equal; false otherwise.</returns>
    public static bool Equals(PipedProcessResult? left, PipedProcessResult? right)
    {
        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    ///     Determines if a PipedProcessResult is equal to another PipedProcessResult.
    /// </summary>
    /// <param name="left">A PipedProcessResult to be compared.</param>
    /// <param name="right">The other PipedProcessResult to be compared.</param>
    /// <returns>True if both PipedProcessResults are equal to each other; false otherwise.</returns>
    public static bool operator ==(PipedProcessResult? left, PipedProcessResult? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    ///     Determines if a PipedProcessResult is not equal to another PipedProcessResult.
    /// </summary>
    /// <param name="left">A PipedProcessResult to be compared.</param>
    /// <param name="right">The other PipedProcessResult to be compared.</param>
    /// <returns>True if both PipedProcessResults are not equal to each other; false otherwise.</returns>
    public static bool operator !=(PipedProcessResult? left, PipedProcessResult? right)
    {
        return !Equals(left, right);
    }
}