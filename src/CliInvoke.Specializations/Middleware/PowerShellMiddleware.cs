/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Specializations.Configurations;
using CliInvoke.Specializations.Internal;

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
    private readonly PowerShellMiddlewareOptions _options;

    /// <summary>
    ///     Initialises a new instance of the <see cref="PowerShellMiddleware"/> class with
    ///     default options (<see cref="PowerShellMiddlewareOptions.Default"/>).
    /// </summary>
    /// <param name="options">
    ///     The PowerShell middleware options. Defaults to <see cref="PowerShellMiddlewareOptions.Default"/>.
    /// </param>
    public PowerShellMiddleware(PowerShellMiddlewareOptions? options = null)
    {
        _options = options ?? PowerShellMiddlewareOptions.Default;
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

        string originalPath = context.Configuration.TargetFilePath;
        string originalArgs = context.Configuration.Arguments;

        // Escape both the target and the arguments so they are passed to the
        // wrapped command as literal data. Without this, shell metacharacters in
        // the arguments (e.g. ';', '|', '&', '$(...)') would be re-interpreted by
        // PowerShell as additional commands — a command-injection risk.
        string safePath = ShellArgumentEscaper.EscapeForPowerShell(originalPath);
        string safeArgs = ShellArgumentEscaper.EscapeForPowerShell(originalArgs);

        string wrappedCommand = string.IsNullOrWhiteSpace(safeArgs)
            ? $"& \"{safePath}\""
            : $"& \"{safePath}\" {safeArgs}";

        // Emit the wrapper as a verbatim ArgumentList so the OS command-line parser does NOT
        // re-tokenize it before PowerShell parses it. A single re-tokenized Arguments string
        // would let a '"' in the value break the OS-level quoting and let PowerShell reassemble
        // a second command (command-injection). ArgumentList is passed through unchanged.
        IReadOnlyList<string> argumentList =
        [
            "-NoProfile",
            "-NonInteractive",
            "-Command",
            wrappedCommand,
        ];

        // The specialization configuration class is the single source of truth for the
        // pwsh target path and shell flags; this middleware just supplies the wrapped command and
        // forwards the full original configuration.
        ProcessConfiguration src = context.Configuration;
        ProcessConfiguration newConfig = new PowershellProcessConfiguration(
            string.Empty,
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
            windowCreation: _options.WindowCreation,
            useShellExecution: _options.UseShellExecution,
            argumentList: argumentList);

        InvocationContext newContext = context.WithConfiguration(newConfig);

        await next(newContext);

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
