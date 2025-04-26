/*
    CliInvoke.Core 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

using AlastairLundy.CliInvoke.Core.Internal;
using AlastairLundy.CliInvoke.Core.Primitives;

// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
// ReSharper disable MemberCanBePrivate.Global

namespace AlastairLundy.CliInvoke.Core.Extensions.StartInfos
{
    public static class ToProcessStartInfoExtensions
    {

        /// <summary>
        /// Creates Process Start Information based on specified Process configuration object values.
        /// </summary>
        /// <param name="processConfiguration">The process configuration object to specify Process info.</param>
        /// <returns>A new ProcessStartInfo object configured with the specified Process object values.</returns>
        /// <exception cref="ArgumentException">Thrown if the process configuration's Target File Path is null or empty.</exception>
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
        public static ProcessStartInfo ToProcessStartInfo(this ProcessConfiguration processConfiguration)
        {
            bool redirectStandardError = processConfiguration.StandardError is not null;
            bool redirectStandardOutput = processConfiguration.StandardOutput is not null;
                
            return ToProcessStartInfo(processConfiguration, redirectStandardOutput, redirectStandardError);
        }

        /// <summary>
        /// Creates Process Start Information based on specified parameters and Process configuration object values.
        /// </summary>
        /// <param name="processConfiguration">The process configuration object to specify Process info.</param>
        /// <param name="redirectStandardOutput">Whether to redirect the Standard Output.</param>
        /// <param name="redirectStandardError">Whether to redirect the Standard Error.</param>
        /// <returns>A new ProcessStartInfo object configured with the specified parameters and Process object values.</returns>
        /// <exception cref="ArgumentException">Thrown if the process configuration's Target File Path is null or empty.</exception>
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
        public static ProcessStartInfo ToProcessStartInfo(this ProcessConfiguration processConfiguration, bool redirectStandardOutput, bool redirectStandardError)
        {
            if (string.IsNullOrEmpty(processConfiguration.TargetFilePath))
            {
                throw new ArgumentException(Resources.Command_TargetFilePath_Empty);
            }
            
            ProcessStartInfo output = new ProcessStartInfo()
            {
                FileName = processConfiguration.TargetFilePath,
                WorkingDirectory = processConfiguration.WorkingDirectoryPath ?? Directory.GetCurrentDirectory(),
                UseShellExecute = processConfiguration.UseShellExecution,
                CreateNoWindow = processConfiguration.WindowCreation,
                RedirectStandardInput = processConfiguration.StandardInput != StreamWriter.Null && processConfiguration.StandardInput != StreamWriter.Null,
                RedirectStandardOutput = redirectStandardOutput || processConfiguration.StandardOutput != StreamReader.Null,
                RedirectStandardError = redirectStandardError || processConfiguration.StandardError != StreamReader.Null,
            };

            if (string.IsNullOrEmpty(processConfiguration.Arguments) == false)
            {
                output.Arguments = processConfiguration.Arguments;
            }
            
            if (processConfiguration.RequiresAdministrator)
            {
                output.RunAsAdministrator();
            }

            if (processConfiguration.Credential is not null)
            {
                output.TryApplyUserCredential(processConfiguration.Credential);
            }

            if (processConfiguration.EnvironmentVariables.Any())
            {
               output.ApplyEnvironmentVariables(processConfiguration.EnvironmentVariables);
            }
            
            if (output.RedirectStandardInput)
            {
#if NETSTANDARD2_1 || NET5_0_OR_GREATER
                output.StandardInputEncoding = processConfiguration.StandardInputEncoding ?? Encoding.Default;
#endif
            }
            
            output.StandardOutputEncoding = processConfiguration.StandardOutputEncoding ?? Encoding.Default;
            output.StandardErrorEncoding = processConfiguration.StandardErrorEncoding ?? Encoding.Default;
            
            return output;
        }
    }
}