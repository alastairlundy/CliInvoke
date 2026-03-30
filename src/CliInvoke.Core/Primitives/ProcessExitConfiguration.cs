/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core;

/// <summary>
/// Represents configuration information about the exit behaviour of a process, including timeout policy and result validation.
/// </summary>
///
public class ProcessExitConfiguration : IEquatable<ProcessExitConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessExitConfiguration"/> class with default timeout policy and result validation.
    /// </summary>
    public ProcessExitConfiguration()
    {
        TimeoutPolicy = ProcessTimeoutPolicy.Default;
        TimeoutCancellationPolicy =  ProcessCancellationPolicy.Default;
        RequestedCancellationPolicy =  ProcessCancellationPolicy.Default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessExitConfiguration"/> class with the specified timeout policy and result validation.
    /// </summary>
    /// <param name="timeoutPolicy">The timeout policy to apply to the process.</param>
    /// <param name="timeoutCancellationPolicy"></param>
    /// <param name="requestedCancellationPolicy"></param>
    public ProcessExitConfiguration(ProcessTimeoutPolicy timeoutPolicy,
        ProcessCancellationPolicy timeoutCancellationPolicy,
        ProcessCancellationPolicy requestedCancellationPolicy)
    {
        TimeoutPolicy = timeoutPolicy;
        TimeoutCancellationPolicy = timeoutCancellationPolicy;
        RequestedCancellationPolicy = requestedCancellationPolicy;
    }

    /// <summary>
    /// Gets the default <see cref="ProcessExitConfiguration"/> instance, which uses the default timeout policy and exit code zero validation.
    /// </summary>
    public static ProcessExitConfiguration Default { get; } = new();

    /// <summary>
    /// Gets the default <see cref="ProcessExitConfiguration"/> instance, which uses the default timeout policy, but suppresses the Exception from cancellation.
    /// </summary>
    public static ProcessExitConfiguration DefaultNoException { get; }= new(
        ProcessTimeoutPolicy.Default, ProcessCancellationPolicy.DefaultNoException,
        ProcessCancellationPolicy.DefaultNoException);

    /// <summary>
    /// A preconfigured <see cref="ProcessExitConfiguration"/> instance with Exit Code Validation and without a Timeout Policy.
    /// </summary>
    public static ProcessExitConfiguration NoTimeoutDefault { get; } = new(
        ProcessTimeoutPolicy.None,
        timeoutCancellationPolicy: ProcessCancellationPolicy.None,
        requestedCancellationPolicy: ProcessCancellationPolicy.Default
    );

    /// <summary>
    /// A <see cref="ProcessExitConfiguration"/> instance configured to have no timeout policy
    /// and no exceptions on cancellation during a process termination.
    /// </summary>
    public static ProcessExitConfiguration NoTimeoutNoException
        => new(ProcessTimeoutPolicy.None, timeoutCancellationPolicy: ProcessCancellationPolicy.None,
            requestedCancellationPolicy: ProcessCancellationPolicy.DefaultNoException);
    
    /// <summary>
    /// Gets the timeout policy applied to the process.
    /// </summary>
    public ProcessTimeoutPolicy TimeoutPolicy { get; }

    /// <summary>
    /// The configured <see cref="ProcessCancellationPolicy"/> that determines the behaviour
    /// when a process timeout occurs, including how cancellation is handled and if exceptions
    /// should be allowed or suppressed.
    /// </summary>
    public ProcessCancellationPolicy TimeoutCancellationPolicy { get; }

    /// <summary>
    /// The <see cref="ProcessCancellationPolicy"/> instance that defines the behaviour
    /// for handling cancellations explicitly requested during the process lifecycle.
    /// </summary>
    public ProcessCancellationPolicy RequestedCancellationPolicy { get; }

    /// <summary>
    /// Determines whether the specified <see cref="ProcessExitConfiguration"/> is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="ProcessExitConfiguration"/> to compare with the current instance.</param>
    /// <returns><c>true</c> if the specified object is equal to the current instance; otherwise, <c>false</c>.</returns>
    public bool Equals(ProcessExitConfiguration? other)
    {
        if (other is null)
            return false;

        return TimeoutPolicy.Equals(other.TimeoutPolicy);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns><c>true</c> if the specified object is equal to the current instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (obj is ProcessExitConfiguration exitInfo)
            return Equals(exitInfo);

        return false;
    }

    /// <summary>
    /// Returns a hash code for the current instance.
    /// </summary>
    /// <returns>The hash code for the current instance.</returns>
    public override int GetHashCode() => HashCode.Combine(TimeoutPolicy);

    /// <summary>
    /// Determines whether two <see cref="ProcessExitConfiguration"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessExitConfiguration"/> to compare.</param>
    /// <param name="right">The second <see cref="ProcessExitConfiguration"/> to compare.</param>
    /// <returns><c>true</c> if both instances are equal; otherwise, <c>false</c>.</returns>
    public static bool Equals(ProcessExitConfiguration? left,
        ProcessExitConfiguration? right)
    {
        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two <see cref="ProcessExitConfiguration"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessExitConfiguration"/> to compare.</param>
    /// <param name="right">The second <see cref="ProcessExitConfiguration"/> to compare.</param>
    /// <returns><c>true</c> if both instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(
        ProcessExitConfiguration? left,
        ProcessExitConfiguration? right
    ) => Equals(left, right);

    /// <summary>
    /// Determines whether two <see cref="ProcessExitConfiguration"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessExitConfiguration"/> to compare.</param>
    /// <param name="right">The second <see cref="ProcessExitConfiguration"/> to compare.</param>
    /// <returns><c>true</c> if both instances are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(ProcessExitConfiguration? left,
        ProcessExitConfiguration? right) =>
        !Equals(left, right);
}