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
    ///     Extensions that convert a <see cref="ProcessConfiguration" /> to the
    ///     <see cref="ProcessStartInfo" /> used to launch its process.
    /// </summary>
    /// <param name="processConfiguration">The configuration to convert.</param>
    extension(ProcessConfiguration processConfiguration)
    {
        /// <summary>
        ///     Applies the configuration to a new <see cref="ProcessStartInfo" />.
        /// </summary>
        /// <param name="redirectStandardOutput">Whether to redirect the standard output stream.</param>
        /// <param name="redirectStandardError">Whether to redirect the standard error stream.</param>
        /// <returns>A <see cref="ProcessStartInfo" /> with the configuration applied.</returns>
        /// <exception cref="ArgumentException">Thrown if the target file path is empty, or if
        /// an argument token contains a character that cmd.exe parses itself.</exception>
        internal ProcessStartInfo ToProcessStartInfo(
            bool redirectStandardOutput,
            bool redirectStandardError
        )
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
                RedirectStandardOutput = redirectStandardOutput,
                RedirectStandardError = redirectStandardError,
            };

            // cmd.exe and batch files parse the command line with their own rules, so
            // metacharacters keep their meaning inside tokens no matter how they are
            // quoted. Tokens are re-checked here regardless of how the values are
            // delivered, to apply the same rules outside the runner factory. Values
            // are rejected, not escaped.
            if (processConfiguration.ArgumentsList.Count > 0
                && CmdCompositionArgumentValidator.IsCmdOrBatchTarget(
                    processConfiguration.TargetFilePath))
            {
                CmdCompositionArgumentValidator.Validate(
                    processConfiguration.ArgumentsList,
                    nameof(processConfiguration.ArgumentsList));
            }

            // With pre-tokenized arguments and no shell, pass each value through
            // ArgumentList. The OS quotes every token (embedded quotes become ""),
            // so a value can never be re-parsed into more command-line tokens.
            // With shell execution the raw Arguments string is delivered as-is.
            if (!processConfiguration.UseShellExecution
                && processConfiguration.ArgumentsList.Count > 0)
            {
                processStartInfo.Arguments = string.Empty;
                foreach (string argument in processConfiguration.ArgumentsList)
                    processStartInfo.ArgumentList.Add(argument);
            }
        
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
                processStartInfo.StandardOutputEncoding = processConfiguration.StandardOutputEncoding;

            if (processStartInfo.RedirectStandardError)
                processStartInfo.StandardErrorEncoding = processConfiguration.StandardErrorEncoding;

            return processStartInfo;
        }
    }
}
