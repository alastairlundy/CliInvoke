/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core.Middleware;

/// <summary>
///     The async-only contract for process middleware. Implementations intercept
///     the pipeline by calling <c>next</c> to continue, or omitting
///     the call to short-circuit the chain.
/// </summary>
public interface IProcessMiddleware
{
    /// <summary>
    ///     Invokes the middleware, optionally calling <paramref name="next"/> to
    ///     continue the pipeline.
    /// </summary>
    /// <param name="context">The current invocation context.</param>
    /// <param name="next">The delegate to invoke the next middleware or the terminal pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InvokeAsync(InvocationContext context, Func<InvocationContext, CancellationToken, Task> next);
}
