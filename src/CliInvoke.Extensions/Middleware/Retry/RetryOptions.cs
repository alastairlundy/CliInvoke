/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;

namespace CliInvoke.Extensions.Middleware.Retry;

/// <summary>
///     Configuration options for retry middleware applied to process invocation.
/// </summary>
/// <remarks>
///     Register an instance of this type in the dependency injection container to customise retry
///     behaviour. When no instance is registered, <see cref="Default"/> is used.
/// </remarks>
public sealed class RetryOptions
{
    /// <summary>
    ///     Gets the default options instance (MaxAttempts = 3, BaseDelay = 100 ms, Strategy = Exponential).
    /// </summary>
    public static RetryOptions Default { get; } = new();

    /// <summary>
    ///     Gets or sets the maximum number of invocation attempts, including the initial attempt.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    ///     Gets or sets the base delay applied between retry attempts.
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    ///     Gets or sets the backoff strategy used between retry attempts.
    /// </summary>
    public RetryBackoffStrategy Strategy { get; set; } = RetryBackoffStrategy.Exponential;
}
