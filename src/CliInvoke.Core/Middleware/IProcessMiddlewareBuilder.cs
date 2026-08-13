/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core.Middleware;

/// <summary>
///     A builder for composing a sequence of <see cref="IProcessMiddleware"/> instances.
/// </summary>
public interface IProcessMiddlewareBuilder
{
    /// <summary>
    ///     Adds a middleware to the pipeline.
    /// </summary>
    /// <param name="middleware">The middleware to add.</param>
    /// <returns>The builder for fluent chaining.</returns>
    IProcessMiddlewareBuilder Use(IProcessMiddleware middleware);

    /// <summary>
    ///     Builds the read-only list of middleware in registration order.
    /// </summary>
    /// <returns>A read-only list of middleware instances.</returns>
    IReadOnlyList<IProcessMiddleware> Build();
}
