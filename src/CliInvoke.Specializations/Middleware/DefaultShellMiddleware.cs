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
///     Middleware that resolves the system default shell at invocation time (via
///     <see cref="IShellDetector"/>) and rewrites the <see cref="InvocationContext.Configuration"/>
///     to execute the original command inside it. PowerShell targets are wrapped with the shared
///     <see cref="PowerShellMiddleware.PowerShellRunnerArgs"/> switch set,
///     cmd with <c>/c</c>, and any other detected shell with <c>-c</c>.
/// </summary>
/// <remarks>
///     <para>
///         Window creation and shell-execution behaviour come from the
///         <see cref="ShellMiddlewareOptions"/> instance registered in the dependency injection
///         container (or <see cref="ShellMiddlewareOptions.Default"/> when none is registered).
///     </para>
///     <para>
///         The source configuration's <see cref="ProcessConfiguration.OutputRedirection"/> flag is
///         overwritten on every invocation to match the invocation mode: forced off in
///         <see cref="InvocationMode.Raw"/>, forced on otherwise. This protective overwrite prevents
///         a raw-mode pipe deadlock. Raw mode waits for process exit without draining redirected
///         output, so a redirected pipe nobody reads would fill up and block the child process.
///     </para>
///     <para>
///         On iOS, tvOS, browser, and watchOS, invocation throws
///         <see cref="PlatformNotSupportedException"/> at runtime.
///     </para>
/// </remarks>
[UnsupportedOSPlatform("browser")]
[UnsupportedOSPlatform("ios")]
[UnsupportedOSPlatform("tvos")]
[UnsupportedOSPlatform("watchos")]
internal sealed class DefaultShellMiddleware : IProcessMiddleware
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ShellMiddlewareOptions _options;

    // Async-flow re-entrancy guard: shell detection runs its own probe invocations
    // through the same DI invoker, whose chain contains this middleware. Without the
    // guard each probe re-enters detection and recurses infinitely.
    private static readonly AsyncLocal<bool> DetectionInProgress = new();

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

        if (DetectionInProgress.Value)
        {
            // Re-entrant call: an outer shell-detection probe is running through this
            // chain again. Pass through without re-detecting (a probe must not be
            // shell-wrapped anyway) to break the recursion cycle.
            await next(context).ConfigureAwait(false);
            return;
        }

        DetectionInProgress.Value = true;
        try
        {
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
            // make the composed inner command run rather than being ignored. PowerShell uses
            // the shared safe switch set so wrapping here matches PowerShellMiddleware.
            string runnerArgs = kind switch
            {
                ShellKind.Cmd => "/c",
                ShellKind.PowerShell => PowerShellMiddleware.PowerShellRunnerArgs,
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
        finally
        {
            DetectionInProgress.Value = false;
        }
    }
    
    private static void ThrowIfUnsupported()
    {
        if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() ||
            OperatingSystem.IsBrowser() || OperatingSystem.IsWatchOS())
        {
            throw new PlatformNotSupportedException(Resources
                .Exceptions_Shell_OnlySupportedOnDesktop);
        }
    }
}