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
///     original command inside a PowerShell (<c>pwsh</c> / <c>pwsh.exe</c>) process using
///     <c>-NoProfile -NonInteractive -Command</c>, matching the flag set used by
///     <see cref="CliInvoke.Specializations.PowershellProcessInvoker"/>.
/// </summary>
/// <remarks>
///     Supports the same platforms as <see cref="CliInvoke.Specializations.PowershellProcessInvoker"/>:
///     Windows, macOS, macCatalyst, Linux, and FreeBSD. Calls on Android, iOS, tvOS, watchOS,
///     or browser throw <see cref="PlatformNotSupportedException"/> at runtime.
/// </remarks>
[SupportedOSPlatform("windows")]
[SupportedOSPlatform("macos")]
[SupportedOSPlatform("maccatalyst")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("freebsd")]
[UnsupportedOSPlatform("browser")]
[UnsupportedOSPlatform("android")]
[UnsupportedOSPlatform("ios")]
[UnsupportedOSPlatform("tvos")]
[UnsupportedOSPlatform("watchos")]
internal sealed class PowerShellMiddleware : IProcessMiddleware
{
    private readonly bool _windowCreation;
    private readonly bool _useShellExecution;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PowerShellMiddleware"/> class with the
    ///     default options (<c>windowCreation = false</c>, <c>useShellExecution = false</c>),
    ///     matching the defaults used by <see cref="CliInvoke.Specializations.PowershellProcessInvoker"/>.
    /// </summary>
    public PowerShellMiddleware()
        : this(windowCreation: false, useShellExecution: false)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PowerShellMiddleware"/> class with the
    ///     supplied window-creation and shell-execution flags.
    /// </summary>
    /// <param name="windowCreation">
    ///     Whether PowerShell should create a new window when launched.
    /// </param>
    /// <param name="useShellExecution">
    ///     Whether to use shell execution semantics for the wrapped process.
    /// </param>
    public PowerShellMiddleware(bool windowCreation, bool useShellExecution)
    {
        _windowCreation = windowCreation;
        _useShellExecution = useShellExecution;
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("android")]
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
            ? originalPath
            : $"\"{originalPath}\" {originalArgs}";

        string newArguments = $"-NoProfile -NonInteractive -Command \"{wrappedCommand}\"";

        string targetFilePath = OperatingSystem.IsWindows() ? "pwsh.exe" : "pwsh";

        ProcessConfiguration newConfig = new MiddlewareProcessConfiguration(
            targetFilePath,
            newArguments,
            windowCreation: _windowCreation,
            useShellExecution: _useShellExecution);

        InvocationContext newContext = context.WithConfiguration(newConfig);

        await next(newContext, context.CancellationToken);

        // The terminal ran against the rewritten context, so propagate its result back to the
        // original chain context that the caller reads from.
        context.Result = newContext.Result;
    }

    private static void ThrowIfUnsupported()
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() ||
            OperatingSystem.IsBrowser() || OperatingSystem.IsWatchOS())
        {
            throw new PlatformNotSupportedException(Resources
                .Exceptions_Powershell_OnlySupportedOnDesktop);
        }
    }
}
