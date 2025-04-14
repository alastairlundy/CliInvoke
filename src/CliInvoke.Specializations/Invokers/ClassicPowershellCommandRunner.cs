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
// ReSharper disable RedundantExtendsListEntry

namespace AlastairLundy.CliInvoke.Specializations.Invokers;

/// <summary>
/// Run commands through Windows Powershell with ease.
/// </summary>
public class ClassicPowershellCommandInvoker : SpecializedCliCommandInvoker, ISpecializedCliCommandInvoker
{
    
    /// <summary>
    /// Instantiates the Classic Powershell Cli command invoker.
    /// </summary>
    /// <remarks>Only supported on Windows based operating systems.</remarks>
    /// <param name="commandInvoker">The cli command invoker service to be used to run commands.</param>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("freebsd")]
#endif
    public ClassicPowershellCommandInvoker(ICliCommandInvoker commandInvoker) : base(commandInvoker,
        new ClassicPowershellCommandConfiguration())
    {
        
    }
}