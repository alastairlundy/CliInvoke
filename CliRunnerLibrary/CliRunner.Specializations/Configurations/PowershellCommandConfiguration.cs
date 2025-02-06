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
using System.Threading.Tasks;

using CliRunner.Abstractions;
using CliRunner.Builders;
using CliRunner.Extensibility;

#if NETSTANDARD2_0 || NETSTANDARD2_1
using OperatingSystem = Polyfills.OperatingSystemPolyfill;
#else
using System.Runtime.Versioning;
#endif

// ReSharper disable RedundantBoolCompare

namespace CliRunner.Specializations.Configurations
{
    /// <summary>
    /// A class to make running commands through cross-platform Powershell easier.
    /// </summary>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("watchos")]
#endif
    public class PowershellCommandConfiguration : SpecializedCommandConfiguration
    {
        private readonly ICommandRunner _commandRunner;

        /// <summary>
        /// Initializes a new instance of the PowershellCommandConfiguration class.
        /// </summary>
        /// <param name="commandRunner">The command runner to be used to get the Target File Path.</param>
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
        public PowershellCommandConfiguration(ICommandRunner commandRunner, string arguments = null,
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
            _commandRunner = commandRunner;
        }
        
        /// <summary>
        /// The target file path of cross-platform Powershell.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">Thrown if run on an operating system besides Windows, macOS, Linux, and FreeBSD.</exception>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("watchos")]
#endif
        public new string TargetFilePath
        {
            get
            {
                string filePath = string.Empty;
                
                if (OperatingSystem.IsWindows())
                {
                    filePath = $"{GetWindowsInstallLocation()}{Path.DirectorySeparatorChar}pwsh.exe";
                }
                else if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
                {
                    filePath = GetUnixInstallLocation();
                }

                return filePath;
            }
        }

        private string GetWindowsInstallLocation()
        {
            string programFiles = Environment.GetFolderPath(Environment.Is64BitOperatingSystem == true ?
                Environment.SpecialFolder.ProgramFiles : Environment.SpecialFolder.ProgramFilesX86);

            string[] directories = Directory.GetDirectories(
                $"{programFiles}{Path.DirectorySeparatorChar}Powershell");

            foreach (string directory in directories)
            {
                if (File.Exists($"{directory}{Path.DirectorySeparatorChar}pwsh.exe"))
                {
                    return directory;
                }
            }
            
            throw new FileNotFoundException("Could not find Powershell installation.");
        }

        private string GetUnixInstallLocation()
        {
           ICommandBuilder installLocationBuilder = new CommandBuilder("/usr/bin/which")
                .WithArguments("pwsh");
           
           Command command = installLocationBuilder.ToCommand();
           
          Task<BufferedCommandResult> task = _commandRunner.ExecuteBufferedAsync(command);
          
          task.RunSynchronously();
          
          Task.WaitAll(task);
          
          return task.Result.StandardOutput;
        }
    }
}