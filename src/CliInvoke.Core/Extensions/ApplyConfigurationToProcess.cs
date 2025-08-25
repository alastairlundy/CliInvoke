/*
    AlastairLundy.CliInvoke.Core 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */


using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

using AlastairLundy.CliInvoke.Core.Internal;
using AlastairLundy.CliInvoke.Core.Primitives;

using AlastairLundy.DotExtensions.Processes;

namespace AlastairLundy.CliInvoke.Core;

public static class ApplyConfigurationToProcess
{
    /// <summary>
    /// Applies Process Configuration information to a Process based on specified parameters and Process configuration object values.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="process"></param>
    /// <param name="redirectStandardOutput">Whether to redirect the Standard Output.</param>
    /// <param name="redirectStandardError">Whether to redirect the Standard Error.</param>
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
    public static void ApplyProcessConfiguration(this Process process,
        ProcessConfiguration configuration, bool redirectStandardOutput, bool redirectStandardError)
    {
        if (process.HasStarted() == false && process.HasExited() == false)
        {
            if (string.IsNullOrEmpty(configuration.TargetFilePath))
            {
                throw new ArgumentException(Resources.Exceptions_ProcessConfiguration_TargetFilePath_Empty);
            }

            ProcessStartInfo output = new ProcessStartInfo()
            {
                FileName = configuration.TargetFilePath,
                WorkingDirectory = configuration.WorkingDirectoryPath,
                UseShellExecute = configuration.UseShellExecution,
                CreateNoWindow = configuration.WindowCreation,
                RedirectStandardInput = configuration.StandardInput is not null &&
                                        configuration.StandardInput != StreamWriter.Null,
                RedirectStandardOutput = redirectStandardOutput,
                RedirectStandardError = redirectStandardError,
            };

            if (string.IsNullOrEmpty(configuration.Arguments) == false)
            {
                output.Arguments = configuration.Arguments;
            }

            if (configuration.RequiresAdministrator)
            {
                output.RunAsAdministrator();
            }

            if (configuration.Credential is not null)
            {
                output.TryApplyUserCredential(configuration.Credential);
            }

            if (configuration.EnvironmentVariables.Any())
            {
                output.ApplyEnvironmentVariables(configuration.EnvironmentVariables);
            }

            if (output.RedirectStandardInput)
            {
#if NETSTANDARD2_1 || NET5_0_OR_GREATER
                output.StandardInputEncoding = configuration.StandardInputEncoding;
#endif
            }

            process.StartInfo = output;
        }
        else
        {
            process.SetResourcePolicy(configuration.ResourcePolicy);
        }
    }
}