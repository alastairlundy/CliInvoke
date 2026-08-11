/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke;
using CliInvoke.Core.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CliInvoke.Specializations.Middleware;

/// <summary>
///     Extension methods for adding platform shell middleware to a <see cref="ProcessInvoker"/>.
/// </summary>
public static class PlatformMiddlewareExtensions
{
    /// <summary>
    ///     Creates a new <see cref="ProcessInvoker"/> with <see cref="PowerShellMiddleware"/>
    ///     prepended so that every invocation executes inside <c>pwsh</c>.
    /// </summary>
    /// <param name="invoker">The existing process invoker.</param>
    /// <returns>A new process invoker with PowerShell wrapping middleware applied.</returns>
    public static ProcessInvoker UsePowerShell(this ProcessInvoker invoker)
    {
        ArgumentNullException.ThrowIfNull(invoker);

        IEnumerable<IProcessMiddleware> newList = invoker.Middlewares.Prepend(new PowerShellMiddleware());
        return new ProcessInvoker(invoker.ExternalProcessFactory, newList);
    }

    /// <summary>
    ///     Creates a new <see cref="ProcessInvoker"/> with <see cref="CmdMiddleware"/>
    ///     prepended so that every invocation executes inside <c>cmd.exe</c>.
    /// </summary>
    /// <param name="invoker">The existing process invoker.</param>
    /// <returns>A new process invoker with cmd.exe wrapping middleware applied.</returns>
    public static ProcessInvoker UseCmd(this ProcessInvoker invoker)
    {
        ArgumentNullException.ThrowIfNull(invoker);

        IEnumerable<IProcessMiddleware> newList = invoker.Middlewares.Prepend(new CmdMiddleware());
        return new ProcessInvoker(invoker.ExternalProcessFactory, newList);
    }
}
