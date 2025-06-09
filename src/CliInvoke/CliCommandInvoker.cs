/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

    Based on Tyrrrz's CliWrap Command.Execution.cs
    https://github.com/Tyrrrz/CliWrap/blob/master/CliWrap/Command.Execution.cs

     Constructor signature and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

// ReSharper disable RedundantBoolCompare
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Abstractions;

using AlastairLundy.CliInvoke.Core.Abstractions.Legacy;
using AlastairLundy.CliInvoke.Core.Abstractions.Piping;
using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Exceptions;
using AlastairLundy.CliInvoke.Core.Primitives.Results;

#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

// ReSharper disable CheckNamespace
namespace AlastairLundy.CliInvoke;

/// <summary>
/// The default implementation of the CliInvoke's command running mechanism, ICliCommandInvoker.
/// </summary>
public class CliCommandInvoker : ICliCommandInvoker
{
        private readonly IPipedProcessRunner _pipedProcessRunner;
        
        private readonly IProcessPipeHandler _processPipeHandler;

        /// <summary>
        /// Initializes the CommandInvoker with the ICommandPipeHandler to be used.
        /// </summary>
        /// <param name="pipedProcessRunner">The piped process runner to be used.</param>
        /// <param name="processPipeHandler">The process pipe handler to be used.</param>
        public CliCommandInvoker(IPipedProcessRunner pipedProcessRunner,
            IProcessPipeHandler processPipeHandler)
        {
            _pipedProcessRunner = pipedProcessRunner;
            _processPipeHandler = processPipeHandler;
        }
        
        
        /// <summary>
        /// Executes a command configuration asynchronously and returns Command execution information as a ProcessResult.
        /// </summary>
        /// <param name="processConfiguration">The command configuration to be executed.</param>
        /// <param name="cancellationToken">A token to cancel the operation if required.</param>
        /// <returns>A ProcessResult object containing the execution information of the command.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the executable's specified file path is not found.</exception>
#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("ios")]
        [SupportedOSPlatform("android")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("browser")]
#endif
        public async Task<ProcessResult> ExecuteAsync(ProcessConfiguration processConfiguration, CancellationToken cancellationToken = default)
        {
            Process process = new Process()
            {
                StartInfo = processConfiguration.ToProcessStartInfo()
            };
            
            if (process.StartInfo.RedirectStandardInput &&
                processConfiguration.StandardInput is not null
                && processConfiguration.StandardInput != StreamWriter.Null)
            {
                await _processPipeHandler.PipeStandardInputAsync(processConfiguration.StandardInput.BaseStream, process);
            }
            
            (ProcessResult processResult, Stream standardOutput, Stream standardError) result =
                await _pipedProcessRunner.ExecuteProcessWithPipingAsync(process, ProcessResultValidation.None, processConfiguration.ResourcePolicy,
                    cancellationToken);

            // Throw a CommandNotSuccessful exception if required.
            if (result.processResult.ExitCode != 0 && processConfiguration.ResultValidation == ProcessResultValidation.ExitCodeZero)
            {
                throw new ProcessNotSuccessfulException(result.processResult.ExitCode, process);
            }
            
            if (process.StartInfo.RedirectStandardOutput && 
                processConfiguration.StandardOutput is not null)
            {
                await result.standardOutput.CopyToAsync(processConfiguration.StandardOutput.BaseStream,
                    cancellationToken);
            }
            if (process.StartInfo.RedirectStandardError &&
                processConfiguration.StandardError is not null)
            {
                await result.standardError.CopyToAsync(processConfiguration.StandardError.BaseStream, cancellationToken);
            }
            
            return result.processResult;
        }

        /// <summary>
        /// Executes a command configuration asynchronously and returns Command execution information and Command output as a BufferedProcessResult.
        /// </summary>
        /// <param name="processConfiguration"></param>
        /// <param name="cancellationToken">A token to cancel the operation if required.</param>
        /// <returns>A BufferedProcessResult object containing the output of the command.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the executable's specified file path is not found.</exception>
#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("maccatalyst")]
        [SupportedOSPlatform("android")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("browser")]
#endif
        public async Task<BufferedProcessResult> ExecuteBufferedAsync(ProcessConfiguration processConfiguration,
            CancellationToken cancellationToken = default)
        {
            Process process = new Process()
            {
                StartInfo = processConfiguration.ToProcessStartInfo(true, true)
            };

            if (process.StartInfo.RedirectStandardInput &&
                processConfiguration.StandardInput is not null
                && processConfiguration.StandardInput != StreamWriter.Null)
            {
                await _processPipeHandler.PipeStandardInputAsync(processConfiguration.StandardInput.BaseStream, process);
            }
            
            // PipedProcessRunner runs the Process for us.
            (BufferedProcessResult processResult, Stream standardOutput, Stream standardError) result =
                await _pipedProcessRunner.ExecuteBufferedProcessWithPipingAsync(process, ProcessResultValidation.None, processConfiguration.ResourcePolicy,
                    cancellationToken);
            
            // Throw a CommandNotSuccessful exception if required.
            if (result.processResult.ExitCode != 0 && processConfiguration.ResultValidation == ProcessResultValidation.ExitCodeZero)
            {
                throw new ProcessNotSuccessfulException(result.processResult.ExitCode,
                    process);
            }
            
            if (process.StartInfo.RedirectStandardOutput &&
                processConfiguration.StandardOutput is not null)
            {
                await result.standardOutput.CopyToAsync(processConfiguration.StandardOutput.BaseStream,
                    cancellationToken);
            }
            if (process.StartInfo.RedirectStandardError &&
                processConfiguration.StandardError is not null)
            {
                await result.standardError.CopyToAsync(processConfiguration.StandardError.BaseStream, cancellationToken);
            }
            
            return result.processResult;
        }
}