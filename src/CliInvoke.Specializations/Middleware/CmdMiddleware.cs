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
///     original command inside a Windows Command Processor (<c>cmd.exe</c>) process using the
///     <c>/c</c> switch. This is the single source of truth for CMD wrapping.
/// </summary>
/// <remarks>
///     Windows-only. Calls on any non-Windows platform throw
///     <see cref="PlatformNotSupportedException"/> at runtime.
///     <para>
///         Unlike <see cref="PowerShellMiddleware"/> and <see cref="DefaultShellMiddleware"/>,
///         this middleware takes <see cref="ProcessConfiguration.WindowCreation"/> and
///         <see cref="ProcessConfiguration.UseShellExecution"/> from the source
///         <see cref="ProcessConfiguration"/> per invocation; it does not read
///         <see cref="ShellMiddlewareOptions"/>, so registering that type does not affect it.
///     </para>
///     <para>
///         The source configuration's <see cref="ProcessConfiguration.OutputRedirection"/> flag is
///         overwritten on every invocation to match the invocation mode: forced off in
///         <see cref="InvocationMode.Raw"/>, forced on otherwise. This protective overwrite prevents
///         a raw-mode pipe deadlock. Raw mode waits for process exit without draining redirected
///         output, so a redirected pipe nobody reads would fill up and block the child process.
///     </para>
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
    public async Task InvokeAsync(InvocationContext context, Func<InvocationContext, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        ThrowIfUnsupported();

        ProcessConfiguration src = context.Configuration;
        ProcessConfiguration source = ProcessConfigurationDerivation.Derive(
            src,
            b => b.SetOutputRedirection(context.Mode != InvocationMode.Raw));

        ProcessConfiguration rewritten = ShellRewriter.Rewrite(
            source,
            shellTargetPath: "cmd.exe",
            runnerArgs: "/c",
            kind: ShellKind.Cmd,
            windowCreation: src.WindowCreation,
            useShellExecution: src.UseShellExecution);

        InvocationContext newContext = context.WithConfiguration(rewritten);

        await next(newContext).ConfigureAwait(false);

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