/*
    AlastairLundy.DotPrimitives
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using AlastairLundy.CliInvoke.Core.Internal.Localizations;

namespace AlastairLundy.CliInvoke.Core;

/// <summary>
/// A class that defines a Process' Timeout configuration, if any.
/// </summary>
public class ProcessTimeoutPolicy : IEquatable<ProcessTimeoutPolicy>
{
    /// <summary>
    /// Instantiates the <see cref="ProcessTimeoutPolicy"/> with default values.
    /// </summary>
    public ProcessTimeoutPolicy()
    {
        TimeoutThreshold = TimeSpan.FromMinutes(30);
        CancellationMode = ProcessCancellationMode.Graceful;
    }

    /// <summary>
    /// Instantiates the <see cref="ProcessTimeoutPolicy"/> with default values unless specified parameters are provided.
    /// </summary>
    /// <param name="timeoutThreshold">The timespan to wait for the Process timeout before cancelling the Process.</param>
    /// <param name="cancellationMode">Defaults to Graceful cancellation, otherwise uses the Cancellation Mode specified.</param>
    public ProcessTimeoutPolicy(
        TimeSpan timeoutThreshold,
        ProcessCancellationMode cancellationMode = ProcessCancellationMode.Graceful
    )
    {
#if NET8_0_OR_GREATER
        bool lessThanZero = double.IsNegative(timeoutThreshold.TotalMilliseconds);
#else
        bool lessThanZero = timeoutThreshold.TotalMilliseconds < double.Parse("0.0");
#endif

        if (timeoutThreshold < TimeSpan.Zero || lessThanZero)
            throw new ArgumentOutOfRangeException(
                nameof(timeoutThreshold),
                Resources.Exceptions_ProcessTimeoutPolicy_Timeout_LessThanZero.Replace(
                    "{x}",
                    timeoutThreshold.ToString()
                )
            );

        TimeoutThreshold = timeoutThreshold;
        CancellationMode = cancellationMode;
    }

    /// <summary>
    /// Instantiates a default ProcessTimeoutPolicy which times out after 30 minutes.
    /// </summary>
    public static ProcessTimeoutPolicy Default { get; } =
        new ProcessTimeoutPolicy(TimeSpan.FromMinutes(30));

    /// <summary>
    /// Disables waiting for Process Timeout.
    /// </summary>
    public static ProcessTimeoutPolicy None { get; } =
        new ProcessTimeoutPolicy(TimeSpan.FromSeconds(0), ProcessCancellationMode.None);

    /// <summary>
    /// The timespan after which a Process should no longer be allowed to continue waiting to exit.
    /// </summary>
    public TimeSpan TimeoutThreshold { get; }

    /// <summary>
    /// The mode to use for cancelling the Process if the timeout threshold is reached.
    /// </summary>
    public ProcessCancellationMode CancellationMode { get; }

    /// <summary>
    /// Determines whether the current <see cref="ProcessTimeoutPolicy"/> is equal to another <see cref="ProcessTimeoutPolicy"/> instance.
    /// </summary>
    /// <param name="other">The <see cref="ProcessTimeoutPolicy"/> instance to compare with the current <see cref="ProcessTimeoutPolicy"/> instance.</param>
    /// <returns><c>true</c> if the current <see cref="ProcessTimeoutPolicy"/> is equal to the specified <see cref="ProcessTimeoutPolicy"/> instance; otherwise, <c>false</c>.</returns>
    public bool Equals(ProcessTimeoutPolicy? other)
    {
        if (other is null)
            return false;

        return TimeoutThreshold.Equals(other.TimeoutThreshold)
            && CancellationMode == other.CancellationMode;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="ProcessTimeoutPolicy"/> instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current <see cref="ProcessTimeoutPolicy"/> instance.</param>
    /// <returns><c>true</c> if the specified object is equal to the current <see cref="ProcessTimeoutPolicy"/> instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (obj is ProcessTimeoutPolicy policy)
            return Equals(policy);

        return false;
    }

    /// <summary>
    /// Returns a hash code for the current <see cref="ProcessTimeoutPolicy"/> instance.
    /// </summary>
    /// <returns>A hash code that represents the current <see cref="ProcessTimeoutPolicy"/>.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(TimeoutThreshold, (int)CancellationMode);
    }

    /// <summary>
    /// Determines whether two <see cref="ProcessTimeoutPolicy"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessTimeoutPolicy"/> instance to compare.</param>
    /// <param name="right">The second <see cref="ProcessTimeoutPolicy"/> instance to compare.</param>
    /// <returns><c>true</c> if the two <see cref="ProcessTimeoutPolicy"/> instances are equal; otherwise, <c>false</c>.</returns>
    public static bool Equals(ProcessTimeoutPolicy? left, ProcessTimeoutPolicy? right)
    {
        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two <see cref="ProcessTimeoutPolicy"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessTimeoutPolicy"/> instance to compare.</param>
    /// <param name="right">The second <see cref="ProcessTimeoutPolicy"/> instance to compare.</param>
    /// <returns><c>true</c> if the two <see cref="ProcessTimeoutPolicy"/> instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(ProcessTimeoutPolicy? left, ProcessTimeoutPolicy? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(ProcessTimeoutPolicy? left, ProcessTimeoutPolicy? right)
    {
        return Equals(left, right) == false;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator >(ProcessTimeoutPolicy? left, ProcessTimeoutPolicy? right)
    {
        if (left is null || right is null)
            return false;

        return left.TimeoutThreshold > right.TimeoutThreshold;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator <(ProcessTimeoutPolicy? left, ProcessTimeoutPolicy? right)
    {
        if (left is null || right is null)
            return false;

        if (
            left.CancellationMode == ProcessCancellationMode.None
            && right.CancellationMode != ProcessCancellationMode.None
        )
            return false;

        return left.TimeoutThreshold < right.TimeoutThreshold;
    }

    /// <summary>
    /// Defines a less-than-or-equal-to comparison between two <see cref="ProcessTimeoutPolicy"/> instances.
    /// </summary>
    /// <param name="left">The left-hand operand in the comparison.</param>
    /// <param name="right">The right-hand operand in the comparison.</param>
    /// <returns>
    /// <see langword="true"/> if the left-hand operand is less than or equal to the right-hand operand;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator >=(ProcessTimeoutPolicy? left, ProcessTimeoutPolicy? right)
    {
        if (left is null || right is null)
            return false;

        if (
            left.CancellationMode == ProcessCancellationMode.None
            && right.CancellationMode != ProcessCancellationMode.None
        )
            return false;

        if (
            right.CancellationMode == ProcessCancellationMode.None
            && left.CancellationMode != ProcessCancellationMode.None
        )
            return true;

        return left.TimeoutThreshold >= right.TimeoutThreshold;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator <=(ProcessTimeoutPolicy? left, ProcessTimeoutPolicy? right)
    {
        if (left is null || right is null)
            return false;

        if (
            left.CancellationMode == ProcessCancellationMode.None
            && right.CancellationMode != ProcessCancellationMode.None
        )
            return true;

        if (
            right.CancellationMode == ProcessCancellationMode.None
            && left.CancellationMode != ProcessCancellationMode.None
        )
            return false;

        return left.TimeoutThreshold <= right.TimeoutThreshold;
    }
}
