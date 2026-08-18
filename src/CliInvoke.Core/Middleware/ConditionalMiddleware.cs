/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core.Middleware;

/// <summary>
///     An internal middleware that conditionally routes execution to a sub-pipeline
///     when the predicate returns <c>true</c>, or bypasses directly to the outer
///     <c>next</c> delegate when it returns <c>false</c>.
/// </summary>
internal sealed class ConditionalMiddleware : IProcessMiddleware
{
    private readonly Func<InvocationContext, Task<bool>> _predicate;
    private readonly IReadOnlyList<IProcessMiddleware> _subPipeline;
    private readonly Func<Type, IProcessMiddleware> _resolver;

    /// <summary>
    ///     Initialises a new instance of the <see cref="ConditionalMiddleware"/> class.
    /// </summary>
    /// <param name="predicate">The async condition evaluated before each invocation.</param>
    /// <param name="subPipeline">The ordered sub-pipeline middleware to run when the predicate is true.</param>
    /// <param name="resolver">The resolver used to construct a <see cref="MiddlewareChain"/> for the sub-pipeline.</param>
    public ConditionalMiddleware(
        Func<InvocationContext, Task<bool>> predicate,
        IReadOnlyList<IProcessMiddleware> subPipeline,
        Func<Type, IProcessMiddleware> resolver)
    {
        _predicate = predicate;
        _subPipeline = subPipeline;
        _resolver = resolver;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(InvocationContext context, Func<InvocationContext, Task> next)
    {
        if (await _predicate(context))
        {
            MiddlewareChain subChain = new MiddlewareChain(_subPipeline, next);
            await subChain.RunAsync(context, context.CancellationToken);
        }
        else
        {
            await next(context);
        }
    }
}
