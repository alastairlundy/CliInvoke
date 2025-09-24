/*
    AlastairLundy.CliInvoke.Core 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;

namespace AlastairLundy.CliInvoke.Core.Primitives;

/// <summary>
/// Represents configuration information about the exit behavior of a process, including timeout policy and result validation.
/// </summary>
public class ProcessExitConfiguration : IEquatable<ProcessExitConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessExitConfiguration"/> class with default timeout policy and result validation.
    /// </summary>
    public ProcessExitConfiguration()
    {
        TimeoutPolicy = ProcessTimeoutPolicy.Default;
        ResultValidation = ProcessResultValidation.ExitCodeZero;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessExitConfiguration"/> class with the specified timeout policy and result validation.
    /// </summary>
    /// <param name="timeoutPolicy">The timeout policy to apply to the process.</param>
    /// <param name="resultValidation">The result validation strategy to use for the process exit.</param>
    public ProcessExitConfiguration(ProcessTimeoutPolicy timeoutPolicy, ProcessResultValidation resultValidation)
    {
        TimeoutPolicy = timeoutPolicy;
        ResultValidation = resultValidation;
    }

    /// <summary>
    /// Gets the default <see cref="ProcessExitConfiguration"/> instance, which uses the default timeout policy and exit code zero validation.
    /// </summary>
    public static readonly ProcessExitConfiguration Default =
        new ProcessExitConfiguration(ProcessTimeoutPolicy.Default,
            ProcessResultValidation.ExitCodeZero);
    
    /// <summary>
    /// A preconfigured <see cref="ProcessExitConfiguration"/> instance with Exit Code Validation and without a Timeout Policy.
    /// </summary>
    public static readonly ProcessExitConfiguration NoTimeoutDefault =
        new ProcessExitConfiguration(ProcessTimeoutPolicy.None, ProcessResultValidation.ExitCodeZero);
    
    /// <summary>
    /// Gets the result validation strategy used to determine if the process exited successfully.
    /// </summary>
    public ProcessResultValidation ResultValidation { get; }
    
    /// <summary>
    /// Gets the timeout policy applied to the process.
    /// </summary>
    public ProcessTimeoutPolicy TimeoutPolicy { get; }


    /// <summary>
    /// Determines whether the specified <see cref="ProcessExitConfiguration"/> is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="ProcessExitConfiguration"/> to compare with the current instance.</param>
    /// <returns><c>true</c> if the specified object is equal to the current instance; otherwise, <c>false</c>.</returns>
    public bool Equals(ProcessExitConfiguration? other)
    {
        if (other is null) return false;

        return ResultValidation == other.ResultValidation &&
               TimeoutPolicy.Equals(other.TimeoutPolicy);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns><c>true</c> if the specified object is equal to the current instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;

        if (obj is ProcessExitConfiguration exitInfo)
            return Equals(exitInfo);

        return false;
    }

    /// <summary>
    /// Returns a hash code for the current instance.
    /// </summary>
    /// <returns>The hash code for the current instance.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine((int)ResultValidation, TimeoutPolicy);
    }

    /// <summary>
    /// Determines whether two <see cref="ProcessExitConfiguration"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessExitConfiguration"/> to compare.</param>
    /// <param name="right">The second <see cref="ProcessExitConfiguration"/> to compare.</param>
    /// <returns><c>true</c> if both instances are equal; otherwise, <c>false</c>.</returns>
    public static bool Equals(ProcessExitConfiguration? left, ProcessExitConfiguration? right)
    {
        if(left is null || right is null)
            return false;

        return left.Equals(right);
    }


    /// <summary>
    /// Determines whether two <see cref="ProcessExitConfiguration"/> instances are equal using the == operator.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessExitConfiguration"/> to compare.</param>
    /// <param name="right">The second <see cref="ProcessExitConfiguration"/> to compare.</param>
    /// <returns><c>true</c> if both instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(ProcessExitConfiguration? left, ProcessExitConfiguration? right)
        => Equals(left, right);

    /// <summary>
    /// Determines whether two <see cref="ProcessExitConfiguration"/> instances are not equal using the != operator.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessExitConfiguration"/> to compare.</param>
    /// <param name="right">The second <see cref="ProcessExitConfiguration"/> to compare.</param>
    /// <returns><c>true</c> if both instances are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(ProcessExitConfiguration? left, ProcessExitConfiguration? right)
    {
        return Equals(left, right) == false;
    }
}