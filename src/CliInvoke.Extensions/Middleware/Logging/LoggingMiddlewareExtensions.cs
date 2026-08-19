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
///     Provides extension methods for configuring logging middleware.
/// </summary>
public static class LoggingMiddlewareExtensions
{
    /// <param name="builder">The middleware builder.</param>
    extension(IProcessMiddlewareBuilder builder)
    {
        /// <summary>
        ///     Adds <see cref="LoggingMiddleware"/> to the process invocation pipeline.
        /// </summary>
        /// <remarks>
        ///     The middleware resolves an <see cref="Microsoft.Extensions.Logging.ILogger"/>
        ///     from <see cref="MiddlewareContext.Items"/> using the well-known key
        ///     <c>"Logger"</c>. When absent, <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance"/>
        ///     is used as a no-op fallback.
        /// </remarks>
        /// <returns>The builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        public IProcessMiddlewareBuilder UseLogging()
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.UseMiddleware(new LoggingMiddleware());

            return builder;
        }
    }

}
