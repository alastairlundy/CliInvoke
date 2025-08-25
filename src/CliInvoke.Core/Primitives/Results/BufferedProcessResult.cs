/*
    AlastairLundy.DotPrimitives 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

    Based on Tyrrrz's CliWrap BufferedCommandResult.cs
    https://github.com/Tyrrrz/CliWrap/blob/master/CliWrap/Buffered/BufferedCommandResult.cs

     Constructor signature and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

using System;

// ReSharper disable MemberCanBePrivate.Global


namespace AlastairLundy.CliInvoke.Core.Primitives;

/// <summary>
/// A buffered ProcessResult containing a Process's or Command's StandardOutput and StandardError information.
/// </summary>
public class BufferedProcessResult : ProcessResult, IEquatable<BufferedProcessResult>
{

    /// <summary>
    /// Initializes the BufferedProcessResult with process information.
    /// </summary>
    /// <param name="executableFilePath">The file path of the file that was executed.</param>
    /// <param name="exitCode">The process' exit code.</param>
    /// <param name="standardOutput">The process' standard output as a string.</param>
    /// <param name="standardError">The process' standard error as a string.</param>
    /// <param name="startTime">The start time of the process.</param>
    /// <param name="exitTime">The exit time of the process.</param>
    public BufferedProcessResult(string executableFilePath,
        int exitCode,
        string standardOutput,
        string standardError,
        DateTime startTime,
        DateTime exitTime) : base(executableFilePath, exitCode, startTime, exitTime)
    {
        StandardOutput = standardOutput;
        StandardError = standardError;
    }

    /// <summary>
    /// The Standard Output from a Process or Command represented as a string.
    /// </summary>
    public string StandardOutput { get; }

    /// <summary>
    /// The Standard Error from a Process or Command represented as a string.
    /// </summary>
    public string StandardError { get; }

        
    /// <summary>
    /// Determines whether this BufferedProcessResult is equal to another BufferedProcessResult object.
    /// </summary>
    /// <remarks>This method intentionally does not consider Start and Exit times of Command Results for equality comparison.</remarks>
    /// <param name="other">The other BufferedProcessResult to compare.</param>
    /// <returns>True if this BufferedProcessResult is equal to the other BufferedProcessResult; false otherwise.</returns>
    public bool Equals(BufferedProcessResult? other)
    {
        if (other is null)
            return false;
            
        return StandardOutput == other.StandardOutput &&
               StandardError == other.StandardError &&
               ExitCode == other.ExitCode;
    }

    /// <summary>
    /// Determines whether this BufferedProcessResult is equal to another object.
    /// </summary>
    /// <param name="obj">The other object to compare.</param>
    /// <returns>True if the other object is a BufferedProcessResult and is equal to this BufferedProcessResult; false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (obj.GetType() != typeof(BufferedProcessResult))
            return false;
            
        return Equals((BufferedProcessResult)obj);
    }

    /// <summary>
    /// Returns the hash code for the current BufferedProcessResult.
    /// </summary>
    /// <returns>The hash code for the current BufferedProcessResult.</returns>
    public override int GetHashCode() 
        => HashCode.Combine(StandardOutput, StandardError, ExitCode);

    /// <summary>
    /// Determines whether two BufferedProcessResults are equal.
    /// </summary>
    /// <param name="left">The first BufferedProcessResult to compare.</param>
    /// <param name="right">The second BufferedProcessResult to compare.</param>
    /// <returns>True if the two BufferedProcessResult objects are equal; false otherwise.</returns>
    public static bool Equals(BufferedProcessResult left, BufferedProcessResult? right) 
        => left.Equals(right);

    /// <summary>
    /// Determines if a BufferedProcessResult is equal to another BufferedProcessResult.
    /// </summary>
    /// <param name="left">A BufferedProcessResult to be compared.</param>
    /// <param name="right">The other BufferedProcessResult to be compared.</param>
    /// <returns>True if both BufferedProcessResults are equal to each other; false otherwise.</returns>
    public static bool operator ==(BufferedProcessResult left, BufferedProcessResult? right) 
        => Equals(left, right);

    /// <summary>
    /// Determines if a BufferedProcessResult is not equal to another BufferedProcessResult.
    /// </summary>
    /// <param name="left">A BufferedProcessResult to be compared.</param>
    /// <param name="right">The other BufferedProcessResult to be compared.</param>
    /// <returns>True if both BufferedProcessResults are not equal to each other; false otherwise.</returns>
    public static bool operator !=(BufferedProcessResult left, BufferedProcessResult? right) 
        => Equals(left, right) == false;
}