/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core;

/// <summary>
///     Represents configuration information about the exit behaviour of a process, including timeout
///     policy and result validation.
/// </summary>
public class ProcessExitConfiguration : IEquatable<ProcessExitConfiguration>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProcessExitConfiguration" /> class with default
    ///     timeout policy and result validation.
    /// </summary>
    public ProcessExitConfiguration()
    {
        TimeoutPolicy = ProcessTimeoutPolicy.Default;
        RequestedCancellationExitBehaviour = ProcessExitBehaviour.GracefulExit;
        CancellationThrowsException = false;
        ExceptionBehaviour = ProcessExceptionBehaviour.AllowExceptionsIfUnexpected;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProcessExitConfiguration" /> class with the
    ///     specified timeout policy and result validation.
    /// </summary>
    /// <param name="timeoutPolicy">The timeout policy to apply to the process.</param>
    /// <param name="requestedCancellationExitBehaviour"></param>
    /// <param name="exceptionBehaviour"></param>
    /// <param name="cancellationThrowsException"></param>
    public ProcessExitConfiguration(ProcessTimeoutPolicy timeoutPolicy, 
        ProcessExitBehaviour requestedCancellationExitBehaviour = ProcessExitBehaviour.GracefulExit,
        ProcessExceptionBehaviour exceptionBehaviour = ProcessExceptionBehaviour.AllowExceptionsIfUnexpected,
        bool cancellationThrowsException = false)
    {
        TimeoutPolicy = timeoutPolicy;
        RequestedCancellationExitBehaviour = requestedCancellationExitBehaviour;
        CancellationThrowsException = cancellationThrowsException;
        ExceptionBehaviour = exceptionBehaviour;
    }

    /// <summary>
    ///     Gets the <see cref="ProcessExitConfiguration" /> instance with graceful cancellation and exit,
    ///     and the default timeout policy.
    /// </summary>
    public static ProcessExitConfiguration Graceful { get; } = new(ProcessTimeoutPolicy.Default,
        cancellationThrowsException: true);

    /// <summary>
    ///     Gets the default <see cref="ProcessExitConfiguration" /> instance, which uses the default
    ///     timeout policy, but suppresses the Exception from cancellation.
    /// </summary>
    public static ProcessExitConfiguration GracefulNoException { get; } = new(
        ProcessTimeoutPolicy.Default, ProcessExitBehaviour.GracefulExit, 
        ProcessExceptionBehaviour.SuppressExceptions);

    /// <summary>
    /// 
    /// </summary>
    public static ProcessExitConfiguration Forceful { get; } = new(
        ProcessTimeoutPolicy.Default, ProcessExitBehaviour.ForcefulExit, 
        ProcessExceptionBehaviour.AllowExceptions, true);
    
    /// <summary>
    /// 
    /// </summary>
    public static ProcessExitConfiguration ForcefulNoException { get; } = new(ProcessTimeoutPolicy.Default,
        ProcessExitBehaviour.ForcefulExit, ProcessExceptionBehaviour.SuppressExceptions);

    /// <summary>
    /// Gets the <see cref="ProcessExitConfiguration" /> instance configured to wait indefinitely
    /// for the process to exit without applying any timeout restrictions and with cancellation
    /// throwing exceptions if cancellation is requested.
    /// </summary>
    public static ProcessExitConfiguration WaitForExit { get;  } = new(ProcessTimeoutPolicy.None,
        ProcessExitBehaviour.WaitForExit, cancellationThrowsException: true);
    
    /// <summary>
    ///     A <see cref="ProcessExitConfiguration" /> instance configured to have no timeout policy
    /// and no exceptions on cancellation during a process termination.
    /// </summary>
    public static ProcessExitConfiguration WaitForExitNoException
        => new(ProcessTimeoutPolicy.None, ProcessExitBehaviour.WaitForExit, 
            ProcessExceptionBehaviour.SuppressExceptions);

    /// <summary>
    ///     Gets the timeout policy applied to the process.
    /// </summary>
    public ProcessTimeoutPolicy TimeoutPolicy { get; }

    /// <summary>
    /// Gets the <see cref="ProcessExitBehaviour" /> value that determines the behaviour
    /// when a cancellation request is issued for the process.
    /// </summary>
    /// <remarks>
    /// Options include waiting for the process to exit, attempting a graceful exit, or forcing
    /// termination. The default behaviour is <see cref="ProcessExitBehaviour.GracefulExit" />.
    /// </remarks>
    public ProcessExitBehaviour RequestedCancellationExitBehaviour { get; }

    /// <summary>
    /// Gets the <see cref="ProcessExceptionBehaviour" /> value that determines how exceptions
    /// are handled during process execution.
    /// </summary>
    /// <remarks>This property controls whether exceptions are suppressed,
    /// always allowed, or allowed only if they are unexpected.</remarks>
    public ProcessExceptionBehaviour ExceptionBehaviour { get; }

    /// <summary>
    /// Gets a value indicating whether a cancellation request during process execution
    /// will throw an exception.
    /// </summary>
    public bool CancellationThrowsException { get; }
    
    /// <summary>
    ///     Determines whether the specified <see cref="ProcessExitConfiguration" /> is equal to the
    ///     current instance.
    /// </summary>
    /// <param name="other">
    ///     The <see cref="ProcessExitConfiguration" /> to compare with the current
    ///     instance.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the specified object is equal to the current instance; otherwise,
    ///     <c>false</c>.
    /// </returns>
    public bool Equals(ProcessExitConfiguration? other)
    {
        if (other is null)
            return false;

        return TimeoutPolicy.Equals(other.TimeoutPolicy) &&
               CancellationThrowsException == other.CancellationThrowsException &&
               ExceptionBehaviour == other.ExceptionBehaviour &&
               RequestedCancellationExitBehaviour ==  other.RequestedCancellationExitBehaviour;
    }

    /// <summary>
    ///     Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>
    ///     <c>true</c> if the specified object is equal to the current instance; otherwise,
    ///     <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (obj is ProcessExitConfiguration exitInfo)
            return Equals(exitInfo);

        return false;
    }

    /// <summary>
    ///     Returns a hash code for the current instance.
    /// </summary>
    /// <returns>The hash code for the current instance.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(TimeoutPolicy, CancellationThrowsException,
            RequestedCancellationExitBehaviour, RequestedCancellationExitBehaviour,
            ExceptionBehaviour);
    }

    /// <summary>
    ///     Determines whether two <see cref="ProcessExitConfiguration" /> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessExitConfiguration" /> to compare.</param>
    /// <param name="right">The second <see cref="ProcessExitConfiguration" /> to compare.</param>
    /// <returns><c>true</c> if both instances are equal; otherwise, <c>false</c>.</returns>
    public static bool Equals(ProcessExitConfiguration? left,
        ProcessExitConfiguration? right)
    {
        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    ///     Determines whether two <see cref="ProcessExitConfiguration" /> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessExitConfiguration" /> to compare.</param>
    /// <param name="right">The second <see cref="ProcessExitConfiguration" /> to compare.</param>
    /// <returns><c>true</c> if both instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(ProcessExitConfiguration? left,
        ProcessExitConfiguration? right) => Equals(left, right);

    /// <summary>
    ///     Determines whether two <see cref="ProcessExitConfiguration" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessExitConfiguration" /> to compare.</param>
    /// <param name="right">The second <see cref="ProcessExitConfiguration" /> to compare.</param>
    /// <returns><c>true</c> if both instances are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(ProcessExitConfiguration? left,
        ProcessExitConfiguration? right) => !Equals(left, right);
}