/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core.Exceptions;

/// <summary>
///     An exception thrown when a post-exit validation rule fails.
/// </summary>
/// <remarks>
///     The exception retains the <see cref="Result"/> so callers can inspect it. When
///     <see cref="Result"/> is a <c>PipedProcessResult</c>, the exception handler must dispose it
///     to release the underlying output streams.
/// </remarks>
public sealed class ProcessValidationException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProcessValidationException"/> class.
    /// </summary>
    /// <param name="result">The process result that failed validation.</param>
    /// <param name="failureReason">The description of the failed validation rule.</param>
    public ProcessValidationException(ProcessResult result, string failureReason)
        : base(failureReason)
    {
        Result = result;
    }

    /// <summary>
    ///     Gets the process result that failed validation.
    /// </summary>
    public ProcessResult Result { get; }
}
