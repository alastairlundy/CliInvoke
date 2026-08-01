/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core;

/// <summary>
///     The single state-bearing object the process invocation pipeline accepts and mutates during execution.
/// </summary>
public class ProcessInvocationContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProcessInvocationContext"/> class.
    /// </summary>
    /// <param name="configuration">The process configuration.</param>
    /// <param name="exitConfiguration">The process exit configuration.</param>
    /// <param name="mode">The invocation mode that determines the capture path.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    public ProcessInvocationContext(
        ProcessConfiguration configuration,
        ProcessExitConfiguration exitConfiguration,
        InvocationMode mode,
        CancellationToken cancellationToken = default)
    {
        Configuration = configuration;
        ExitConfiguration = exitConfiguration;
        Mode = mode;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    ///     Gets the process configuration.
    /// </summary>
    public ProcessConfiguration Configuration { get; }

    /// <summary>
    ///     Gets the process exit configuration.
    /// </summary>
    public ProcessExitConfiguration ExitConfiguration { get; }

    /// <summary>
    ///     Gets the invocation mode that determines the capture path.
    /// </summary>
    public InvocationMode Mode { get; }

    /// <summary>
    ///     Gets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    ///     Gets or sets the process result, set by the pipeline after execution.
    /// </summary>
    public ProcessResult? Result { get; set; }
}
