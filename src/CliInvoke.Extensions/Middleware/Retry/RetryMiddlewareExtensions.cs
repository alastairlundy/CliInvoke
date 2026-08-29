/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Middleware;
using CliInvoke.Core.Validation;
using CliInvoke.Validation;

namespace CliInvoke.Extensions.Middleware.Retry;

/// <summary>
///     Provides extension methods for configuring retry middleware on the process pipeline.
/// </summary>
public static class RetryMiddlewareExtensions
{
    /// <summary>
    ///     Builds a default retryable-conditions validator equivalent to the one registered by
    ///     <c>AddCliInvoke</c> (exit-code-zero classification).
    /// </summary>
    private static IProcessResultValidator<ProcessResult> DefaultRetryableValidator()
        => new ProcessResultValidator<ProcessResult>(
            [CommonValidationRules<ProcessResult>.RequiresExitCodeZero]);

    /// <param name="builder">The middleware builder.</param>
    extension(IProcessMiddlewareBuilder builder)
    {
        /// <summary>
        ///     Adds retry middleware to the process pipeline using the default options and the
        ///     default retryable-conditions validator (resolved from the dependency injection container).
        /// </summary>
        /// <returns>The builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <c>null</c>.</exception>
        public IProcessMiddlewareBuilder UseRetryPolicy()
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.UseMiddleware<RetryMiddleware>();
        }

        /// <summary>
        ///     Adds retry middleware using a custom retryable-conditions validator and the default options.
        /// </summary>
        /// <param name="validator">The validator whose <c>ShouldRetry</c> decides whether to retry.</param>
        /// <returns>The builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="builder"/> or <paramref name="validator"/> is <c>null</c>.
        /// </exception>
        public IProcessMiddlewareBuilder UseRetryPolicy(IProcessResultValidator<ProcessResult> validator)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(validator);

            return builder.UseMiddleware(new RetryMiddleware(validator, RetryOptions.Default));
        }

        /// <summary>
        ///     Adds retry middleware using the default retryable-conditions validator and custom options.
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

            return builder.UseMiddleware(new RetryMiddleware(DefaultRetryableValidator(), options));
        }

        /// <summary>
        ///     Adds retry middleware using a custom retryable-conditions validator and custom options.
        /// </summary>
        /// <param name="validator">The validator whose <c>ShouldRetry</c> decides whether to retry.</param>
        /// <param name="options">The retry options (attempts, base delay, strategy).</param>
        /// <returns>The builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="builder"/>, <paramref name="validator"/>, or <paramref name="options"/> is <c>null</c>.
        /// </exception>
        public IProcessMiddlewareBuilder UseRetryPolicy(
            IProcessResultValidator<ProcessResult> validator,
            RetryOptions options)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(validator);
            ArgumentNullException.ThrowIfNull(options);

            return builder.UseMiddleware(new RetryMiddleware(validator, options));
        }
    }
}
