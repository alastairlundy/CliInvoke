/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Threading.Tasks;

using CliInvoke.Core.Middleware;
using CliInvoke.Core.Validation;

namespace CliInvoke.Extensions.Middleware.Retry;

/// <summary>
///     Middleware that re-invokes the pipeline while the result is retryable and attempts remain,
///     applying the configured backoff between attempts.
/// </summary>
/// <remarks>
///     Retries by default for classified (retryable) failures; callers should avoid this middleware for
///     non-idempotent invocations (see DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D005).
/// </remarks>
internal sealed class RetryMiddleware : IProcessMiddleware
{
    private readonly IProcessResultValidator<ProcessResult> _retryableConditions;
    private readonly RetryOptions _options;

    /// <summary>
    ///     Initialises a new instance of the <see cref="RetryMiddleware"/> class.
    /// </summary>
    /// <param name="retryableConditions">The validator whose <c>ShouldRetry</c> decides whether to retry.</param>
    /// <param name="options">The retry options (attempts, base delay, strategy).</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="retryableConditions"/> or <paramref name="options"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="options"/>.<see cref="RetryOptions.MaxAttempts"/> is less than 1, since
    ///     zero or negative attempts would be meaningless and the <c>do</c> loop would still run once, or when
    ///     <paramref name="options"/>.<see cref="RetryOptions.BaseDelay"/> is negative (which would make the
    ///     first <see cref="Task.Delay"/> throw).
    /// </exception>
    public RetryMiddleware(IProcessResultValidator<ProcessResult> retryableConditions, RetryOptions options)
    {
        ArgumentNullException.ThrowIfNull(retryableConditions);
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaxAttempts < 1)
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "RetryOptions.MaxAttempts must be at least 1; zero or negative attempts are not allowed.");

        if (options.BaseDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "RetryOptions.BaseDelay must not be negative; a negative delay would cause Task.Delay to throw on the first retry.");

        _retryableConditions = retryableConditions;
        _options = options;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(InvocationContext context, Func<InvocationContext, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        int attempts = 0;

        do
        {
            await next(context);

            attempts++;

            if (context.Result is null)
                return;

            if (!_retryableConditions.ShouldRetry(context.Result))
                return;

            if (attempts >= _options.MaxAttempts)
                return;

            TimeSpan delay = ComputeDelay(attempts, _options);

            await Task.Delay(delay, context.CancellationToken).ConfigureAwait(false);
        }
        while (attempts < _options.MaxAttempts);
    }

    private static TimeSpan ComputeDelay(int completedAttempts, RetryOptions options)
    {
        return options.Strategy switch
        {
            RetryBackoffStrategy.Fixed => options.BaseDelay,
            RetryBackoffStrategy.Exponential => TimeSpan.FromTicks(
                options.BaseDelay.Ticks * (long)Math.Pow(2, completedAttempts - 1)),
            _ => options.BaseDelay
        };
    }
}
