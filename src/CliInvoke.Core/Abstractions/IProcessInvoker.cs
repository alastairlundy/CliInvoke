/*
    AlastairLundy.CliInvoke.Core
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Policies;
using AlastairLundy.CliInvoke.Core.Primitives.Results;

namespace AlastairLundy.CliInvoke.Core.Abstractions
{
    /// <summary>
    /// Defines the contract for a class that executes processes.
    /// </summary>
    public interface IProcessInvoker
    {
        /// <summary>
        /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
        /// </summary>
        /// <param name="process">The process to be run.</param>
        /// <param name="processConfiguration"></param>
        /// <param name="cancellationToken">A token to cancel the operation if required.</param>
        /// <returns>The Process Results from the running the process.</returns>
         Task<ProcessResult> ExecuteProcessAsync(Process process, 
            ProcessConfiguration processConfiguration,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
        /// </summary>
        /// <param name="process">The process to be run.</param>
        /// <param name="processResultValidation">The process result validation to be used.</param>
        /// <param name="processResourcePolicy">The process resource policy to be set if it is not null.</param>
        /// <param name="cancellationToken">A token to cancel the operation if required.</param>
        /// <returns>The Process Results from the running the process.</returns>
         Task<ProcessResult> ExecuteProcessAsync(Process process, ProcessResultValidation processResultValidation,
            ProcessResourcePolicy? processResourcePolicy = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
        /// </summary>
        /// <param name="process">The process to be run.</param>
        /// <param name="processConfiguration"></param>
        /// <param name="cancellationToken">A token to cancel the operation if required.</param>
        /// <returns>The Buffered Process Results from running the process.</returns>
         Task<BufferedProcessResult> ExecuteBufferedProcessAsync(Process process,
            ProcessConfiguration processConfiguration,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
        /// </summary>
        /// <param name="process">The process to be run.</param>
        /// <param name="processResultValidation">The process result validation to be used.</param>
        /// <param name="processResourcePolicy">The process resource policy to be set if it is not null.</param>
        /// <param name="cancellationToken">A token to cancel the operation if required.</param>
        /// <returns>The Buffered Process Results from running the process.</returns>
         Task<BufferedProcessResult> ExecuteBufferedProcessAsync(Process process,
            ProcessResultValidation processResultValidation,
            ProcessResourcePolicy? processResourcePolicy = null,
            CancellationToken cancellationToken = default);
    }
}