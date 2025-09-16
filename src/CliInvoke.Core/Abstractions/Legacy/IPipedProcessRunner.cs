/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AlastairLundy.CliInvoke.Core.Internal;
using AlastairLundy.CliInvoke.Core.Primitives.Policies;
using AlastairLundy.CliInvoke.Core.Primitives.Results;

namespace AlastairLundy.CliInvoke.Core.Abstractions.Legacy
{
    /// <summary>
    /// A Process Runner like interface for Piping output after Executing processes.
    /// </summary>
    [Obsolete(DeprecationMessages.InterfaceDeprecationV2)]
    public interface IPipedProcessRunner
    {
        /// <summary>
        /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
        /// </summary>
        /// <param name="process">The process to be run.</param>
        /// <param name="processResultValidation">The process result validation to be used.</param>
        /// <param name="processResourcePolicy"></param>
        /// <param name="cancellationToken">A token to cancel the operation if required.</param>
        /// <returns>The Process Results from the running the process with the Piped Standard Output and Standard Error.</returns>
        [Obsolete(DeprecationMessages.InterfaceDeprecationV2)]
        Task<(ProcessResult processResult, Stream standardOutput, Stream standardError)>
            ExecuteProcessWithPipingAsync(Process process, ProcessResultValidation processResultValidation,
                ProcessResourcePolicy? processResourcePolicy = null,
                CancellationToken cancellationToken = default);


        /// <summary>
        /// Runs the process asynchronously, waits for exit, and safely disposes of the Process before returning.
        /// </summary>
        /// <param name="process">The process to be run.</param>
        /// <param name="processResultValidation">The process result validation to be used.</param>
        /// <param name="processResourcePolicy"></param>
        /// <param name="cancellationToken">A token to cancel the operation if required.</param>
        /// <returns>The Buffered Process Results from running the process with the Piped Standard Output and Standard Error.</returns>
        [Obsolete(DeprecationMessages.InterfaceDeprecationV2)]
        Task<(BufferedProcessResult processResult, Stream standardOutput, Stream standardError)>
            ExecuteBufferedProcessWithPipingAsync(Process process,
                ProcessResultValidation processResultValidation,
                ProcessResourcePolicy? processResourcePolicy = null,
                CancellationToken cancellationToken = default);
    }
}