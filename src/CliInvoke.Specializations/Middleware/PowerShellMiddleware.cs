/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Internal;

namespace CliInvoke.Specializations.Middleware;

/// <summary>
///     Middleware that rewrites the <see cref="InvocationContext.Configuration"/> to execute the
///     original command inside a PowerShell (<c>pwsh</c> / <c>pwsh.exe</c>) process using
///     <c>-NoProfile -NonInteractive -Command</c>. This is the single source of truth for PowerShell
///     wrapping.
/// </summary>
/// <remarks>
///     Supports Windows, macOS, macCatalyst, Linux, and FreeBSD. Calls on Android, iOS, tvOS, watchOS,
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
    private readonly ShellMiddlewareOptions _options;

    /// <summary>
    ///     Initialises a new instance of the <see cref="PowerShellMiddleware"/> class with
    ///     default options (<see cref="ShellMiddlewareOptions.Default"/>).
    /// </summary>
    /// <param name="options">
    ///     The PowerShell middleware options. Defaults to <see cref="ShellMiddlewareOptions.Default"/>.
    /// </param>
    public PowerShellMiddleware(ShellMiddlewareOptions? options = null)
    {
        _options = options ?? ShellMiddlewareOptions.Default;
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
    public async Task InvokeAsync(InvocationContext context, Func<InvocationContext, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        ThrowIfUnsupported();

        ProcessConfiguration src = context.Configuration;
        ProcessConfiguration source = new(src.TargetFilePath, src.Arguments, outputRedirection: context.Mode != InvocationMode.Raw)
        {
            RedirectStandardInput = src.RedirectStandardInput,
            RequiresAdministrator = src.RequiresAdministrator,
            WorkingDirectoryPath = src.WorkingDirectoryPath,
            EnvironmentVariables = new Dictionary<string, string>(src.EnvironmentVariables),
            Credential = src.Credential,
            StandardInput = src.StandardInput,
            StandardInputEncoding = src.StandardInputEncoding,
            StandardOutputEncoding = src.StandardOutputEncoding,
            StandardErrorEncoding = src.StandardErrorEncoding,
            ResourcePolicy = src.ResourcePolicy,
        };

        ProcessConfiguration rewritten = ShellRewriter.Rewrite(
            source,
            shellTargetPath: OperatingSystem.IsWindows() ? "pwsh.exe" : "pwsh",
            runnerArgs: "-NoProfile -NonInteractive -Command",
            kind: ShellKind.PowerShell,
            windowCreation: _options.WindowCreation,
            useShellExecution: _options.UseShellExecution);

        InvocationContext newContext = context.WithConfiguration(rewritten);

        await next(newContext).ConfigureAwait(false);

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
