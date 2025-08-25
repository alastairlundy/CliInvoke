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
    /// <param name="configuration">The process configuration to use.</param>
    /// <param name="process">The process to apply the configuration to.</param>
    /// <param name="redirectStandardOutput">Whether to redirect the Standard Output.</param>
    /// <param name="redirectStandardError">Whether to redirect the Standard Error.</param>
    /// <exception cref="ArgumentException">Thrown if the process configuration's Target File Path is null or empty.</exception>
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
    public static void ApplyProcessConfiguration(this Process process,
        ProcessConfiguration configuration, bool redirectStandardOutput, bool redirectStandardError)
    {
        if (process.HasStarted())
        {
            process.SetResourcePolicy(configuration.ResourcePolicy);
            return;
        }
        
        ApplyProcessConfiguration(process.StartInfo, configuration, redirectStandardOutput, redirectStandardError);
    }

    /// <summary>
    /// Applies Process Configuration information to a ProcessStartInfo based on specified parameters and Process configuration object values.
    /// </summary>
    /// <param name="processStartInfo">The process start info to apply the configuration to.</param>
    /// <param name="configuration">The process configuration to use.</param>
    /// <param name="redirectStandardOutput">Whether to redirect the Standard Output.</param>
    /// <param name="redirectStandardError">Whether to redirect the Standard Error.</param>
    /// <exception cref="ArgumentException">Thrown if the process configuration's Target File Path is null or empty.</exception>
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
    public static void ApplyProcessConfiguration(this ProcessStartInfo processStartInfo,
        ProcessConfiguration configuration,  bool redirectStandardOutput, bool redirectStandardError)
    {
            if (string.IsNullOrEmpty(configuration.TargetFilePath))
                throw new ArgumentException(Resources.Exceptions_ProcessConfiguration_TargetFilePath_Empty);
            
            processStartInfo.FileName = configuration.TargetFilePath;
            processStartInfo.WorkingDirectory = configuration.WorkingDirectoryPath;
            processStartInfo.UseShellExecute = configuration.UseShellExecution;
            processStartInfo.CreateNoWindow = configuration.WindowCreation;
            processStartInfo.RedirectStandardInput = configuration.StandardInput is not null && configuration.StandardInput != StreamWriter.Null;
            processStartInfo.RedirectStandardOutput = redirectStandardOutput;
            processStartInfo.RedirectStandardError = redirectStandardError;

            if (string.IsNullOrEmpty(configuration.Arguments) == false)
            {
                processStartInfo.Arguments = configuration.Arguments;
            }

            if (configuration.RequiresAdministrator)
            {
                processStartInfo.RunAsAdministrator();
            }

            if (configuration.Credential is not null)
            {
                processStartInfo.TryApplyUserCredential(configuration.Credential);
            }

            if (configuration.EnvironmentVariables.Any())
            {
                processStartInfo.ApplyEnvironmentVariables(configuration.EnvironmentVariables);
            }

            if (processStartInfo.RedirectStandardInput)
            {
                processStartInfo.StandardInputEncoding = configuration.StandardInputEncoding;
            }
    }
}