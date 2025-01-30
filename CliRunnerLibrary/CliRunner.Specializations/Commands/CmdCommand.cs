﻿/*
    CliRunner Specializations
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CliRunner.Abstractions;
using CliRunner.Builders;
using CliRunner.Extensibility;

using CliRunner.Specializations.Internal.Localizations;
// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable RedundantBoolCompare

#if NETSTANDARD2_0 || NETSTANDARD2_1
using OperatingSystem = AlastairLundy.OSCompatibilityLib.Polyfills.OperatingSystem;
#else
using System.Runtime.Versioning;
#endif

namespace CliRunner.Specializations
{

    /// <summary>
    /// A class to make running commands through Windows CMD easier.
    /// </summary>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("watchos")]
#endif
    [Obsolete("This class is deprecated and will be removed in a future version.")]
    public class CmdCommand : AbstractInstallableCommand
    {
        private readonly ICommandRunner _commandRunner;

        private readonly Command _cmdVersionCommand;
        
        /// <summary>
        /// The target file path of Cmd.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">Thrown if not run on a Windows based operating system.</exception>
        public new string TargetFilePath
        {
            get
            {
                if (OperatingSystem.IsWindows() == false)
                {
                    throw new PlatformNotSupportedException(Resources.Exceptions_Cmd_OnlySupportedOnWindows);
                }
            
                if (IsInstalled() == false)
                {
                    throw new ArgumentException(Resources.Exceptions_Cmd_NotInstalled);
                }

                return $"{Environment.SystemDirectory}{Path.DirectorySeparatorChar}cmd.exe"; ;
            }
        }
        
        /// <summary>
        /// Sets up the CmdCommand class.
        /// </summary>
#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
        [UnsupportedOSPlatform("macos")]
        [UnsupportedOSPlatform("linux")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("watchos")]
#endif
        public CmdCommand() : base("")
        {
            base.TargetFilePath = TargetFilePath;
            _commandRunner = new CommandRunner(new CommandPipeHandler());

            ICommandBuilder commandBuilder = new CommandBuilder(this)
                .WithWorkingDirectory(Environment.SystemDirectory)
                .WithArguments("--version");

            _cmdVersionCommand = commandBuilder.ToCommand();
        }

        /// <summary>
        /// Sets up the CmdCommand class.
        /// </summary>
#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
        [UnsupportedOSPlatform("macos")]
        [UnsupportedOSPlatform("linux")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("watchos")]
#endif
        [Obsolete("This class is deprecated and will be removed in a future version.")]
        public CmdCommand(ICommandRunner commandRunner) : base("")
        {
            base.TargetFilePath = TargetFilePath;
            _commandRunner = commandRunner;
            
            ICommandBuilder commandBuilder = new CommandBuilder(this)
                .WithWorkingDirectory(Environment.SystemDirectory)
                .WithArguments("--version");

            _cmdVersionCommand = commandBuilder.ToCommand();
        }
        
        [Obsolete("This method is deprecated and will be removed in a future version.")]
        public override bool IsCurrentOperatingSystemSupported()
        {
            return OperatingSystem.IsWindows() == true;
        }

        /// <summary>
        /// Asynchronously returns the installed version of CMD if the current OS is Windows.
        /// </summary>
        /// <returns>The installed version of CMD if the current operating system is Windows based.</returns>
        /// <exception cref="PlatformNotSupportedException">Thrown if run on an operating system that isn't based on Windows.</exception>
#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
        [UnsupportedOSPlatform("macos")]
        [UnsupportedOSPlatform("linux")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("watchos")]
#endif
        [Obsolete("This method is deprecated and will be removed in a future version.")]

        public override async Task<Version> GetInstalledVersionAsync()
        {
            if (OperatingSystem.IsWindows() == false)
            {
                throw new PlatformNotSupportedException(Resources.Exceptions_Cmd_NotInstalled);
            }
            
            BufferedCommandResult result =  await _commandRunner.ExecuteBufferedAsync(_cmdVersionCommand);

#if NET5_0_OR_GREATER
            string output = result.StandardOutput.Split(Environment.NewLine).First()
                .Replace("Microsoft Windows [", string.Empty)
                .Replace("]", string.Empty)
                .Replace("Version",string.Empty)
                .Replace(" ", string.Empty);
#else
                string output = result.StandardOutput
                    .Replace("Microsoft Windows [", string.Empty)
                    .Replace("]", string.Empty)
                    .Replace("Version", string.Empty).Split(' ').First();
#endif
                   
            return Version.Parse(output);
        }
    }
}