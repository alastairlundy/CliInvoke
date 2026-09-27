/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core.Middleware;

/// <summary>
///     The per-run state exposed to middleware via the chain walker. One instance is constructed
///     per <see cref="MiddlewareChain.RunAsync(InvocationContext, CancellationToken)"/> call and
///     shared by every middleware in the chain; it carries the run's single cancellation token.
/// </summary>
public sealed class MiddlewareContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MiddlewareContext"/> class.
    /// </summary>
    /// <param name="next">
    ///     The delegate that continues the chain (the terminal pipeline for a freshly created
    ///     context). The chain walker re-points this at each step's forward delegate while that
    ///     step executes and restores it when the step returns, so during a middleware's execution
    ///     it always identifies the delegate that invokes the next middleware or the terminal.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token for the chain run. The same token is shared by every middleware
    ///     in the chain.
    /// </param>
    /// <param name="items">
    ///     Optional pre-seeded items. When provided, the context shares this instance so
    ///     values injected before the chain runs (e.g. a logger) are visible
    ///     to every middleware. When omitted, a fresh <see cref="MiddlewareItems"/> is created.
    /// </param>
    public MiddlewareContext(
        Func<InvocationContext, Task> next,
        CancellationToken cancellationToken,
        MiddlewareItems? items = null)
    {
        Next = next;
        CancellationToken = cancellationToken;
        Items = items ?? new MiddlewareItems();
    }

    /// <summary>
    ///     Gets the delegate to invoke the next middleware in the chain, or the terminal pipeline
    ///     for the innermost middleware. The chain walker points this at the forward delegate of
    ///     the middleware currently executing and restores the caller's value when that middleware
    ///     returns, so invoking it only ever runs middleware further down the chain, never the
    ///     current middleware or anything upstream of it. This property is read-only to consumers
    ///     and retained for diagnostics and introspection. Middleware should normally receive the
    ///     <c>next</c> delegate via the <see cref="IProcessMiddleware.InvokeAsync"/> parameter
    ///     rather than reading it from this property.
    /// </summary>
    public Func<InvocationContext, Task> Next { get; internal set; }

    /// <summary>
    ///     Gets the cancellation token for the chain run this context belongs to. The same token
    ///     is shared by every middleware in the chain.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    ///     Gets the items dictionary shared by every middleware in the chain.
    /// </summary>
    public MiddlewareItems Items { get; }
}
