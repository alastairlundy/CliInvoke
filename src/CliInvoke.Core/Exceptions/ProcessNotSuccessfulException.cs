/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Runtime.CompilerServices;

using CliInvoke.Core.Validation;

namespace CliInvoke.Core.Exceptions;

/// <summary>
///     An exception thrown if a Process is run unsuccessfully.
/// </summary>
public sealed class ProcessNotSuccessfulException<TProcessResult> : Exception
where TProcessResult : ProcessResult
{
    /// <summary>
    ///     Thrown when an executed Process exited with a non-zero exit code.
    /// </summary>
    /// <param name="processInfo">The Process that was executed.</param>
    public ProcessNotSuccessfulException(ProcessExceptionInfo<TProcessResult> processInfo)
        : base(
            Resources.Exceptions_ProcessNotSuccessful_Specific.Replace(
                    "{x}",
                    processInfo.Result.ExecutedFilePath)
                .Replace("{y}", processInfo.Result.ExitCode.ToString())
        )
    {
        ExecutedProcessInfo = processInfo;

        Source = processInfo.Result.ExecutedFilePath;
    }

    /// <summary>
    ///     The command that was executed.
    /// </summary>
    public ProcessExceptionInfo<TProcessResult>? ExecutedProcessInfo { get; }
    
    /// <summary>
    ///     Throws an exception if a process execution is unsuccessful.
    /// </summary>
    /// <param name="resultValidator">The validator used to validate the executed process result.</param>
    /// <param name="result">The result of the executed process.</param>
    /// <param name="configuration">The configuration for executing the process.</param>
    /// <exception cref="ProcessNotSuccessfulException{TProcessResult}">Thrown when the process execution is unsuccessful.</exception>
    public static void ThrowIfNotSuccessful(
        IProcessResultValidator<TProcessResult> resultValidator, TProcessResult result,
        ProcessConfiguration configuration)
    {
        if (!resultValidator.Validate(result))
            throw new ProcessNotSuccessfulException<TProcessResult>(
                new ProcessExceptionInfo<TProcessResult>(result, configuration));
    }
}