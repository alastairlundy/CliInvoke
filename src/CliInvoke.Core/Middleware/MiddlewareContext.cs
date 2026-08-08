/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core.Middleware;

/// <summary>
///     The per-chain-step state exposed to middleware via the chain walker.
///     Constructed by the chain walker for each middleware invocation.
/// </summary>
public sealed class MiddlewareContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MiddlewareContext"/> class.
    /// </summary>
    /// <param name="next">The delegate to invoke the next middleware or terminal.</param>
    /// <param name="cancellationToken">The cancellation token for this step.</param>
    public MiddlewareContext(
        Func<ProcessInvocationContext, CancellationToken, Task> next,
        CancellationToken cancellationToken)
    {
        Next = next;
        CancellationToken = cancellationToken;
        Items = new MiddlewareItems();
    }

    /// <summary>
    ///     Gets the delegate to invoke the next middleware or the terminal pipeline.
    /// </summary>
    public Func<ProcessInvocationContext, CancellationToken, Task> Next { get; }

    /// <summary>
    ///     Gets the cancellation token for this middleware step.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    ///     Gets the per-step items dictionary for sharing data between middleware.
    /// </summary>
    public MiddlewareItems Items { get; }
}
