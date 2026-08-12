/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Threading;
using System.Threading.Tasks;

using CliInvoke.Core.Exceptions;
using CliInvoke.Core.Middleware;

namespace CliInvoke.Extensions.Middleware.Validation;

/// <summary>
///     Middleware that runs a post-exit validation rule against the resolved
///     <see cref="ProcessResult"/> and throws when the rule reports a failure.
/// </summary>
internal sealed class PostExitValidationMiddleware : IProcessMiddleware
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PostExitValidationMiddleware"/> class.
    /// </summary>
    /// <param name="options">The validation options applied to the process result.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
    public PostExitValidationMiddleware(PostExitValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    private readonly PostExitValidationOptions _options;

    /// <summary>
    ///     Executes the middleware pipeline and validates the resulting process result.
    /// </summary>
    /// <param name="context">The current invocation context.</param>
    /// <param name="next">The delegate to invoke the next middleware or the terminal pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(InvocationContext context, Func<InvocationContext, CancellationToken, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await next(context, context.CancellationToken);

        ProcessResult? result = context.Result;

        if (result is null)
            return;

        string? message = _options.Rule(result);

        if (message is not null)
            throw new ProcessValidationException(result, message);
    }
}
