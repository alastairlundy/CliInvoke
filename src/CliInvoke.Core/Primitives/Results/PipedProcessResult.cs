/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

#if NET8_0_OR_GREATER
using System.Threading.Tasks;
#endif

namespace CliInvoke.Core;

/// <summary>
/// A Piped ProcessResult containing a Process's or Command's StandardOutput and StandardError information.
/// </summary>
public class PipedProcessResult
    : ProcessResult,
        IEquatable<PipedProcessResult>,
        IDisposable
#if NET8_0_OR_GREATER
        ,
        IAsyncDisposable
#endif
{
    /// <summary>
    /// The Standard Output from a Process or Command represented as a Pipe.
    /// </summary>
    public Stream StandardOutput { get; }

    /// <summary>
    /// The Standard Error from a Process or Command represented as a Pipe.
    /// </summary>
    public Stream StandardError { get; }

    /// <summary>
    /// Initializes the PipedProcessResult with process information.
    /// </summary>
    /// <param name="executableFilePath">The file path of the file that was executed.</param>
    /// <param name="exitCode">The process' exit code.</param>
    /// <param name="startTime">The start time of the process.</param>
    /// <param name="exitTime">The exit time of the process.</param>
    /// <param name="standardOutput">The process' standard output.</param>
    /// <param name="standardError">The process' standard error.</param>
    public PipedProcessResult(
        string executableFilePath,
        int exitCode,
        DateTime startTime,
        DateTime exitTime,
        Stream standardOutput,
        Stream standardError
    )
        : base(executableFilePath, exitCode, startTime, exitTime)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableFilePath);
        
        ArgumentNullException.ThrowIfNull(standardOutput);
        ArgumentNullException.ThrowIfNull(standardError);
        
        StandardOutput = standardOutput;
        StandardError = standardError;
    }

    /// <summary>
    /// Determines whether this PipedProcessResult is equal to another PipedProcessResult object.
    /// </summary>
    /// <remarks>This method intentionally does not consider Start and Exit times of Command Results for equality comparison.</remarks>
    /// <param name="other">The other PipedProcessResult to compare.</param>
    /// <returns>True if this PipedProcessResult is equal to the other PipedProcessResult; false otherwise.</returns>
    public bool Equals(PipedProcessResult? other)
    {
        if (other is null)
            return false;

        return ExecutedFilePath == other.ExecutedFilePath &&
               StandardOutput.Equals(other.StandardOutput)
               && StandardError.Equals(other.StandardError)
               && ExitCode.Equals(other.ExitCode)
               && StartTime.Equals(other.StartTime)
               && ExitTime.Equals(other.ExitTime);
    }

    /// <summary>
    /// Determines whether this PipedProcessResult is equal to another object.
    /// </summary>
    /// <param name="obj">The other object to compare.</param>
    /// <returns>True if the other object is a PipedProcessResult and is equal to this PipedProcessResult; false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (obj is PipedProcessResult pipedProcessResult)
            return Equals(pipedProcessResult);

        return false;
    }

    /// <summary>
    /// Returns the hash code for the current PipedProcessResult.
    /// </summary>
    /// <returns>The hash code for the current PipedProcessResult.</returns>
    public override int GetHashCode() => HashCode.Combine(ExecutedFilePath, ExitCode, StartTime, ExitTime, StandardOutput, StandardError);

    /// <summary>
    /// Determines whether two PipedProcessResults are equal.
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
    /// Determines if a PipedProcessResult is equal to another PipedProcessResult.
    /// </summary>
    /// <param name="left">A PipedProcessResult to be compared.</param>
    /// <param name="right">The other PipedProcessResult to be compared.</param>
    /// <returns>True if both PipedProcessResults are equal to each other; false otherwise.</returns>
    public static bool operator ==(PipedProcessResult? left, PipedProcessResult? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines if a PipedProcessResult is not equal to another PipedProcessResult.
    /// </summary>
    /// <param name="left">A PipedProcessResult to be compared.</param>
    /// <param name="right">The other PipedProcessResult to be compared.</param>
    /// <returns>True if both PipedProcessResults are not equal to each other; false otherwise.</returns>
    public static bool operator !=(PipedProcessResult? left, PipedProcessResult? right) =>
        Equals(left, right) == false;

    /// <summary>
    /// Disposes of the <see cref="StandardOutput"/> and <see cref="StandardError"/> streams.
    /// </summary>
    public void Dispose()
    {
        StandardOutput.Dispose();
        StandardError.Dispose();

        GC.SuppressFinalize(this);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Disposes of the <see cref="StandardOutput"/> and <see cref="StandardError"/> streams asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await StandardOutput.DisposeAsync();
        await StandardError.DisposeAsync();

        GC.SuppressFinalize(this);
    }
#endif
}
