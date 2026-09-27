/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Specializations.Middleware;

/// <summary>
///     Extension methods for adding platform shell middleware to a <see cref="IProcessMiddlewareBuilder"/>.
/// </summary>
public static class PlatformMiddlewareExtensions
{
    /// <param name="builder">The middleware builder.</param>
    extension(IProcessMiddlewareBuilder builder)
    {
        /// <summary>
        ///     Adds <see cref="PowerShellMiddleware"/> to the process invocation pipeline.
        /// </summary>
        /// <remarks>
        ///     Register a <see cref="ShellMiddlewareOptions"/> instance in the dependency
        ///     injection container to customise window-creation and shell-execution behaviour.
        ///     When no instance is registered, <see cref="ShellMiddlewareOptions.Default"/> is used.
        /// </remarks>
        /// <returns>The builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="builder"/> is <c>null</c>.
        /// </exception>
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
        public IProcessMiddlewareBuilder UsePowerShell()
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.UseMiddleware<PowerShellMiddleware>();

            return builder;
        }

        /// <summary>
        ///     Adds <see cref="CmdMiddleware"/> to the process invocation pipeline.
        /// </summary>
        /// <remarks>
        ///     <c>CmdMiddleware</c> honors the source <see cref="ProcessConfiguration"/>'s
        ///     <see cref="ProcessConfiguration.WindowCreation"/> and
        ///     <see cref="ProcessConfiguration.UseShellExecution"/> flags rather than
        ///     <see cref="ShellMiddlewareOptions"/>: registering that options type affects
        ///     <c>UsePowerShell()</c> and <c>UseDefaultShell()</c> only. Set the flags on the
        ///     per-invocation configuration to change cmd wrapping behaviour.
        /// </remarks>
        /// <returns>The builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        [SupportedOSPlatform("windows")]
        [UnsupportedOSPlatform("macos")]
        [UnsupportedOSPlatform("linux")]
        [UnsupportedOSPlatform("freebsd")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("watchos")]
        public IProcessMiddlewareBuilder UseCmd()
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.UseMiddleware<CmdMiddleware>();

            return builder;
        }

        /// <summary>
        ///     Adds <see cref="DefaultShellMiddleware"/> to the process invocation pipeline, wrapping
        ///     the original command in the shell detected as the system default (for example pwsh,
        ///     Windows PowerShell, cmd, or sh).
        /// </summary>
        /// <remarks>
        ///     Requires <see cref="IShellDetector"/> to be registered (via <c>AddCliInvoke</c>) and
        ///     <see cref="ShellMiddlewareOptions"/> (registered by <c>AddCliInvokeSpecializations</c>).
        ///     The detected shell determines how the original command is wrapped: cmd runs it via
        ///     <c>/c</c>, pwsh and Windows PowerShell via
        ///     <see cref="PowerShellMiddleware.PowerShellRunnerArgs"/> (equivalent to
        ///     <c>-NoProfile -NonInteractive -Command</c>, the same switch set
        ///     <c>UsePowerShell()</c> uses), and any other detected shell (for example sh or bash)
        ///     is wrapped as a POSIX shell invoked with <c>-c</c>.
        ///     <see cref="PlatformNotSupportedException"/> is thrown at invocation time only on
        ///     platforms where this middleware is unsupported (iOS, tvOS, browser, and watchOS).
        /// </remarks>
        /// <returns>The builder for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("watchos")]
        public IProcessMiddlewareBuilder UseDefaultShell()
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.UseMiddleware<DefaultShellMiddleware>();

            return builder;
        }
    }
}
