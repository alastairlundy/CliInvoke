/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Extensions.Middleware.Retry;

/// <summary>
///     Selects the backoff strategy used between retry attempts.
/// </summary>
public enum RetryBackoffStrategy
{
    /// <summary>
    ///     Waits a constant <see cref="RetryOptions.BaseDelay"/> between attempts.
    /// </summary>
    Fixed,

    /// <summary>
    ///     Waits an exponentially increasing delay (base × 2^(attempt-1)) between attempts.
    /// </summary>
    Exponential,
}
