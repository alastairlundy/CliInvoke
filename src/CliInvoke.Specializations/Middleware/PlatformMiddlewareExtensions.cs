/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Middleware;

using System.Collections.Generic;
using System.Linq;

namespace CliInvoke.Specializations.Middleware;

/// <summary>
///     Extension methods for adding platform shell middleware to a <see cref="ProcessInvoker"/>.
/// </summary>
public static class PlatformMiddlewareExtensions
{
    /// <param name="invoker">The existing process invoker.</param>
    extension(ProcessInvoker invoker)
    {
        /// <summary>
        ///     Creates a new <see cref="ProcessInvoker"/> with <see cref="PowerShellMiddleware"/>
        ///     prepended so that every invocation executes inside <c>pwsh</c> / <c>pwsh.exe</c>
        ///     with the default options (<c>windowCreation = false</c>, <c>useShellExecution = false</c>).
        /// </summary>
        /// <remarks>
        ///     Equivalent to <c>UsePowerShell(windowCreation: false, useShellExecution: false)</c>.
        ///     This is the same behaviour <see cref="PowershellProcessInvoker"/> provides as a thin
        ///     wrapper. Use the overload when you need non-default window-creation or shell-execution
        ///     settings.
        /// </remarks>
        /// <returns>A new process invoker with PowerShell wrapping middleware applied.</returns>
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
        public ProcessInvoker UsePowerShell()
        {
            ArgumentNullException.ThrowIfNull(invoker);

            return invoker.UsePowerShell(new CliInvoke.FilePathResolver());
        }

        /// <summary>
        ///     Creates a new <see cref="ProcessInvoker"/> with <see cref="PowerShellMiddleware"/>
        ///     prepended and configured with the supplied window-creation and shell-execution flags,
        ///     so that every invocation executes inside <c>pwsh</c> / <c>pwsh.exe</c>.
        /// </summary>
        /// <param name="windowCreation">
        ///     Whether PowerShell should create a new window when launched. Defaults to <c>false</c>
        ///     to match <see cref="PowershellProcessInvoker"/>.
        /// </param>
        /// <param name="useShellExecution">
        ///     Whether to use shell execution semantics. Defaults to <c>false</c> to match
        ///     <see cref="PowershellProcessInvoker"/>.
        /// </param>
        /// <returns>A new process invoker with PowerShell wrapping middleware applied.</returns>
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
        public ProcessInvoker UsePowerShell(bool windowCreation,
            bool useShellExecution)
        {
            ArgumentNullException.ThrowIfNull(invoker);

            return invoker.UsePowerShell(new CliInvoke.FilePathResolver(), windowCreation, useShellExecution);
        }

        /// <summary>
        ///     Creates a new <see cref="ProcessInvoker"/> with a <see cref="PowerShellMiddleware"/>
        ///     prepended and configured with the supplied resolver, window-creation, and shell-execution
        ///     flags, so that every invocation executes inside <c>pwsh</c> / <c>pwsh.exe</c>.
        /// </summary>
        /// <param name="filePathResolver">
        ///     The resolver used to locate the <c>pwsh</c> / <c>pwsh.exe</c> executable.
        /// </param>
        /// <param name="windowCreation">
        ///     Whether PowerShell should create a new window when launched. Defaults to <c>false</c>
        ///     to match <see cref="PowershellProcessInvoker"/>.
        /// </param>
        /// <param name="useShellExecution">
        ///     Whether to use shell execution semantics. Defaults to <c>false</c> to match
        ///     <see cref="PowershellProcessInvoker"/>.
        /// </param>
        /// <returns>A new process invoker with PowerShell wrapping middleware applied.</returns>
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
        public ProcessInvoker UsePowerShell(IFilePathResolver filePathResolver,
            bool windowCreation = false,
            bool useShellExecution = false)
        {
            ArgumentNullException.ThrowIfNull(invoker);
            ArgumentNullException.ThrowIfNull(filePathResolver);

            IEnumerable<IProcessMiddleware> newList =
                invoker.Middlewares.Prepend(new PowerShellMiddleware(filePathResolver, windowCreation, useShellExecution));
            return new ProcessInvoker(invoker.ExternalProcessFactory, newList, invoker.SharedItems);
        }

        /// <summary>
        ///     Creates a new <see cref="ProcessInvoker"/> with <see cref="CmdMiddleware"/>
        ///     prepended so that every invocation executes inside <c>cmd.exe</c>.
        /// </summary>
        /// <returns>A new process invoker with cmd.exe wrapping middleware applied.</returns>
        [SupportedOSPlatform("windows")]
        [UnsupportedOSPlatform("macos")]
        [UnsupportedOSPlatform("linux")]
        [UnsupportedOSPlatform("freebsd")]
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("watchos")]
        public ProcessInvoker UseCmd()
        {
            ArgumentNullException.ThrowIfNull(invoker);

            IEnumerable<IProcessMiddleware> newList = invoker.Middlewares.Prepend(new CmdMiddleware());
            return new ProcessInvoker(invoker.ExternalProcessFactory, newList, invoker.SharedItems);
        }
    }
}
