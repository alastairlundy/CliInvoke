/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Helpers.Processes;

internal static class ToStartInfoExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="processConfiguration"></param>
    extension(ProcessConfiguration processConfiguration)
    {
        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal ProcessStartInfo ToProcessStartInfo()
        {
            ArgumentException.ThrowIfNullOrEmpty(processConfiguration.TargetFilePath);

            ProcessStartInfo processStartInfo = new()
            {
                FileName = processConfiguration.TargetFilePath,
                Arguments = string.IsNullOrEmpty(processConfiguration.Arguments)
                    ? string.Empty
                    : processConfiguration.Arguments,
                WorkingDirectory = processConfiguration.WorkingDirectoryPath,
                UseShellExecute = processConfiguration.UseShellExecution,
                CreateNoWindow = !processConfiguration.WindowCreation,
                RedirectStandardInput =
                    processConfiguration.StandardInput is not null
                    && processConfiguration.RedirectStandardInput,
                RedirectStandardOutput =  processConfiguration.OutputRedirection != OutputRedirectionMode.None,
                RedirectStandardError = processConfiguration.OutputRedirection != OutputRedirectionMode.None,
            };

            if (processConfiguration.RequiresAdministrator)
                processStartInfo.RunAsAdministrator();

#pragma warning disable CA1416
            processStartInfo.SetUserCredential(processConfiguration.Credential);
#pragma warning restore CA1416

            if (processConfiguration.EnvironmentVariables.Count > 0)
                processStartInfo.SetEnvironmentVariables(processConfiguration.EnvironmentVariables);

#if NET8_0_OR_GREATER
            if (processStartInfo.RedirectStandardInput)
                processStartInfo.StandardInputEncoding = processConfiguration.StandardInputEncoding;
#endif

            if (processStartInfo.RedirectStandardOutput)
                processStartInfo.StandardOutputEncoding =
                    processConfiguration.StandardOutputEncoding;

            if (processStartInfo.RedirectStandardError)
                processStartInfo.StandardErrorEncoding = processConfiguration.StandardErrorEncoding;

            return processStartInfo;
        }
    }
}