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
/// <remarks>
///     <see cref="IProcessMiddleware"/> is a first-class participant in the Process
///     Invocation Pipeline. It wraps cross-cutting concerns (logging, diagnostics,
///     policy enforcement, validation, instrumentation) around process execution
///     without coupling those concerns to the invoker. Registered middleware form an
///     ordered chain: each instance receives the <c>next</c> delegate that
///     invokes the subsequent middleware and, at the centre, the terminal pipeline that
///     actually starts the process. Calling <c>next</c> continues the chain; omitting the
///     call short-circuits it, so a middleware may observe, transform, or veto an
///     invocation before the process is ever launched. Middleware are resolved and invoked
///     by <see cref="MiddlewareChain"/> in registration order.
/// </remarks>
public interface IProcessMiddleware
{
    /// <summary>
    ///     Invokes the middleware, optionally calling <paramref name="next"/> to
    ///     continue the pipeline.
    /// </summary>
    /// <remarks>
    ///     The <c>next</c> delegate no longer carries a <see cref="CancellationToken"/> parameter.
    ///     Middleware must read the cancellation token from <c>context.CancellationToken</c>.
    /// </remarks>
    /// <param name="context">The current invocation context.</param>
    /// <param name="next">The delegate to invoke the next middleware or the terminal pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InvokeAsync(InvocationContext context, Func<InvocationContext, Task> next);
}
