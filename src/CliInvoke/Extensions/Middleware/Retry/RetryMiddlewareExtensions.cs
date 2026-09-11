/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Middleware;

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

/// <summary>
///     Provides extension methods for configuring retry middleware on the process pipeline.
/// </summary>
public static class RetryMiddlewareExtensions
{
    /// <param name="builder">The middleware builder.</param>
    extension(IProcessMiddlewareBuilder builder)
    {
        /// <summary>
        ///     Adds retry middleware to the process pipeline using the default options and the
        ///     default retry classifier (exit-code-zero, resolved from the dependency injection container).
        /// </summary>
        /// <returns>The builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <c>null</c>.</exception>
        public IProcessMiddlewareBuilder UseRetryPolicy()
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.UseMiddleware<RetryMiddleware>();
        }

        /// <summary>
        ///     Adds retry middleware using a custom retry classifier and the default options.
        /// </summary>
        /// <param name="retryClassifier">The classifier that decides whether a result is retryable.</param>
        /// <returns>The builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="builder"/> or <paramref name="retryClassifier"/> is <c>null</c>.
        /// </exception>
        public IProcessMiddlewareBuilder UseRetryPolicy(IRetryClassifier retryClassifier)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(retryClassifier);

            return builder.UseMiddleware(new RetryMiddleware(retryClassifier, RetryOptions.Default));
        }

        /// <summary>
        ///     Adds retry middleware using the default retry policy and custom options.
        /// </summary>
        /// <param name="options">The retry options (attempts, base delay, strategy).</param>
        /// <returns>The builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="builder"/> or <paramref name="options"/> is <c>null</c>.
        /// </exception>
        public IProcessMiddlewareBuilder UseRetryPolicy(RetryOptions options)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(options);

            return builder.UseMiddleware(new RetryMiddleware(RetryConditions.ExitCodeZero(), options));
        }

        /// <summary>
        ///     Adds retry middleware using a custom retry classifier and custom options.
        /// </summary>
        /// <param name="retryClassifier">The classifier that decides whether a result is retryable.</param>
        /// <param name="options">The retry options (attempts, base delay, strategy).</param>
        /// <returns>The builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="builder"/>, <paramref name="retryClassifier"/>, or <paramref name="options"/> is <c>null</c>.
        /// </exception>
        public IProcessMiddlewareBuilder UseRetryPolicy(
            IRetryClassifier retryClassifier,
            RetryOptions options)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(retryClassifier);
            ArgumentNullException.ThrowIfNull(options);

            return builder.UseMiddleware(new RetryMiddleware(retryClassifier, options));
        }
    }
}
