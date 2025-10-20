﻿/*
    CliInvoke Specializations
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.Versioning;

using AlastairLundy.CliInvoke.Specializations.Internal.Localizations;

using AlastairLundy.CliInvoke.Core;

// ReSharper disable UnusedMember.Global

namespace AlastairLundy.CliInvoke.Specializations.Configurations;

/// <summary>
/// A Command configuration to make running commands through Windows CMD easier.
/// </summary>
[SupportedOSPlatform("windows")]
[UnsupportedOSPlatform("macos")]
[UnsupportedOSPlatform("linux")]
[UnsupportedOSPlatform("browser")]
[UnsupportedOSPlatform("ios")]
[UnsupportedOSPlatform("android")]
[UnsupportedOSPlatform("browser")]
[UnsupportedOSPlatform("tvos")]
[UnsupportedOSPlatform("watchos")]
public class CmdProcessConfiguration : ProcessConfiguration
{
    /// <summary>
    /// Initializes a new instance of the CmdCommandConfiguration class.
    /// </summary>
    /// <param name="arguments">The arguments to be passed to the command.</param>
    /// <param name="workingDirectoryPath">The working directory for the command.</param>
    /// <param name="requiresAdministrator">Indicates whether the command requires administrator privileges.</param>
    /// <param name="environmentVariables">A dictionary of environment variables to be set for the command.</param>
    /// <param name="credentials">The user credentials to be used when running the command.</param>
    /// <param name="standardInput">The stream for the standard input.</param>
    /// <param name="standardOutput">The stream for the standard output.</param>
    /// <param name="standardError">The stream for the standard error.</param>
    /// <param name="standardInputEncoding">The encoding for the standard input stream.</param>
    /// <param name="standardOutputEncoding">The encoding for the standard output stream.</param>
    /// <param name="standardErrorEncoding">The encoding for the standard error stream.</param>
    /// <param name="processResourcePolicy">The process resource policy for the command.</param>
    /// <param name="useShellExecution">Indicates whether to use the shell to execute the command.</param>
    /// <param name="windowCreation">Indicates whether to create a new window for the command.</param>
    public CmdProcessConfiguration(string arguments = null,
        string workingDirectoryPath = null, bool requiresAdministrator = false,
        Dictionary<string, string> environmentVariables = null, UserCredential credentials = null,
        StreamWriter standardInput = null, StreamReader standardOutput = null, StreamReader standardError = null,
        Encoding standardInputEncoding = default, Encoding standardOutputEncoding = default,
        Encoding standardErrorEncoding = default, ProcessResourcePolicy processResourcePolicy = null,
        bool useShellExecution = false, bool windowCreation = false) : 
        base("",
            false,
            true,
            true,
            arguments,
            workingDirectoryPath,
            requiresAdministrator,
            environmentVariables,
            credentials,
            standardInput,
            standardOutput,
            standardError,
            standardInputEncoding,
            standardOutputEncoding,
            standardErrorEncoding,
            processResourcePolicy,
            windowCreation: useShellExecution,
            useShellExecution: windowCreation)
    {
        base.TargetFilePath = this.TargetFilePath;
    }


    /// <summary>
    /// The target file path of Cmd.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Thrown if not run on a Windows-based operating system.</exception>
    [SupportedOSPlatform("windows")]
    public new string TargetFilePath
    {
        get
        {
            if (OperatingSystem.IsWindows() == false)
            {
                throw new PlatformNotSupportedException(Resources.Exceptions_Cmd_OnlySupportedOnWindows);
            }

            return Environment.SystemDirectory + Path.DirectorySeparatorChar + "cmd.exe";
        }
    }
}