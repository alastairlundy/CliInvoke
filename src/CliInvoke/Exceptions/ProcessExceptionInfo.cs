/*
    CliInvoke.Core
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace CliInvoke.Exceptions;

/// <summary>
/// Represents detailed information about an exception that occurred during a process execution.
/// </summary>
public class ProcessExceptionInfo : IEquatable<ProcessExceptionInfo>, IDisposable
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="startInfo"></param>
    /// <param name="processId"></param>
    /// <param name="processName"></param>
    /// <param name="processWasNew"></param>
    /// <param name="processResourcePolicy"></param>
    /// <param name="credential"></param>
    public ProcessExceptionInfo(ProcessResult result, ProcessStartInfo startInfo, int processId, string processName, bool processWasNew, 
        ProcessResourcePolicy processResourcePolicy, UserCredential? credential = null)
    {
        Result = result;
        Id = processId;
        ProcessName = processName;
        ProcessWasNew = processWasNew;
        ResourcePolicy = processResourcePolicy;
        Credential = credential;
        StartInfo = startInfo;
        
        ArgumentsConflict = startInfo.UseShellExecute && (startInfo.RedirectStandardOutput ||
                                                          startInfo.RedirectStandardError || 
                                                          startInfo.RedirectStandardInput);
    }

    /// <summary>
    /// Represents the result of a process execution, encapsulating information such as
    /// the exit code, execution duration, and other details about the executed process.
    /// </summary>
    public ProcessResult Result { get; }

    /// <summary>
    /// Provides information about the configuration and parameters used to start the process.
    /// This includes details such as the file name, arguments, and other properties related
    /// to the process initialization.
    /// </summary>
    public ProcessStartInfo StartInfo { get; }

    /// <summary>
    /// Represents the unique identifier of the process associated with the exception.
    /// This value corresponds to the process ID assigned by the operating system
    /// when the process was initiated.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Indicates whether the associated process was newly created during execution.
    /// This property is used to determine if the process is a fresh instance
    /// as opposed to an existing one.
    /// </summary>
    public bool ProcessWasNew { get; }

    /// <summary>
    /// Indicates whether there is a conflict between the configured process start options.
    /// This conflict arises when <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute"/> is set to true
    /// while either <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardOutput"/>,
    /// <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardError"/>, or
    /// <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardInput"/> is enabled.
    /// Such a configuration is invalid and will prevent the process from starting.
    /// </summary>
    public bool ArgumentsConflict { get; }

    /// <summary>
    /// Represents the executable name or identifier used to reference the process,
    /// aiding in diagnostics and error tracking.
    /// </summary>
    public string ProcessName { get; }

    /// <summary>
    /// Defines the policy for managing process resource usage, including constraints on
    /// processor affinity, working set, and priority class.
    /// </summary>
    public ProcessResourcePolicy ResourcePolicy { get; }

    /// <summary>
    /// Gets the user credentials associated with the process.
    /// Represents an optional <see cref="UserCredential"/> instance that provides
    /// information for domain, username, password, and user profile loading.
    /// This is primarily used for scenarios where processes require specific
    /// user authentication.
    /// </summary>
    public UserCredential? Credential { get; }

    /// <summary>
    /// Determines whether the current instance is equal to another instance of the <see cref="ProcessExceptionInfo"/> class.
    /// </summary>
    /// <param name="other">The other <see cref="ProcessExceptionInfo"/> instance to compare with the current instance.</param>
    /// <returns><c>true</c> if the current instance is equal to the specified instance; otherwise, <c>false</c>.</returns>
    public bool Equals(ProcessExceptionInfo? other)
    {
        if (other is null) return false;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        return Result.Equals(other.Result) &&
               Id == other.Id && ProcessWasNew == other.ProcessWasNew &&
               ArgumentsConflict == other.ArgumentsConflict &&
               ProcessName == other.ProcessName &&
               ResourcePolicy.Equals(other.ResourcePolicy) &&
               Credential.Equals(Credential);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }

    /// <summary>
    /// Determines whether the current instance is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with the current <see cref="ProcessExceptionInfo"/> instance.</param>
    /// <returns><c>true</c> if the specified object is equal to the current instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;

        if(obj is  ProcessExceptionInfo other) return Equals(other);

        return false;
    }

    /// <summary>
    /// Returns a hash code for the current instance of the <see cref="ProcessExceptionInfo"/> class.
    /// </summary>
    /// <returns>An integer that represents the hash code of the current instance.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Result, Id, ProcessWasNew, ArgumentsConflict, ProcessName, ResourcePolicy, Credential);
    }

    /// <summary>
    /// Determines whether two instances of the <see cref="ProcessExceptionInfo"/> class are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessExceptionInfo"/> instance to compare.</param>
    /// <param name="right">The second <see cref="ProcessExceptionInfo"/> instance to compare.</param>
    /// <returns><c>true</c> if the two instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(ProcessExceptionInfo? left, ProcessExceptionInfo? right)
    {
        if (left is null || right is null)
            return false;
        
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two instances of the <see cref="ProcessExceptionInfo"/> class are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessExceptionInfo"/> instance to compare.</param>
    /// <param name="right">The second <see cref="ProcessExceptionInfo"/> instance to compare.</param>
    /// <returns><c>true</c> if the two specified instances are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(ProcessExceptionInfo? left, ProcessExceptionInfo? right)
    {
        if (left is null || right is null)
            return false;

        return !left.Equals(right);
    }

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="ProcessExceptionInfo"/> class.
    /// </summary>
    public void Dispose()
    {
        Credential?.Dispose();
    }
}