/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;

using CliInvoke.Core;
using CliInvoke.Core.Extensibility;
using CliInvoke.Core.Extensibility.Factories;

namespace CliInvoke.Extensibility;

/// <summary>
/// The default implementation of <see cref="RunnerProcessInvokerBase"/>.
/// </summary>
public class DefaultRunnerProcessInvoker : RunnerProcessInvokerBase
{
    /// <summary>
    /// Provides a default implementation for executing and managing runner processes, extending
    /// the abstract functionality defined in <see cref="RunnerProcessInvokerBase"/>.
    /// </summary>
    /// <param name="processInvoker"></param>
    /// <param name="runnerProcessFactory"></param>
    /// <param name="runnerProcessConfiguration"></param>
    public DefaultRunnerProcessInvoker(
        IProcessInvoker processInvoker,
        IRunnerProcessFactory runnerProcessFactory,
        ProcessConfiguration runnerProcessConfiguration
    )
        : base(processInvoker, runnerProcessFactory, runnerProcessConfiguration)
    {
        ArgumentNullException.ThrowIfNull(runnerProcessConfiguration);
    }
}
