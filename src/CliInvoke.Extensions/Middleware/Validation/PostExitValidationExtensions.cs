/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;

using CliInvoke.Core.Middleware;

namespace CliInvoke.Extensions.Middleware.Validation;

/// <summary>
///     Provides extension methods for configuring post-exit validation middleware.
/// </summary>
public static class PostExitValidationExtensions
{
    /// <summary>
    ///     Adds post-exit validation middleware to the process pipeline.
    /// </summary>
    /// <param name="builder">The middleware builder.</param>
    /// <param name="options">The validation options applied to the process result.</param>
    /// <returns>The builder for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="builder"/> or <paramref name="options"/> is <c>null</c>.
    /// </exception>
    public static IProcessMiddlewareBuilder UsePostExitValidation(
        this IProcessMiddlewareBuilder builder,
        PostExitValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        builder.Use(new PostExitValidationMiddleware(options));

        return builder;
    }
}
