/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Extensions.Middleware.Retry;

/// <summary>
///     Provides pre-built <see cref="IRetryClassifier"/> implementations.
/// </summary>
public static class RetryConditions
{
    /// <summary>
    ///     Returns a classifier that retries whenever the exit code is non-zero.
    /// </summary>
    /// <returns>An <see cref="IRetryClassifier"/> that retries on non-zero exit codes.</returns>
    public static IRetryClassifier ExitCodeZero()
        => new ExitCodeZeroClassifier();

    private sealed class ExitCodeZeroClassifier : IRetryClassifier
    {
        public bool ShouldRetry(ProcessResult result)
            => result.ExitCode != 0;
    }
}