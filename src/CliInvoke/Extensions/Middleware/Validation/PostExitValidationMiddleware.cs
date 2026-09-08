/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core;
using CliInvoke.Core.Middleware;
using CliInvoke.Core.Validation;

namespace CliInvoke.Extensions.Middleware.Validation;

/// <summary>
///     Middleware that folds a post-exit validator's rules into the invocation's exit
///     configuration before the next stage runs, so the pipeline's single validation path
///     evaluates them. It performs no post-next evaluation itself.
/// </summary>
internal sealed class PostExitValidationMiddleware : IProcessMiddleware
{
    public PostExitValidationMiddleware(IProcessResultValidator<ProcessResult> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        _validator = validator;
    }

    private readonly IProcessResultValidator<ProcessResult> _validator;

    public async Task InvokeAsync(InvocationContext context, Func<InvocationContext, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        ValidationRule<ProcessResult>[] mergedRules =
        [
            .. context.ExitConfiguration.ValidationRules,
            .. _validator.ValidationRules
        ];

        await next(context.WithExitConfiguration(
            ProcessExitConfigurationCreationExtensions.WithValidationRules(context.ExitConfiguration, mergedRules))).ConfigureAwait(false);
    }
}