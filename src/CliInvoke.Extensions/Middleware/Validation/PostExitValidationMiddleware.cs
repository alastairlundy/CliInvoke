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
using CliInvoke.Core.Validation;

namespace CliInvoke.Extensions.Middleware.Validation;

/// <summary>
///     Middleware that runs a post-exit validation rule against the resolved
///     <see cref="ProcessResult"/> and throws when the rule reports a failure.
/// </summary>
internal sealed class PostExitValidationMiddleware : IProcessMiddleware
{
    /// <summary>
    ///     Initialises a new instance of the <see cref="PostExitValidationMiddleware"/> class.
    /// </summary>
    /// <param name="validator">The validator applied to the process result.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validator"/> is <c>null</c>.</exception>
    public PostExitValidationMiddleware(IProcessResultValidator<ProcessResult> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        _validator = validator;
    }

    private readonly IProcessResultValidator<ProcessResult> _validator;

    /// <summary>
    ///     Executes the middleware pipeline and validates the resulting process result.
    /// </summary>
    /// <param name="context">The current invocation context.</param>
    /// <param name="next">The delegate to invoke the next middleware or the terminal pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(InvocationContext context, Func<InvocationContext, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await next(context);

        ProcessResult? result = context.Result;

        if (result is null)
            return;

        ValidationFailure<ProcessResult>[] failures = _validator.GetValidationFailures(result);

        if (failures.Length > 0)
            throw new ProcessValidationException(result, BuildFailureMessage(failures));
    }

    private static string BuildFailureMessage(ValidationFailure<ProcessResult>[] failures)
    {
        string[] lines = new string[failures.Length];

        for (int i = 0; i < failures.Length; i++)
        {
            ValidationFailure<ProcessResult> failure = failures[i];
            lines[i] = $"{failure.Rule.Name}: {failure.FailureMessage}";
        }

        return "Process result failed validation: " + string.Join("; ", lines);
    }
}