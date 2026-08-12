/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core;
using CliInvoke.Core.Middleware;
using CliInvoke.Specializations.Internal.Localizations;
using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace CliInvoke.Specializations.Middleware;

/// <summary>
///     Middleware that rewrites the <see cref="InvocationContext.Configuration"/> to execute the
///     original command inside a Windows Command Processor (<c>cmd.exe</c>) process using the
///     <c>/c</c> switch, matching the flag used by
///     <see cref="CliInvoke.Specializations.CmdProcessInvoker"/>.
/// </summary>
/// <remarks>
///     Windows-only. Calls on any non-Windows platform throw
///     <see cref="PlatformNotSupportedException"/> at runtime, mirroring
///     <see cref="CliInvoke.Specializations.CmdProcessInvoker"/>.
/// </remarks>
[SupportedOSPlatform("windows")]
[UnsupportedOSPlatform("macos")]
[UnsupportedOSPlatform("linux")]
[UnsupportedOSPlatform("freebsd")]
[UnsupportedOSPlatform("android")]
[UnsupportedOSPlatform("browser")]
[UnsupportedOSPlatform("ios")]
[UnsupportedOSPlatform("tvos")]
[UnsupportedOSPlatform("watchos")]
internal sealed class CmdMiddleware : IProcessMiddleware
{
    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("watchos")]
    public async Task InvokeAsync(InvocationContext context, Func<InvocationContext, CancellationToken, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        ThrowIfUnsupported();

        string originalPath = context.Configuration.TargetFilePath;
        string originalArgs = context.Configuration.Arguments;

        string wrappedCommand = string.IsNullOrWhiteSpace(originalArgs)
            ? $"\"{originalPath}\""
            : $"\"{originalPath}\" {originalArgs}";

        string newArguments = $"/c {wrappedCommand}";

        ProcessConfiguration newConfig = new ProcessConfiguration("cmd.exe", newArguments);
        InvocationContext newContext = context.WithConfiguration(newConfig);

        await next(newContext, context.CancellationToken);

        // The terminal ran against the rewritten context, so propagate its result back to the
        // original chain context that the caller reads from.
        context.Result = newContext.Result;
    }

    private static void ThrowIfUnsupported()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(Resources
                .Exceptions_Cmd_OnlySupportedOnWindows);
        }
    }
}
