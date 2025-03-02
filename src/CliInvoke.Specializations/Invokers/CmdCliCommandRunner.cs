﻿/*
    CliInvoke Specializations
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Runtime.Versioning;
using AlastairLundy.CliInvoke.Abstractions;
using AlastairLundy.CliInvoke.Extensibility.Abstractions.Runners;

using AlastairLundy.CliInvoke.Specializations.Configurations;
using AlastairLundy.CliInvoke.Specializations.Internal.Localizations;

namespace AlastairLundy.CliInvoke.Specializations.Invokers;

/// <summary>
/// Run commands through CMD with ease.
/// </summary>
public class CmdCliCommandInvoker : SpecializedCliCommandInvoker
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>Only supported on Windows based operating systems.</remarks>
    /// <param name="commandInvoker"></param>
    #if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("freebsd")]
    #endif
    public CmdCliCommandInvoker(ICliCommandInvoker commandInvoker) : base(commandInvoker, new CmdCommandConfiguration())
    {

    }
}