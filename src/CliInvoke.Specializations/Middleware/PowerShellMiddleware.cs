/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core;
using CliInvoke.Core.Middleware;
using CliInvoke.Specializations.Configurations;

namespace CliInvoke.Specializations.Middleware;

/// <summary>
///     Middleware that rewrites the <see cref="InvocationContext.Configuration"/> to execute the
///     original command inside a PowerShell (<c>pwsh</c> / <c>pwsh.exe</c>) process using
///     <c>-NoProfile -NonInteractive -Command</c>. This is the single source of truth for PowerShell
///     wrapping; <see cref="CliInvoke.Specializations.PowershellProcessInvoker"/> is now a thin wrapper
///     around <see cref="CliInvoke.ProcessInvoker"/> with this middleware applied.
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
    private readonly IFilePathResolver _filePathResolver;
    private readonly bool _windowCreation;
    private readonly bool _useShellExecution;

    /// <summary>
    ///     Initialises a new instance of the <see cref="PowerShellMiddleware"/> class with the
    ///     default options (<c>windowCreation = false</c>, <c>useShellExecution = false</c>),
    ///     matching the unified defaults used by <see cref="ProcessConfiguration"/>.
    /// </summary>
    /// <param name="filePathResolver">
    ///     The resolver used to locate the <c>pwsh</c> / <c>pwsh.exe</c> executable. A default
    ///     <see cref="CliInvoke.FilePathResolver"/> is used when omitted.
    /// </param>
    public PowerShellMiddleware(IFilePathResolver? filePathResolver = null)
        : this(filePathResolver, windowCreation: false, useShellExecution: false)
    {
    }

    /// <summary>
    ///     Initialises a new instance of the <see cref="PowerShellMiddleware"/> class with the
    ///     supplied window-creation and shell-execution flags.
    /// </summary>
    /// <param name="filePathResolver">
    ///     The resolver used to locate the <c>pwsh</c> / <c>pwsh.exe</c> executable. A default
    ///     <see cref="CliInvoke.FilePathResolver"/> is used when omitted.
    /// </param>
    /// <param name="windowCreation">
    ///     Whether PowerShell should create a new window when launched.
    /// </param>
    /// <param name="useShellExecution">
    ///     Whether to use shell execution semantics for the wrapped process.
    /// </param>
    public PowerShellMiddleware(IFilePathResolver? filePathResolver, bool windowCreation, bool useShellExecution)
    {
        _filePathResolver = filePathResolver ?? new CliInvoke.FilePathResolver();
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
            ? $"& \"{originalPath}\""
            : $"& \"{originalPath}\" {originalArgs}";

        string newArguments = $"-NoProfile -NonInteractive -Command {wrappedCommand}";

        // The specialization configuration class is the single source of truth for the
        // pwsh target path and shell flags; this middleware just supplies the wrapped command and
        // forwards the full original configuration.
        ProcessConfiguration src = context.Configuration;
        ProcessConfiguration newConfig = new PowershellProcessConfiguration(
            _filePathResolver,
            newArguments,
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
