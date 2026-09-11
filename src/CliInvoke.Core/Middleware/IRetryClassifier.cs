/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core.Middleware;

/// <summary>
///     Classifies whether a failed process result should be retried.
/// </summary>
/// <remarks>
///     Implementations classify results as retryable or terminal. The retry middleware
///     consults this classifier after each completed attempt to decide whether to re-invoke
///     the pipeline.
/// </remarks>
public interface IRetryClassifier
{
    /// <summary>
    ///     Classifies whether the specified process result should be retried.
    /// </summary>
    /// <param name="result">The result of the most recent process attempt.</param>
    /// <returns>
    ///     <c>true</c> if the operation should be retried; otherwise, <c>false</c>.
    /// </returns>
    bool ShouldRetry(ProcessResult result);
}
