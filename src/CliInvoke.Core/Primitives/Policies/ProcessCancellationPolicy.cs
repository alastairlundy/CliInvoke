/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core;

/// <summary>
/// 
/// </summary>
public class ProcessCancellationPolicy : IEquatable<ProcessCancellationPolicy>
{
    public ProcessCancellationPolicy()
    {
        CancellationMode = ProcessCancellationMode.Graceful;
        CancellationExceptionBehaviour = ProcessExceptionBehaviour.AllowExceptionsIfUnexpected;
    }

    public ProcessCancellationPolicy(ProcessCancellationMode cancellationMode,
        ProcessExceptionBehaviour exceptionBehaviour =
            ProcessExceptionBehaviour.AllowExceptionsIfUnexpected)
    {
        CancellationMode = cancellationMode;
        CancellationExceptionBehaviour = exceptionBehaviour;
    }

    /// <summary>
    ///     The mode to use for cancelling the Process if cancellation is requested.
    /// </summary>
    public ProcessCancellationMode CancellationMode { get; }

    /// <summary>
    /// </summary>
    public ProcessExceptionBehaviour CancellationExceptionBehaviour { get; }

    /// <summary>
    ///     Represents the default process cancellation policy, which uses a graceful cancellation mode.
    ///     This value serves as a convenient baseline configuration for process handling.
    /// </summary>
    public static ProcessCancellationPolicy Default =>
        new(ProcessCancellationMode.Graceful);

    /// <summary>
    ///     Provides a default process cancellation policy that employs a graceful cancellation mode
    ///     while suppressing cancellation-related exceptions that may occur during the cancellation
    ///     process.
    /// </summary>
    public static ProcessCancellationPolicy DefaultNoException =>
        new(ProcessCancellationMode.Graceful, ProcessExceptionBehaviour.SuppressExceptions);

    /// <summary>
    /// Represents a cancellation policy that uses a forceful mode of process termination.
    /// </summary>
    public static ProcessCancellationPolicy Forceful =>
        new(ProcessCancellationMode.Forceful);

    /// <summary>
    /// A predefined cancellation policy that represents no cancellation mode and suppresses exceptions.
    /// </summary>
    public static ProcessCancellationPolicy None =>
        new(ProcessCancellationMode.None, ProcessExceptionBehaviour.SuppressExceptions);

    public bool Equals(ProcessCancellationPolicy? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return CancellationMode == other.CancellationMode &&
               CancellationExceptionBehaviour == other.CancellationExceptionBehaviour;
    }

    /// <summary>
    /// Creates a new instance of <see cref="ProcessCancellationPolicy"/> based on the specified cancellation mode.
    /// </summary>
    /// <param name="mode">The <see cref="ProcessCancellationMode"/> to define the cancellation policy.</param>
    /// <returns>A <see cref="ProcessCancellationPolicy"/> instance configured with the specified cancellation mode.</returns>
    public static ProcessCancellationPolicy FromCancellationMode(ProcessCancellationMode mode)
    {
        return new ProcessCancellationPolicy(mode);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if(obj is null) return false;
        
        return obj is ProcessCancellationPolicy other && Equals(other);
    }
    
    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine((int)CancellationMode, (int)CancellationExceptionBehaviour);
    }

    /// <summary>
    /// Determines whether two <see cref="ProcessCancellationPolicy"/> instances are equal.
    /// </summary>
    /// <param name="left">The first instance of <see cref="ProcessCancellationPolicy"/> to compare.</param>
    /// <param name="right">The second instance of <see cref="ProcessCancellationPolicy"/> to compare.</param>
    /// <returns><c>true</c> if both instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(ProcessCancellationPolicy? left,
        ProcessCancellationPolicy? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines whether two specified <see cref="ProcessCancellationPolicy"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessCancellationPolicy"/> instance to compare.</param>
    /// <param name="right">The second <see cref="ProcessCancellationPolicy"/> instance to compare.</param>
    /// <returns>
    /// <c>true</c> if the two <see cref="ProcessCancellationPolicy"/> instances are not equal; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator !=(ProcessCancellationPolicy? left,
        ProcessCancellationPolicy? right) =>
        !Equals(left, right);

    /// <summary>
    /// Determines whether two <see cref="ProcessCancellationPolicy"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessCancellationPolicy"/> instance to compare.</param>
    /// <param name="right">The second <see cref="ProcessCancellationPolicy"/> instance to compare.</param>
    /// <returns><c>true</c> if both instances are equal; otherwise, <c>false</c>.</returns>
    public static bool Equals(ProcessCancellationPolicy? left, ProcessCancellationPolicy? right)
    {
        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }
}