/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Middleware;
using CliInvoke.Core.Validation;

namespace CliInvoke.Extensions.Middleware.Validation;

/// <summary>
///     Provides extension methods for configuring post-exit validation middleware.
/// </summary>
public static class PostExitValidationExtensions
{
    /// <param name="builder">The middleware builder.</param>
    extension(IProcessMiddlewareBuilder builder)
    {
        /// <summary>
        ///     Adds post-exit validation middleware to the process pipeline.
        /// </summary>
        /// <param name="validator">The validator applied to the process result.</param>
        /// <returns>The builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="builder"/> or <paramref name="validator"/> is <c>null</c>.
        /// </exception>
        public IProcessMiddlewareBuilder UsePostExitValidation(IProcessResultValidator<ProcessResult> validator)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(validator);

            builder.UseMiddleware(new PostExitValidationMiddleware(validator));

            return builder;
        }
    }

}
