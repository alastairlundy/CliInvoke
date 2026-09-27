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
/// <remarks>
///     Composes the registered middleware into a nested-await (Russian-doll) pipeline.
///     The outermost middleware executes first and control unwinds back through each layer.
///     A single per-run <see cref="MiddlewareContext"/> carrying seeded <see cref="MiddlewareItems"/>
///     is exposed to every middleware via <c>InvocationContext.Middleware</c>. The chain is
///     <c>internal sealed</c> because it is an implementation detail of the invoker.
/// </remarks>
internal sealed class MiddlewareChain
{
    private readonly IReadOnlyList<IProcessMiddleware> _middleware;
    private readonly Func<InvocationContext, Task> _terminal;
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
        Func<InvocationContext, Task> terminal,
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
        // One MiddlewareContext per run, shared by every middleware in the chain. Its Next
        // delegate is re-pointed at each step's forward delegate while that step executes
        // (see InvokeStepAsync), so MiddlewareContext.Next never re-runs the current
        // middleware or anything upstream of it.
        MiddlewareContext middlewareContext =
            new MiddlewareContext(_terminal, cancellationToken, _initialItems);

        // Expose the run-scoped MiddlewareContext (with any seeded items) to every middleware
        // through InvocationContext.Middleware, so services such as an ILogger injected via
        // MiddlewareItems are reachable from within the chain.
        context.Middleware = middlewareContext;

        // Build the chain from last to first, wrapping each middleware around the next.
        // The terminal is the innermost delegate.
        Func<InvocationContext, Task> next = _terminal;

        for (int i = _middleware.Count - 1; i >= 0; i--)
        {
            IProcessMiddleware middleware = _middleware[i];
            Func<InvocationContext, Task> currentNext = next;
            next = ctx => InvokeStepAsync(ctx, middleware, currentNext, middlewareContext);
        }

        // Invoke the outermost middleware (or the terminal if no middleware registered).
        await next(context).ConfigureAwait(false);
    }

    /// <summary>
    ///     Invokes a single middleware step with <see cref="MiddlewareContext.Next"/> pointed at
    ///     this step's forward delegate for the step's whole dynamic extent (including middleware
    ///     running after <c>await next</c>), restoring the caller's value on exit so unwinding
    ///     middleware observe their own next delegate again.
    /// </summary>
    /// <param name="context">The invocation context to pass to the middleware.</param>
    /// <param name="middleware">The middleware to invoke.</param>
    /// <param name="currentNext">
    ///     The delegate that invokes the middleware after this one, or the terminal pipeline for
    ///     the innermost middleware.
    /// </param>
    /// <param name="middlewareContext">The run-scoped context whose <c>Next</c> is managed.</param>
    /// <returns>A task representing the middleware step execution.</returns>
    private static async Task InvokeStepAsync(
        InvocationContext context,
        IProcessMiddleware middleware,
        Func<InvocationContext, Task> currentNext,
        MiddlewareContext middlewareContext)
    {
        Func<InvocationContext, Task> previousNext = middlewareContext.Next;
        middlewareContext.Next = currentNext;

        try
        {
            await middleware.InvokeAsync(context, currentNext).ConfigureAwait(false);
        }
        finally
        {
            middlewareContext.Next = previousNext;
        }
    }
}
