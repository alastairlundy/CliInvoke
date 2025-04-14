/*
    CliInvoke Specializations
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using AlastairLundy.CliInvoke.Abstractions;
using AlastairLundy.CliInvoke.Extensibility.Abstractions.Invokers;
using AlastairLundy.CliInvoke.Specializations.Configurations;

#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace AlastairLundy.CliInvoke.Specializations.Invokers;

/// <summary>
/// Run commands through cross-platform modern Powershell with ease.
/// </summary>
public class PowershellCliCommandInvoker : SpecializedCliCommandInvoker, ISpecializedCliCommandInvoker
{
    /// <summary>
    /// Instantiates the cross-platform Powershell Cli Command Invoker
    /// </summary>
    /// <param name="commandInvoker">The cli command invoker service to be used to run commands.</param>
#if NET5_0_OR_GREATER
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
#endif
    public PowershellCliCommandInvoker(ICliCommandInvoker commandInvoker) : base(commandInvoker,
        new PowershellCommandConfiguration(commandInvoker))
    {
        
    }
}