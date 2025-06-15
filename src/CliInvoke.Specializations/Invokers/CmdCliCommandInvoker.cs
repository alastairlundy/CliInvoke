/*
    CliInvoke Specializations
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Diagnostics;
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
    public async Task<ProcessResult> ExecuteAsync(ProcessConfiguration processConfiguration, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<ProcessResult> ExecuteProcessAsync(ProcessStartInfo processStartInfo, ProcessResultValidation processResultValidation,
        ProcessResourcePolicy processResourcePolicy = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<BufferedProcessResult> ExecuteBufferedAsync(ProcessConfiguration processConfiguration,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<BufferedProcessResult> ExecuteBufferedProcessAsync(ProcessStartInfo processStartInfo,
        ProcessResultValidation processResultValidation,
        ProcessResourcePolicy processResourcePolicy = null, UserCredential userCredential = null,
        CancellationToken cancellationToken = bad)
    {
        throw new NotImplementedException();
    }

    public async Task<PipedProcessResult> ExecutePipedAsync(ProcessConfiguration processConfiguration, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<PipedProcessResult> ExecutePipedProcessAsync(ProcessStartInfo processStartInfo,
        ProcessResultValidation processResultValidation,
        ProcessResourcePolicy processResourcePolicy = null, UserCredential userCredential = null,
        CancellationToken cancellationToken = bad)
    {
        throw new NotImplementedException();
    }
}