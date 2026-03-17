/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Runtime.CompilerServices;

using CliInvoke.Core.Validation;

namespace CliInvoke.Exceptions;

/// <summary>
/// An exception thrown if a Process is run unsuccessfully.
/// </summary>
public sealed class ProcessNotSuccessfulException : Exception
{
    /// <summary>
    /// The command that was executed.
    /// </summary>
    public ProcessExceptionInfo? ExecutedProcess { get; }

    /// <summary>
    /// The exit code of the Command that was executed.
    /// </summary>
    [Obsolete("This property is deprecated and will be removed in a future version.")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public int ExitCode { get; private set; }
    
    /// <summary>
    /// Thrown when an executed Process exited with a non-zero exit code.
    /// </summary>
    /// <param name="exitCode">The exit code of the Process that was executed.</param>
    [Obsolete("This constructor overload is deprecated and will be removed in a future version.")]
    [OverloadResolutionPriority(2)]
    public ProcessNotSuccessfulException(int exitCode)
        : base(
            Resources.Exceptions_ProcessNotSuccessful_Generic.Replace("{x}", exitCode.ToString()))
    {
        ExitCode = exitCode;

        ExecutedProcess = null;
    }

    /// <summary>
    /// Thrown when an executed Process exited with a non-zero exit code.
    /// </summary>
    /// <param name="process">The Process that was executed.</param>
    public ProcessNotSuccessfulException(ProcessExceptionInfo process)
        : base(
            Resources.Exceptions_ProcessNotSuccessful_Specific.Replace(
                    "{x}",
                    process.Result.ExecutedFilePath)
                .Replace("{y}", process.Result.ExitCode.ToString())
        )
    {
        ExecutedProcess = process;

        Source = ExecutedProcess.Configuration.TargetFilePath;
#pragma warning disable CS0618 // Type or member is obsolete
        ExitCode = process.Result.ExitCode;
#pragma warning restore CS0618 // Type or member is obsolete
    }
    
    /// <summary>
    /// Thrown when an executed Process exited with a non-zero exit code.
    /// </summary>
    /// <param name="exitCode">The exit code of the Process that was executed.</param>
    /// <param name="process">The Process that was executed.</param>
    [OverloadResolutionPriority(3)]
    [Obsolete("This constructor overload is deprecated and will be removed in a future version.")]
    public ProcessNotSuccessfulException(int exitCode, ProcessExceptionInfo process)
        : base(
            Resources.Exceptions_ProcessNotSuccessful_Specific.Replace(
                "{y}",
                exitCode.ToString().Replace("{x}", process.Configuration.TargetFilePath)
            )
        )
    {
        ExecutedProcess = process;

        Source = ExecutedProcess.Configuration.TargetFilePath;
        ExitCode = exitCode;
    }

    /// <summary>
    /// Throws an exception if a process execution is unsuccessful.
    /// </summary>
    /// <param name="resultValidator">The validator used to validate the executed process result.</param>
    /// <param name="result">The result of the executed process.</param>
    /// <param name="configuration">The configuration for executing the process.</param>
    /// <exception cref="ProcessNotSuccessfulException">Thrown when the process execution is unsuccessful.</exception>
    public static void ThrowIfNotSuccessful<TProcessResult>(
        IProcessResultValidator<TProcessResult> resultValidator, TProcessResult result,
        ProcessConfiguration configuration)
        where TProcessResult : ProcessResult
    {
        if (!resultValidator.Validate(result))
            throw new ProcessNotSuccessfulException(
                new ProcessExceptionInfo(result, configuration));
    }
}