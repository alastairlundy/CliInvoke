/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core;
using CliInvoke.Core.Middleware;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CliInvoke.Specializations.Middleware;

/// <summary>
///     Middleware that rewrites the <see cref="InvocationContext.Configuration"/> to execute the
///     original command inside a PowerShell (<c>pwsh</c>) process.
/// </summary>
internal sealed class PowerShellMiddleware : IProcessMiddleware
{
    /// <inheritdoc />
    public async Task InvokeAsync(InvocationContext context, Func<InvocationContext, CancellationToken, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        string originalPath = context.Configuration.TargetFilePath;
        string originalArgs = context.Configuration.Arguments;

        string wrappedCommand = string.IsNullOrWhiteSpace(originalArgs)
            ? originalPath
            : $"\"{originalPath}\" {originalArgs}";

        string newArguments = $"-NoProfile -Command \"{wrappedCommand}\"";

        ProcessConfiguration newConfig = new ProcessConfiguration("pwsh", newArguments);
        InvocationContext newContext = context.WithConfiguration(newConfig);

        await next(newContext, context.CancellationToken);
    }
}
