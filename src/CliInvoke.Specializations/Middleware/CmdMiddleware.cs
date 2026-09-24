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
        ProcessConfiguration source = new(src.TargetFilePath, src.Arguments, outputRedirection: context.Mode != InvocationMode.Raw)
        {
            ArgumentList = src.ArgumentList,
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
            shellTargetPath: "cmd.exe",
            runnerArgs: "/c",
            kind: ShellKind.Cmd,
            windowCreation: src.WindowCreation,
            useShellExecution: src.UseShellExecution);

        InvocationContext newContext = context.WithConfiguration(rewritten);

        await next(newContext).ConfigureAwait(false);

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