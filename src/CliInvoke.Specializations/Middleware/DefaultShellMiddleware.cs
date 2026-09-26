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
    private readonly IServiceProvider _serviceProvider;
    private readonly ShellMiddlewareOptions _options;

    /// <summary>
    ///
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="options"></param>
    public DefaultShellMiddleware(IServiceProvider serviceProvider, ShellMiddlewareOptions? options = null)
    {
        _serviceProvider = serviceProvider;
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

        IShellDetector shellDetector = _serviceProvider.GetRequiredService<IShellDetector>();
        ShellInformation shell = await shellDetector.ResolveDefaultShellAsync(context.CancellationToken).ConfigureAwait(false);
        
        string shellName = Path.GetFileNameWithoutExtension(shell.TargetFilePath.Name);

        ShellKind kind = shellName.Equals("cmd", StringComparison.OrdinalIgnoreCase)
            ? ShellKind.Cmd
            : shellName.Equals("pwsh", StringComparison.OrdinalIgnoreCase) ||
              shellName.Equals("powershell", StringComparison.OrdinalIgnoreCase)
                ? ShellKind.PowerShell
                : ShellKind.Posix;

        ProcessConfiguration source = ProcessConfigurationDerivation.Derive(
            context.Configuration,
            b => b.SetOutputRedirection(context.Mode != InvocationMode.Raw));

        // Shell switches are caller-owned: each kind needs its own execution switch to
        // make the composed inner command run rather than being ignored.
        string runnerArgs = kind switch
        {
            ShellKind.Cmd => "/c",
            ShellKind.PowerShell => "-Command",
            _ => "-c"
        };

        ProcessConfiguration rewritten = ShellRewriter.Rewrite(
            source,
            shellTargetPath: shell.TargetFilePath.FullName,
            runnerArgs: runnerArgs,
            kind: kind,
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