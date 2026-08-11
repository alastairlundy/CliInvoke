/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Collections.Generic;
using System.Linq;

using CliInvoke;
using CliInvoke.Core.Middleware;

namespace CliInvoke.Extensions.Middleware;

/// <summary>
///     Provides extension methods for configuring logging middleware.
/// </summary>
public static class LoggingMiddlewareExtensions
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
    /// <param name="builder">The middleware builder.</param>
    /// <returns>The builder for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IProcessMiddlewareBuilder UseLogging(this IProcessMiddlewareBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Use(new LoggingMiddleware());

        return builder;
    }

    /// <summary>
    ///     Creates a new <see cref="ProcessInvoker"/> with <see cref="LoggingMiddleware"/>
    ///     prepended so that every invocation logs entry and exit details.
    /// </summary>
    /// <remarks>
    ///     The middleware resolves an <see cref="Microsoft.Extensions.Logging.ILogger"/>
    ///     from the chain's shared <see cref="MiddlewareItems"/> using the well-known key
    ///     <c>"Logger"</c>. When absent, a no-op logger is used.
    /// </remarks>
    /// <param name="invoker">The existing process invoker.</param>
    /// <returns>A new process invoker with logging middleware applied.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="invoker"/> is <c>null</c>.
    /// </exception>
    public static ProcessInvoker UseLogging(this ProcessInvoker invoker)
    {
        ArgumentNullException.ThrowIfNull(invoker);

        IEnumerable<IProcessMiddleware> newList = invoker.Middlewares.Prepend(new LoggingMiddleware());
        return new ProcessInvoker(invoker.ExternalProcessFactory, newList, invoker.SharedItems);
    }
}
