/*
    CliInvoke 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

// ReSharper disable RedundantBoolCompare

#nullable enable

#if NETSTANDARD2_0 || NETSTANDARD2_1
using OperatingSystem = Polyfills.OperatingSystemPolyfill;
#else
using System.Runtime.Versioning;
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using AlastairLundy.CliInvoke.Abstractions;
using AlastairLundy.CliInvoke.Core.Extensions.StartInfos;
using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Internal;
using AlastairLundy.CliInvoke.Internal.Localizations;

namespace AlastairLundy.CliInvoke;

/// <summary>
/// A class to enable easy Process Creation with Command Configuration information.
/// </summary>
[Obsolete(DeprecationMessages.ClassDeprecationV2)]
public class CommandProcessFactory : ICommandProcessFactory
{
    /// <summary>
    /// Creates a process with the specified process start information.
    /// </summary>
    /// <param name="processStartInfo">The process start information to be used to configure the process to be created.</param>
    /// <returns>The newly created Process with the specified start information.</returns>
    /// <exception cref="ArgumentException">Thrown if the Process Start Info's File Name is null or empty.</exception>
#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("ios")]
        [SupportedOSPlatform("android")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("watchos")]
        [UnsupportedOSPlatform("browser")]
#endif
        [Obsolete(DeprecationMessages.ClassDeprecationV2)]
        public Process CreateProcess(ProcessStartInfo processStartInfo)
        {
            if (string.IsNullOrEmpty(processStartInfo.FileName))
            {
                throw new ArgumentException(Resources.Process_FileName_Empty);
            }

            Process output = new Process
            {
                StartInfo = processStartInfo,
            };
            
            return output;
        }

        /// <summary>
        /// Creates Process Start Information based on specified Command configuration object values.
        /// </summary>
        /// <param name="commandConfiguration">The command configuration object to specify Process info.</param>
        /// <returns>A new ProcessStartInfo object configured with the specified Command object values.</returns>
        /// <exception cref="ArgumentException">Thrown if the command configuration's Target File Path is null or empty.</exception>
    #if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("ios")]
        [SupportedOSPlatform("android")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("watchos")]
        [UnsupportedOSPlatform("browser")]
    #endif
        public ProcessStartInfo ConfigureProcess(CliCommandConfiguration commandConfiguration)
        {
            return ConfigureProcess(commandConfiguration, commandConfiguration.StandardOutput is not null,
                commandConfiguration.StandardError is not null);
        }

        /// <summary>
        /// Creates Process Start Information based on specified parameters and Command configuration object values.
        /// </summary>
        /// <param name="commandConfiguration">The command configuration object to specify Process info.</param>
        /// <param name="redirectStandardOutput">Whether to redirect the Standard Output.</param>
        /// <param name="redirectStandardError">Whether to redirect the Standard Error.</param>
        /// <returns>A new ProcessStartInfo object configured with the specified parameters and Command object values.</returns>
        /// <exception cref="ArgumentException">Thrown if the command configuration's Target File Path is null or empty.</exception>
#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("ios")]
        [SupportedOSPlatform("android")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("watchos")]
        [UnsupportedOSPlatform("browser")]
#endif
        public ProcessStartInfo ConfigureProcess(CliCommandConfiguration commandConfiguration, bool redirectStandardOutput,
            bool redirectStandardError)
        {
            if (string.IsNullOrEmpty(commandConfiguration.TargetFilePath))
            {
                throw new ArgumentException(Resources.Command_TargetFilePath_Empty);
            }
            
            ProcessStartInfo output = new ProcessStartInfo()
            {
                FileName = commandConfiguration.TargetFilePath,
                WorkingDirectory = commandConfiguration.WorkingDirectoryPath ?? Directory.GetCurrentDirectory(),
                UseShellExecute = commandConfiguration.UseShellExecution,
                CreateNoWindow = commandConfiguration.WindowCreation,
                RedirectStandardInput = commandConfiguration.StandardInput != StreamWriter.Null && commandConfiguration.StandardInput != StreamWriter.Null,
                RedirectStandardOutput = redirectStandardOutput || commandConfiguration.StandardOutput != StreamReader.Null,
                RedirectStandardError = redirectStandardError || commandConfiguration.StandardError != StreamReader.Null,
            };

            if (string.IsNullOrEmpty(commandConfiguration.Arguments) == false)
            {
                output.Arguments = commandConfiguration.Arguments;
            }
            
            if (commandConfiguration.RequiresAdministrator == true)
            {
                if (OperatingSystem.IsWindows())
                {
                    output.Verb = "runas";
                }
                else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
                {
                    output.Verb = "sudo";
                }
            }

            if (commandConfiguration.Credential is not null)
            {
                if (OperatingSystem.IsWindows())
                {
                   output.ApplyUserCredential(commandConfiguration.Credential);
                }
            }

            if (commandConfiguration.EnvironmentVariables is not null)
            {
                foreach (KeyValuePair<string, string> variable in commandConfiguration.EnvironmentVariables)
                {
                    if (variable.Value is not null)
                    {
                        output.Environment[variable.Key] = variable.Value;
                    }
                }
            }
            
            if (output.RedirectStandardInput == true)
            {
#if NETSTANDARD2_1 || NET5_0_OR_GREATER
                output.StandardInputEncoding = commandConfiguration.StandardInputEncoding ?? Encoding.Default;
#endif
            }
            
            output.StandardOutputEncoding = commandConfiguration.StandardOutputEncoding ?? Encoding.Default;
            output.StandardErrorEncoding = commandConfiguration.StandardErrorEncoding ?? Encoding.Default;
            
            return output;
        }
}