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
/// 
/// </summary>
[UnsupportedOSPlatform("browser")]
[UnsupportedOSPlatform("ios")]
[UnsupportedOSPlatform("tvos")]
[UnsupportedOSPlatform("watchos")]
internal sealed class DefaultShellMiddleware : IProcessMiddleware
{
    private readonly IShellDetector _shellDetector;
    private readonly ShellMiddlewareOptions _options;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="shellDetector"></param>
    /// <param name="options"></param>
    public DefaultShellMiddleware(IShellDetector shellDetector, ShellMiddlewareOptions? options = null)
    {
        _shellDetector = shellDetector;
        _options = options ?? ShellMiddlewareOptions.Default;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task InvokeAsync(InvocationContext context, Func<InvocationContext, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        ThrowIfUnsupported();
        
        ShellInformation shell = await _shellDetector.ResolveDefaultShellAsync(context.CancellationToken).ConfigureAwait(false);
        
        string shellName = Path.GetFileNameWithoutExtension(shell.TargetFilePath.Name);

        ShellKind kind = shellName.Equals("cmd", StringComparison.OrdinalIgnoreCase)
            ? ShellKind.Cmd
            : shellName.Equals("pwsh", StringComparison.OrdinalIgnoreCase) ||
              shellName.Equals("powershell", StringComparison.OrdinalIgnoreCase)
                ? ShellKind.PowerShell
                : ShellKind.Posix;

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
            shellTargetPath: shell.TargetFilePath.FullName,
            runnerArgs: string.Empty,
            kind: kind,
            delivery: kind == ShellKind.Cmd ? ShellDelivery.Arguments : ShellDelivery.ArgumentList,
            windowCreation: _options.WindowCreation,
            useShellExecution: _options.UseShellExecution);

        InvocationContext newContext = context.WithConfiguration(rewritten);

        await next(newContext).ConfigureAwait(false);

        context.Result = newContext.Result;   
    }
    
    private static void ThrowIfUnsupported()
    {
        if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() ||
            OperatingSystem.IsBrowser() || OperatingSystem.IsWatchOS())
        {
            throw new PlatformNotSupportedException(Resources
                .Exceptions_Powershell_OnlySupportedOnDesktop);
        }
    }
}