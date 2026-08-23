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
    ///     Initializes a new instance of the <see cref="InvocationContext"/> class, sharing the
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
    ///     <para>
    ///         This property is <b>owned by the pipeline</b>, not by caller code. The only
    ///         legitimate mutators are the <see cref="MiddlewareChain"/> walker, the terminal
    ///         delegate that bridges the chain to the pipeline (for example
    ///         <c>ProcessInvoker.RunPipelineThroughContext</c>), and any propagating middleware
    ///         that short-circuits the chain by assigning its own result. Caller code — including
    ///         user-authored middleware — must not read or write <see cref="Result"/> outside those
    ///         mutators; reading it before the chain completes returns <see langword="null"/>.
    ///     </para>
    ///     <para>
    ///         Derived contexts (created via <see cref="WithConfiguration"/>) share this value,
    ///         so a result set by the terminal pipeline on a derived context is visible to the
    ///         original context that the invoker reads from.
    ///     </para>
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
    ///     <para>
    ///         This property is <b>owned by the invoker</b>, not by caller code. The only legitimate
    ///         mutator is the <see cref="MiddlewareChain"/> walker, which assigns this immediately
    ///         before invoking the first middleware so that middleware can read framework-level
    ///         services (such as a logger) from <see cref="MiddlewareContext.Items"/>. Caller code —
    ///         including user-authored middleware — must not read or write <see cref="Middleware"/>
    ///         outside the chain; it is <see langword="null"/> until the walker populates it.
    ///     </para>
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
}
