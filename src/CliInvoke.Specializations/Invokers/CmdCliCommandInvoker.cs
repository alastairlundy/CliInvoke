/*
    CliInvoke Specializations
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
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Abstractions;
using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Policies;
using AlastairLundy.CliInvoke.Core.Primitives.Results;
using AlastairLundy.CliInvoke.Specializations.Configurations;
using AlastairLundy.CliInvoke.Specializations.Internal.Localizations;

namespace AlastairLundy.CliInvoke.Specializations.Invokers;

/// <summary>
/// Run commands through CMD with ease.
/// </summary>
public class CmdCliCommandInvoker : IProcessInvoker
{
    public Task<ProcessResult> ExecuteAsync(ProcessConfiguration processConfiguration, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ProcessResult> ExecuteAsync(ProcessStartInfo processStartInfo, ProcessResultValidation processResultValidation,
        ProcessResourcePolicy processResourcePolicy = null, UserCredential userCredential = null,
        StreamWriter standardInput = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<BufferedProcessResult> ExecuteBufferedAsync(ProcessConfiguration processConfiguration, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<BufferedProcessResult> ExecuteBufferedAsync(ProcessStartInfo processStartInfo, ProcessResultValidation processResultValidation,
        ProcessResourcePolicy processResourcePolicy = null, UserCredential userCredential = null,
        StreamWriter standardInput = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<PipedProcessResult> ExecutePipedAsync(ProcessConfiguration processConfiguration, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<PipedProcessResult> ExecutePipedAsync(ProcessStartInfo processStartInfo, ProcessResultValidation processResultValidation,
        ProcessResourcePolicy processResourcePolicy = null, UserCredential userCredential = null,
        StreamWriter standardInput = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}