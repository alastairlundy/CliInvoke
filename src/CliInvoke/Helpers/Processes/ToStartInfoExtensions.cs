/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Diagnostics;
using System.Linq;

using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Internal.Localizations;

namespace AlastairLundy.CliInvoke.Helpers.Processes;

internal static class ToStartInfoExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="processConfiguration"></param>
    /// <param name="redirectStandardOutput"></param>
    /// <param name="redirectStandardError"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal static ProcessStartInfo ToProcessStartInfo(this ProcessConfiguration processConfiguration,
        bool redirectStandardOutput, bool redirectStandardError)
    {
        ProcessStartInfo processStartInfo = new ProcessStartInfo()
        {
            FileName = processConfiguration.TargetFilePath,
            Arguments = string.IsNullOrEmpty(processConfiguration.Arguments) ? string.Empty : processConfiguration.Arguments,
            WorkingDirectory = processConfiguration.WorkingDirectoryPath,
            UseShellExecute = processConfiguration.UseShellExecution,
            CreateNoWindow = processConfiguration.WindowCreation == false,
            RedirectStandardInput = processConfiguration.StandardInput is not null &&
                                    processConfiguration.RedirectStandardInput,
            RedirectStandardOutput = redirectStandardOutput,
            RedirectStandardError = redirectStandardError
        };
        
        if (string.IsNullOrEmpty(processConfiguration.TargetFilePath))
            throw new ArgumentException(Resources.Exceptions_TargetFile_NullOrEmpty);
        
        if (processConfiguration.RequiresAdministrator) 
            processStartInfo.RunAsAdministrator();

#pragma warning disable CA1416
        processStartInfo.SetUserCredential(processConfiguration.Credential);
#pragma warning restore CA1416
                
        if (processConfiguration.EnvironmentVariables.Any()) 
            processStartInfo.SetEnvironmentVariables(processConfiguration.EnvironmentVariables);

#if NET8_0_OR_GREATER
        if (processStartInfo.RedirectStandardInput) 
            processStartInfo.StandardInputEncoding = processConfiguration.StandardInputEncoding;
#endif
        
        if (processStartInfo.RedirectStandardOutput) 
            processStartInfo.StandardOutputEncoding = processConfiguration.StandardOutputEncoding;
        
        if (processStartInfo.RedirectStandardError) 
            processStartInfo.StandardErrorEncoding = processConfiguration.StandardErrorEncoding;

        
        return processStartInfo;
    }
}