/*
    CliInvoke.Core
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Internal;

namespace CliInvoke.Core;

/// <summary>
/// Represents configuration information about the exit behavior of a process, including timeout policy and result validation.
/// </summary>
public class ProcessExitConfiguration<TProcessResult> : ProcessExitConfiguration, IEquatable<ProcessExitConfiguration<TProcessResult>>
where TProcessResult : ProcessResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessExitConfiguration"/> class with default timeout policy and result validation.
    /// </summary>
    public ProcessExitConfiguration()
    {
        TimeoutPolicy = ProcessTimeoutPolicy.Default;
        ResultValidation = ProcessResultValidation.ExitCodeZero;
        ValidationRules = [(result => result.ExitCode == 0),
            (result => result.RuntimeDuration > TimeSpan.FromMilliseconds(0))
        ];
        CancellationExceptionBehavior = ProcessCancellationExceptionBehavior.AllowException;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessExitConfiguration"/> class with the specified timeout policy and result validation.
    /// </summary>
    /// <param name="timeoutPolicy">The timeout policy to apply to the process.</param>
    /// <param name="resultValidation">The result validation strategy to use for the process exit.</param>
    /// <param name="cancellationValidation"></param>
    public ProcessExitConfiguration(
        ProcessTimeoutPolicy timeoutPolicy,
        ProcessResultValidation resultValidation,
        Func<TProcessResult, bool>[] validationRules,
        ProcessCancellationExceptionBehavior cancellationValidation
    )
    {
        ArgumentNullException.ThrowIfNull(validationRules);
        
        TimeoutPolicy = timeoutPolicy;
        ResultValidation = resultValidation;
        ValidationRules = validationRules;
        CancellationExceptionBehavior = cancellationValidation;
    }

    /// <summary>
    /// Gets the default <see cref="ProcessExitConfiguration"/> instance, which uses the default timeout policy and exit code zero validation.
    /// </summary>
    public static readonly ProcessExitConfiguration<TProcessResult> Default = new(
        ProcessTimeoutPolicy.Default,
        ProcessResultValidation.ExitCodeZero,
        [(result => result.ExitCode == 0),
            (result => result.RuntimeDuration > TimeSpan.FromMilliseconds(0))
        ],
        ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected
    );

    /// <summary>
    /// Gets the default <see cref="ProcessExitConfiguration"/> instance, which uses the default timeout policy, but suppresses the Exception from cancellation.
    /// </summary>
    public static readonly ProcessExitConfiguration<TProcessResult> DefaultNoException = new(
        ProcessTimeoutPolicy.Default,
        ProcessResultValidation.ExitCodeZero,
        [(result => result.ExitCode == 0),
            (result => result.RuntimeDuration > TimeSpan.FromMilliseconds(0))
        ],
        ProcessCancellationExceptionBehavior.SuppressException
    );

    /// <summary>
    /// A preconfigured <see cref="ProcessExitConfiguration"/> instance with Exit Code Validation and without a Timeout Policy.
    /// </summary>
    public static readonly ProcessExitConfiguration<TProcessResult> NoTimeoutDefault = new(
        ProcessTimeoutPolicy.None,
        ProcessResultValidation.ExitCodeZero,
        [(result => result.ExitCode == 0),
            (result => result.RuntimeDuration > TimeSpan.FromMilliseconds(0))
        ],
        ProcessCancellationExceptionBehavior.SuppressException
    );

    /// <summary>
    /// Represents a <see cref="ProcessExitConfiguration"/> that applies no validation
    /// or constraints, using no timeout, no result validation, and suppression of exceptions.
    /// </summary>
    public static readonly ProcessExitConfiguration<TProcessResult> NoValidation = new(ProcessTimeoutPolicy.None,
        ProcessResultValidation.None,
        [result => true],
        ProcessCancellationExceptionBehavior.SuppressException);

    /// <summary>
    /// Gets the result validation strategy used to determine if the process exited successfully.
    /// </summary>
    [Obsolete(DeprecationMessages.DeprecationV3)]
    public new ProcessResultValidation ResultValidation { get; }
    
    public Func<TProcessResult, bool>[] ValidationRules { get; }

    /// <summary>
    /// Gets the result validation strategy used to determine if Process cancellation should throw an exception.
    /// </summary>
    public new ProcessCancellationExceptionBehavior CancellationExceptionBehavior { get; }

    /// <summary>
    /// Gets the timeout policy applied to the process.
    /// </summary>
    public new ProcessTimeoutPolicy TimeoutPolicy { get; }

    /// <summary>
    /// Determines whether the specified <see cref="ProcessExitConfiguration"/> is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="ProcessExitConfiguration"/> to compare with the current instance.</param>
    /// <returns><c>true</c> if the specified object is equal to the current instance; otherwise, <c>false</c>.</returns>
    public bool Equals(ProcessExitConfiguration<TProcessResult>? other)
    {
        if (other is null)
            return false;

        return ResultValidation == other.ResultValidation
               && ValidationRules == other.ValidationRules
               && TimeoutPolicy.Equals(other.TimeoutPolicy)
               && CancellationExceptionBehavior == other.CancellationExceptionBehavior;
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

        if (obj is ProcessExitConfiguration<TProcessResult> exitInfo)
            return Equals(exitInfo);

        return false;
    }

    /// <summary>
    /// Returns a hash code for the current instance.
    /// </summary>
    /// <returns>The hash code for the current instance.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(ResultValidation, ValidationRules, TimeoutPolicy, CancellationExceptionBehavior);
    }

    /// <summary>
    /// Determines whether two <see cref="ProcessExitConfiguration"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessExitConfiguration"/> to compare.</param>
    /// <param name="right">The second <see cref="ProcessExitConfiguration"/> to compare.</param>
    /// <returns><c>true</c> if both instances are equal; otherwise, <c>false</c>.</returns>
    public static bool Equals(ProcessExitConfiguration<TProcessResult>? left,
        ProcessExitConfiguration<TProcessResult>? right)
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
        ProcessExitConfiguration<TProcessResult>? left,
        ProcessExitConfiguration<TProcessResult>? right
    ) => Equals(left, right);

    /// <summary>
    /// Determines whether two <see cref="ProcessExitConfiguration"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessExitConfiguration"/> to compare.</param>
    /// <param name="right">The second <see cref="ProcessExitConfiguration"/> to compare.</param>
    /// <returns><c>true</c> if both instances are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(ProcessExitConfiguration<TProcessResult>? left,
        ProcessExitConfiguration<TProcessResult>? right)
    {
        return !Equals(left, right);
    }
}
