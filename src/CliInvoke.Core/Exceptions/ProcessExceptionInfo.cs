/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace CliInvoke.Core.Exceptions;

/// <summary>
///     Represents detailed information about an exception that occurred during a process execution.
/// </summary>
public class ProcessExceptionInfo<TProcessResult> : IEquatable<ProcessExceptionInfo<TProcessResult>>, IDisposable
    where TProcessResult : ProcessResult
{
    public ProcessExceptionInfo(TProcessResult result)
    {
        Result = result;
        Id = result.ProcessId;
        ProcessName = result.ExecutedFilePath;
        ResourcePolicy = ProcessResourcePolicy.Default;
        Configuration = null;
        Credential = null;
        ArgumentsConflict = false;
    }
    
    /// <summary>
    /// </summary>
    /// <param name="result"></param>
    /// <param name="configuration"></param>
    public ProcessExceptionInfo(TProcessResult result, ProcessConfiguration configuration)
    {
        Result = result;
        Id = result.ProcessId;
        ProcessName = result.ExecutedFilePath;
        ResourcePolicy = configuration.ResourcePolicy;
        Credential = configuration.Credential;
        Configuration = configuration;

        ArgumentsConflict = configuration.UseShellExecution &&
                            (configuration.OutputRedirection == OutputRedirectionMode.Pipe ||
                             configuration.OutputRedirection == OutputRedirectionMode.Buffer ||
                             configuration.RedirectStandardInput);
    }

    /// <summary>
    ///     Represents the result of a process execution, encapsulating information such as
    ///     the exit code, execution duration, and other details about the executed process.
    /// </summary>
    public TProcessResult Result { get; }

    /// <summary>
    ///     Provides information about the configuration and parameters used to start the process.
    ///     This includes details such as the file name, arguments, and other properties related
    ///     to the process initialization.
    /// </summary>
    public ProcessConfiguration? Configuration { get; }

    /// <summary>
    ///     Represents the unique identifier of the process associated with the exception.
    ///     This value corresponds to the process ID assigned by the operating system
    ///     when the process was initiated.
    /// </summary>
    public int Id { get; }

    /// <summary>
    ///     Indicates whether there is a conflict between the configured process start options.
    ///     This conflict arises when <see cref="System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is
    ///     set to true
    ///     while either <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardOutput" />,
    ///     <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardError" />, or
    ///     <see cref="System.Diagnostics.ProcessStartInfo.RedirectStandardInput" /> is enabled.
    ///     Such a configuration is invalid and will prevent the process from starting.
    /// </summary>
    public bool ArgumentsConflict { get; }

    /// <summary>
    ///     Represents the executable name or identifier used to reference the process,
    ///     aiding in diagnostics and error tracking.
    /// </summary>
    public string ProcessName { get; }

    /// <summary>
    ///     Defines the policy for managing process resource usage, including constraints on
    ///     processor affinity, working set, and priority class.
    /// </summary>
    public ProcessResourcePolicy ResourcePolicy { get; }

    /// <summary>
    ///     Gets the user credentials associated with the process.
    ///     Represents an optional <see cref="UserCredential" /> instance that provides
    ///     information for domain, username, password, and user profile loading.
    ///     This is primarily used for scenarios where processes require specific
    ///     user authentication.
    /// </summary>
    public UserCredential? Credential { get; }

    /// <summary>
    ///     Releases all resources used by the current instance of the <see cref="ProcessExceptionInfo{TProcessResult}" />
    ///     class.
    /// </summary>
    public void Dispose()
    {
        Credential?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Determines whether the current instance is equal to another instance of the
    ///     <see cref="ProcessExceptionInfo{TProcessResult}" /> class.
    /// </summary>
    /// <param name="other">
    ///     The other <see cref="ProcessExceptionInfo{TProcessResult}" /> instance to compare with the
    ///     current instance.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the current instance is equal to the specified instance; otherwise,
    ///     <c>false</c>.
    /// </returns>
    public bool Equals(ProcessExceptionInfo<TProcessResult>? other)
    {
        if (other is null) return false;


        if (Configuration is not null)
        {
            return (Credential is not null) switch
            {
                false => Configuration.Equals(other.Configuration) && Result.Equals(other.Result) &&
                         ArgumentsConflict == other.ArgumentsConflict &&
                         ResourcePolicy.Equals(other.ResourcePolicy) &&
                         Credential.Equals(other.Credential),
                true => Configuration.Equals(other.Configuration) && Result.Equals(other.Result) &&
                        ArgumentsConflict == other.ArgumentsConflict &&
                        ResourcePolicy.Equals(other.ResourcePolicy)
            };
        }
        
        return (Credential is not null) switch
        {
            false => Result.Equals(other.Result) &&
                     ArgumentsConflict == other.ArgumentsConflict &&
                     ResourcePolicy.Equals(other.ResourcePolicy) &&
                     Credential.Equals(other.Credential),
            true => Result.Equals(other.Result) &&
                    ArgumentsConflict == other.ArgumentsConflict &&
                    ResourcePolicy.Equals(other.ResourcePolicy)
        };
    }

    /// <summary>
    ///     Determines whether the current instance is equal to the specified object.
    /// </summary>
    /// <param name="obj">
    ///     The object to compare with the current <see cref="ProcessExceptionInfo{TProcessResult}" />
    ///     instance.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the specified object is equal to the current instance; otherwise,
    ///     <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;

        if (obj is ProcessExceptionInfo<TProcessResult> other) return Equals(other);

        return false;
    }

    /// <summary>
    ///     Returns a hash code for the current instance of the <see cref="ProcessExceptionInfo{TProcessResult}" /> class.
    /// </summary>
    /// <returns>An integer that represents the hash code of the current instance.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Result, Id, ArgumentsConflict, ProcessName, ResourcePolicy,
            Credential);
    }

    /// <summary>
    ///     Determines whether two instances of the <see cref="ProcessExceptionInfo{TProcessResult}" /> class are equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessExceptionInfo{TProcessResult}" /> instance to compare.</param>
    /// <param name="right">The second <see cref="ProcessExceptionInfo{TProcessResult}" /> instance to compare.</param>
    /// <returns><c>true</c> if the two instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(ProcessExceptionInfo<TProcessResult>? left, ProcessExceptionInfo<TProcessResult>? right)
    {
        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    ///     Determines whether two instances of the <see cref="ProcessExceptionInfo{TProcessResult}" /> class are not
    ///     equal.
    /// </summary>
    /// <param name="left">The first <see cref="ProcessExceptionInfo{TProcessResult}" /> instance to compare.</param>
    /// <param name="right">The second <see cref="ProcessExceptionInfo{TProcessResult}" /> instance to compare.</param>
    /// <returns><c>true</c> if the two specified instances are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(ProcessExceptionInfo<TProcessResult>? left, ProcessExceptionInfo<TProcessResult>? right)
    {
        if (left is null || right is null)
            return false;

        return !left.Equals(right);
    }
}