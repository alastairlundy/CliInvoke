﻿/*
    CliRunner 
    Copyright (C) 2024  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CliRunner.Commands;

namespace CliRunner.Extensibility
{
    public abstract class AbstractSpecializedCommand : Command
    {
        /// <summary>
        /// The target file path of the Command.
        /// </summary>
        public static new string TargetFilePath { get; protected set; }
        
        protected AbstractSpecializedCommand(string targetFilePath, string arguments = null, string workingDirectoryPath = null, bool runAsAdministrator = false, IReadOnlyDictionary<string, string> environmentVariables = null, UserCredentials credentials = null, CommandResultValidation commandResultValidation = CommandResultValidation.ExitCodeZero, StreamWriter standardInput = null, StreamReader standardOutput = null, StreamReader standardError = null, IntPtr processorAffinity = default(IntPtr), bool useShellExecute = false) : base(targetFilePath, arguments, workingDirectoryPath, runAsAdministrator, environmentVariables, credentials, commandResultValidation, standardInput, standardOutput, standardError, processorAffinity, useShellExecute)
        {
            TargetFilePath = targetFilePath;
        }
        
        public static Command Run() => new Command(TargetFilePath);
        
        public abstract Task<string> GetInstallLocationAsync();

        /// <summary>
        /// Detects whether the Command is installed on a system.
        /// </summary>
        /// <returns>True if the Command is installed; returns false otherwise.</returns>
        public async Task<bool> IsInstalledAsync()
        {
            string installLocation = await GetInstallLocationAsync();

            try
            {
                return Directory.Exists(installLocation) &&
                       Directory.GetFiles(installLocation).Contains(TargetFilePath);
            }
            catch
            {
                return false;
            }
        }

        public abstract Task<Version> GetInstalledVersionAsync();
    }
}