/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core.Middleware;

/// <summary>
///     Walks a composed middleware chain in nested-await (Russian-doll) order,
///     executing from the first registered middleware to the terminal pipeline.
/// </summary>
internal sealed class MiddlewareChain
{
    private readonly IReadOnlyList<IProcessMiddleware> _middleware;
    private readonly Func<InvocationContext, CancellationToken, Task> _terminal;
    private readonly MiddlewareItems? _initialItems;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MiddlewareChain"/> class.
    /// </summary>
    /// <param name="middleware">The ordered list of middleware to execute.</param>
    /// <param name="terminal">The terminal delegate (the pipeline) invoked after all middleware.</param>
    /// <param name="initialItems">
    ///     Optional pre-seeded items shared across every middleware step. Use this to inject
    ///     framework-level services (such as a logger) into the chain before it runs.
    /// </param>
    public MiddlewareChain(
        IReadOnlyList<IProcessMiddleware> middleware,
        Func<InvocationContext, CancellationToken, Task> terminal,
        MiddlewareItems? initialItems = null)
    {
        _middleware = middleware;
        _terminal = terminal;
        _initialItems = initialItems;
    }

    /// <summary>
    ///     Runs the middleware chain, executing middleware in registration order
    ///     using nested awaits (Russian-doll model).
    /// </summary>
    /// <param name="context">The invocation context to pass through the chain.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous chain execution.</returns>
    public async Task RunAsync(InvocationContext context, CancellationToken cancellationToken)
    {
        // Build the chain from last to first, wrapping each middleware around the next.
        // The terminal is the innermost delegate.
        Func<InvocationContext, CancellationToken, Task> next = _terminal;

        for (int i = _middleware.Count - 1; i >= 0; i--)
        {
            IProcessMiddleware middleware = _middleware[i];
            Func<InvocationContext, CancellationToken, Task> currentNext = next;
            next = (ctx, ct) => middleware.InvokeAsync(ctx, currentNext);
        }

        // Expose a per-step MiddlewareContext (with any seeded items) to every middleware
        // through InvocationContext.Middleware, so services such as an ILogger injected via
        // MiddlewareItems are reachable from within the chain.
        context.Middleware = new MiddlewareContext(next, cancellationToken, _initialItems);

        // Invoke the outermost middleware (or the terminal if no middleware registered).
        await next(context, cancellationToken);
    }
}
