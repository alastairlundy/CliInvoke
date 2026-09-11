/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Middleware;

namespace CliInvoke.Core;

/// <summary>
///     The single state-bearing object the process invocation pipeline accepts and mutates during execution.
/// </summary>
/// <remarks>
///     Renamed from <c>ProcessInvocationContext</c> for traceability.
/// </remarks>
public class InvocationContext
{
    /// <summary>
    ///     Initialises a new instance of the <see cref="InvocationContext"/> class.
    /// </summary>
    /// <param name="configuration">The process configuration.</param>
    /// <param name="exitConfiguration">The process exit configuration.</param>
    /// <param name="mode">The invocation mode that determines the capture path.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    public InvocationContext(
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
    ///     Initialises a new instance of the <see cref="InvocationContext"/> class, sharing the
    ///     result state with another context.
    /// </summary>
    private InvocationContext(
        ProcessConfiguration configuration,
        ProcessExitConfiguration exitConfiguration,
        InvocationMode mode,
        CancellationToken cancellationToken,
        SharedResult sharedResult)
        : this(configuration, exitConfiguration, mode, cancellationToken)
    {
        _sharedResult = sharedResult;
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
    ///     A shared holder for the execution result so that derived contexts produced by
    ///     <see cref="WithConfiguration"/> observe the same result state as the original context.
    /// </summary>
    private readonly SharedResult _sharedResult = new();

    /// <summary>
    ///     Gets or sets the process result produced by the invocation.
    /// </summary>
    /// <remarks>
    ///     Owned by the pipeline, not by caller code. Only the <see cref="MiddlewareChain"/>
    ///     walker, the terminal delegate, and propagating middleware may set this. Reading it
    ///     before the chain completes returns <see langword="null"/>. Derived contexts (created
    ///     via <see cref="WithConfiguration"/>) share the same result state.
    /// </remarks>
    public ProcessResult? Result
    {
        get => _sharedResult.Value;
        set => _sharedResult.Value = value;
    }

    private sealed class SharedResult
    {
        public ProcessResult? Value { get; set; }
    }

    /// <summary>
    ///     Gets or sets the <see cref="MiddlewareContext"/> exposed to middleware during execution.
    /// </summary>
    /// <remarks>
    ///     Owned by the invoker. The <see cref="MiddlewareChain"/> walker assigns this before
    ///     invoking the first middleware. It is <see langword="null"/> until the walker populates it.
    /// </remarks>
    public MiddlewareContext? Middleware { get; set; }

    /// <summary>
    ///     Creates a new <see cref="InvocationContext"/> with the specified configuration
    ///     while preserving all other context state.
    /// </summary>
    /// <param name="configuration">The new process configuration to use.</param>
    /// <returns>A new invocation context with the updated configuration.</returns>
    public InvocationContext WithConfiguration(ProcessConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new InvocationContext(
            configuration,
            ExitConfiguration,
            Mode,
            CancellationToken,
            _sharedResult)
        {
            Middleware = Middleware
        };
    }

    /// <summary>
    ///     Creates a new <see cref="InvocationContext"/> with the specified exit configuration
    ///     while preserving all other context state.
    /// </summary>
    /// <param name="exitConfiguration">The new process exit configuration to use.</param>
    /// <returns>A new invocation context with the updated exit configuration.</returns>
    public InvocationContext WithExitConfiguration(ProcessExitConfiguration exitConfiguration)
    {
        ArgumentNullException.ThrowIfNull(exitConfiguration);

        return new InvocationContext(
            Configuration,
            exitConfiguration,
            Mode,
            CancellationToken,
            _sharedResult)
        {
            Middleware = Middleware
        };
    }
}
