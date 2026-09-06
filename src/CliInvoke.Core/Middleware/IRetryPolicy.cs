/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core.Middleware;

/// <summary>
///     Decides whether a failed process result should be retried.
/// </summary>
/// <remarks>
///     Implementations classify results as retryable or terminal. The retry middleware
///     consults this policy after each completed attempt to decide whether to re-invoke
///     the pipeline.
/// </remarks>
public interface IRetryPolicy
{
    /// <summary>
    ///     Determines whether the specified process result warrants a retry.
    /// </summary>
    /// <param name="result">The result of the most recent process attempt.</param>
    /// <param name="completedAttempts">
    ///     The 1-based count of completed attempts at the time this decision is made.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the operation should be retried; otherwise, <c>false</c>.
    /// </returns>
    bool ShouldRetry(ProcessResult result, int completedAttempts);
}
