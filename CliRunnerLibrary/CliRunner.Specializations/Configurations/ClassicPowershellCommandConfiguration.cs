﻿/*
    CliRunner Specializations
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using CliRunner.Extensibility;
using CliRunner.Specializations.Internal.Localizations;

#if NETSTANDARD2_0 || NETSTANDARD2_1
using OperatingSystem = AlastairLundy.OSCompatibilityLib.Polyfills.OperatingSystem;
#else
using System.Runtime.Versioning;
#endif

namespace CliRunner.Specializations.Configurations
{
    /// <summary>
    /// A class to make running commands through Windows Powershell easier.
    /// </summary>
#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
        [UnsupportedOSPlatform("macos")]
        [UnsupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("linux")]
        [UnsupportedOSPlatform("freebsd")]
        [UnsupportedOSPlatform("android")]
#endif
    public class ClassicPowershellCommandConfiguration : SpecializedCommandConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the ClassicPowershellCommandConfiguration class.
        /// </summary>
        /// <param name="arguments">The arguments to be passed to the command.</param>
        /// <param name="workingDirectoryPath">The working directory for the command.</param>
        /// <param name="requiresAdministrator">Indicates whether the command requires administrator privileges.</param>
        /// <param name="environmentVariables">A dictionary of environment variables to be set for the command.</param>
        /// <param name="credentials">The user credentials to be used when running the command.</param>
        /// <param name="resultValidation">The validation criteria for the command result.</param>
        /// <param name="standardInput">The stream for the standard input.</param>
        /// <param name="standardOutput">The stream for the standard output.</param>
        /// <param name="standardError">The stream for the standard error.</param>
        /// <param name="standardInputEncoding">The encoding for the standard input stream.</param>
        /// <param name="standardOutputEncoding">The encoding for the standard output stream.</param>
        /// <param name="standardErrorEncoding">The encoding for the standard error stream.</param>
        /// <param name="processorAffinity">The processor affinity for the command.</param>
        /// <param name="useShellExecution">Indicates whether to use the shell to execute the command.</param>
        /// <param name="windowCreation">Indicates whether to create a new window for the command.</param>
        public ClassicPowershellCommandConfiguration(string arguments = null,
            string workingDirectoryPath = null, bool requiresAdministrator = false,
            IReadOnlyDictionary<string, string> environmentVariables = null, UserCredential credentials = null,
            CommandResultValidation resultValidation = CommandResultValidation.ExitCodeZero,
            StreamWriter standardInput = null, StreamReader standardOutput = null, StreamReader standardError = null,
            Encoding standardInputEncoding = default, Encoding standardOutputEncoding = default,
            Encoding standardErrorEncoding = default, IntPtr processorAffinity = default(IntPtr),
            bool useShellExecution = false, bool windowCreation = false) : base("", arguments,
            workingDirectoryPath,
            requiresAdministrator, environmentVariables, credentials, resultValidation, standardInput, standardOutput,
            standardError, standardInputEncoding, standardOutputEncoding, standardErrorEncoding, processorAffinity,
            useShellExecution, windowCreation)
        {
            
        }
        
        /// <summary>
        /// The target file path of Windows Powershell.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">Thrown if not run on a Windows based operating system.</exception>
#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
        [UnsupportedOSPlatform("macos")]
        [UnsupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("linux")]
        [UnsupportedOSPlatform("freebsd")]
        [UnsupportedOSPlatform("android")]
#endif
        // ReSharper disable once MemberCanBePrivate.Global
        public new string TargetFilePath
        {
            get
            {
                if (OperatingSystem.IsWindows() == false)
                {
                    throw new PlatformNotSupportedException(Resources.Exceptions_ClassicPowershell_OnlySupportedOnWindows);
                }

                return $"{Environment.SystemDirectory}{Path.DirectorySeparatorChar}" +
                                  $"System32{Path.DirectorySeparatorChar}WindowsPowerShell{Path.DirectorySeparatorChar}v1.0{Path.DirectorySeparatorChar}powershell.exe";
            }
        }
        
    }
}