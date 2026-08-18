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
    ///     Adds a middleware instance to the pipeline.
    /// </summary>
    /// <param name="middleware">The middleware instance to add.</param>
    /// <returns>The builder for fluent chaining.</returns>
    IProcessMiddlewareBuilder UseMiddleware(IProcessMiddleware middleware);

    /// <summary>
    ///     Adds a middleware type to the pipeline. The type is resolved through the
    ///     builder's resolver at <see cref="Build"/> time.
    /// </summary>
    /// <typeparam name="T">A type implementing <see cref="IProcessMiddleware"/>.</typeparam>
    /// <returns>The builder for fluent chaining.</returns>
    IProcessMiddlewareBuilder UseMiddleware<T>() where T : IProcessMiddleware;

    /// <summary>
    ///     Adds conditional middleware that runs a sub-pipeline only when the
    ///     synchronous predicate returns <c>true</c>.
    /// </summary>
    /// <param name="predicate">The condition evaluated before each invocation.</param>
    /// <param name="configuration">An action that configures the sub-pipeline builder.</param>
    /// <returns>The builder for fluent chaining.</returns>
    IProcessMiddlewareBuilder UseWhen(Func<InvocationContext, bool> predicate, Action<IProcessMiddlewareBuilder> configuration);

    /// <summary>
    ///     Adds conditional middleware that runs a sub-pipeline only when the
    ///     asynchronous predicate returns <c>true</c>.
    /// </summary>
    /// <param name="predicate">The async condition evaluated before each invocation.</param>
    /// <param name="configuration">An action that configures the sub-pipeline builder.</param>
    /// <returns>The builder for fluent chaining.</returns>
    IProcessMiddlewareBuilder UseWhen(Func<InvocationContext, Task<bool>> predicate, Action<IProcessMiddlewareBuilder> configuration);

    /// <summary>
    ///     Builds the middleware pipeline, resolving any type-based entries through
    ///     the builder's internal resolver.
    /// </summary>
    /// <returns>An ordered, read-only list of middleware instances.</returns>
    IReadOnlyList<IProcessMiddleware> Build();
}
