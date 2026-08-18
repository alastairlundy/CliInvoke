/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Middleware;
using CliInvoke.Specializations.Configurations;

namespace CliInvoke.Specializations.Middleware;

/// <summary>
///     Middleware that rewrites the <see cref="InvocationContext.Configuration"/> to execute the
///     original command inside a Windows Command Processor (<c>cmd.exe</c>) process using the
///     <c>/c</c> switch. This is the single source of truth for CMD wrapping;
///     <see cref="CliInvoke.Specializations.CmdProcessInvoker"/> is now a thin wrapper around
///     <see cref="CliInvoke.ProcessInvoker"/> with this middleware applied.
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

        string originalPath = context.Configuration.TargetFilePath;
        string originalArgs = context.Configuration.Arguments;

        string wrappedCommand = string.IsNullOrWhiteSpace(originalArgs)
            ? $"\"{originalPath}\""
            : $"\"{originalPath}\" {originalArgs}";

        // The specialization configuration class is the single source of truth for the cmd.exe
        // target and the /c switch; this middleware just supplies the wrapped command and
        // forwards the full original configuration.
        ProcessConfiguration src = context.Configuration;
        ProcessConfiguration newConfig = new CmdProcessConfiguration(
            wrappedCommand,
            src.RedirectStandardInput,
            outputRedirection: context.Mode != InvocationMode.Raw,
            workingDirectoryPath: src.WorkingDirectoryPath,
            requiresAdministrator: src.RequiresAdministrator,
            environmentVariables: new Dictionary<string, string>(src.EnvironmentVariables),
            credentials: src.Credential,
            standardInput: src.StandardInput,
            standardInputEncoding: src.StandardInputEncoding,
            standardOutputEncoding: src.StandardOutputEncoding,
            standardErrorEncoding: src.StandardErrorEncoding,
            processResourcePolicy: src.ResourcePolicy,
            windowCreation: src.WindowCreation);
        InvocationContext newContext = context.WithConfiguration(newConfig);

        await next(newContext);

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
