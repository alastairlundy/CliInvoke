/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Middleware;

namespace CliInvoke.Extensions.Middleware;

/// <summary>
///     Provides extension methods for configuring output-truncation middleware on the process pipeline.
/// </summary>
public static class OutputTruncationMiddlewareExtensions
{
    /// <param name="builder">The middleware builder.</param>
    extension(IProcessMiddlewareBuilder builder)
    {
        /// <summary>
        ///     Adds <see cref="OutputTruncationMiddleware"/> to the process invocation pipeline using the
        ///     default 1 MB per-stream cap.
        /// </summary>
        /// <remarks>
        ///     The middleware is opt-in: it is not registered by <c>AddCliInvoke</c>. The options instance is
        ///     supplied directly rather than resolved from dependency injection, keeping the middleware fully
        ///     optional and overridable via <see cref="OutputTruncationMiddlewareExtensions"/>.
        /// </remarks>
        /// <returns>The builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <c>null</c>.</exception>
        public IProcessMiddlewareBuilder UseOutputTruncation()
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.UseMiddleware(new OutputTruncationMiddleware(TruncationOptions.Default));
        }

        /// <summary>
        ///     Adds <see cref="OutputTruncationMiddleware"/> to the process invocation pipeline using the
        ///     supplied <see cref="TruncationOptions"/>.
        /// </summary>
        /// <returns>The builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="builder"/> or <paramref name="options"/> is <c>null</c>.
        /// </exception>
        public IProcessMiddlewareBuilder UseOutputTruncation(TruncationOptions options)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(options);

            return builder.UseMiddleware(new OutputTruncationMiddleware(options));
        }
    }
}
