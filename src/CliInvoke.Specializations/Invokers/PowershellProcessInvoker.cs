﻿/*
    CliInvoke Specializations
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AlastairLundy.CliInvoke.Core.Abstractions;
using AlastairLundy.CliInvoke.Core.Extensibility;
using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Policies;
using AlastairLundy.CliInvoke.Core.Primitives.Results;
using AlastairLundy.CliInvoke.Extensibility;
using AlastairLundy.CliInvoke.Specializations.Configurations;

#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace AlastairLundy.CliInvoke.Specializations.Invokers;

/// <summary>
/// Run commands through cross-platform modern PowerShell with ease.
/// </summary>
public class PowershellProcessInvoker : IProcessInvoker
{
    private readonly IProcessInvoker _processInvoker;
    private readonly IRunnerProcessCreator _runnerProcessCreator;

    public PowershellProcessInvoker(IProcessInvoker processInvoker)
    {
        _processInvoker = processInvoker;
        _runnerProcessCreator = new RunnerProcessCreator(new PowershellProcessConfiguration(processInvoker));
    }
    
    public async Task<ProcessResult> ExecuteProcessAsync(ProcessConfiguration processConfiguration, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    public async Task<ProcessResult> ExecuteProcessAsync(ProcessStartInfo processStartInfo, ProcessResultValidation processResultValidation,
        ProcessResourcePolicy processResourcePolicy = null, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    public async Task<BufferedProcessResult> ExecuteBufferedProcessAsync(ProcessConfiguration processConfiguration,
        CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    public async Task<BufferedProcessResult> ExecuteBufferedProcessAsync(ProcessStartInfo processStartInfo, ProcessResultValidation processResultValidation,
        ProcessResourcePolicy processResourcePolicy = null, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    public async Task<PipedProcessResult> ExecutePipedProcessAsync(ProcessConfiguration processConfiguration, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    public async Task<PipedProcessResult> ExecutePipedProcessAsync(ProcessStartInfo processStartInfo, ProcessResultValidation processResultValidation,
        ProcessResourcePolicy processResourcePolicy = null, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }
}