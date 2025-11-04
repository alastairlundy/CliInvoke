/*
    CliInvoke Specializations
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Specializations.Internal.Localizations;
#if NETSTANDARD2_0
using OperatingSystem = Polyfills.OperatingSystemPolyfill;
#endif

namespace AlastairLundy.CliInvoke.Specializations.Configurations;

/// <summary>
/// A Command configuration to make running commands through Windows PowerShell easier.
/// </summary>
[SupportedOSPlatform("windows")]
[UnsupportedOSPlatform("macos")]
[UnsupportedOSPlatform("maccatalyst")]
[UnsupportedOSPlatform("linux")]
[UnsupportedOSPlatform("freebsd")]
[UnsupportedOSPlatform("android")]
public class ClassicPowershellProcessConfiguration : ProcessConfiguration
{
    /// <summary>
    /// Initializes a new instance of the ClassicPowershellCommandConfiguration class.
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
    /// <param name="redirectStandardInput"></param>
    /// <param name="redirectStandardOutput"></param>
    /// <param name="redirectStandardError"></param>
    public ClassicPowershellProcessConfiguration(
        string arguments,
        bool redirectStandardInput,
        bool redirectStandardOutput,
        bool redirectStandardError,
        string workingDirectoryPath = null,
        bool requiresAdministrator = false,
        Dictionary<string, string> environmentVariables = null,
        UserCredential credentials = null,
        StreamWriter standardInput = null,
        StreamReader standardOutput = null,
        StreamReader standardError = null,
        Encoding standardInputEncoding = default,
        Encoding standardOutputEncoding = default,
        Encoding standardErrorEncoding = default,
        ProcessResourcePolicy processResourcePolicy = null,
        bool useShellExecution = false,
        bool windowCreation = false
    )
        : base(
            "",
            redirectStandardInput,
            redirectStandardOutput,
            redirectStandardError,
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
            windowCreation: windowCreation,
            useShellExecution: useShellExecution
        )
    {
        base.TargetFilePath = TargetFilePath;
    }

    /// <summary>
    /// The target file path of Windows PowerShell.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Thrown if not run on a Windows-based operating system.</exception>
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("android")]
    // ReSharper disable once MemberCanBePrivate.Global
    public new string TargetFilePath
    {
        get
        {
            if (OperatingSystem.IsWindows() == false)
            {
                throw new PlatformNotSupportedException(
                    Resources.Exceptions_ClassicPowershell_OnlySupportedOnWindows
                );
            }

            return $"{Environment.SystemDirectory}{Path.DirectorySeparatorChar}"
                + $"System32{Path.DirectorySeparatorChar}WindowsPowerShell{Path.DirectorySeparatorChar}v1.0{Path.DirectorySeparatorChar}powershell.exe";
        }
    }
}
