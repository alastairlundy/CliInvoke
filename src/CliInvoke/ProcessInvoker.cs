/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Linq;

using CliInvoke.Core.Factories;
using CliInvoke.Core.Middleware;

namespace CliInvoke;

/// <summary>
///     The default implementation of <see cref="IProcessInvoker" />, a safer way to execute processes.
/// </summary>
public class ProcessInvoker : IProcessInvoker
{
    private readonly IExternalProcessFactory _externalProcessFactory;
    private readonly ProcessInvocationPipeline _pipeline;
    private readonly IReadOnlyList<IProcessMiddleware> _middlewares;
    private readonly MiddlewareChain? _chain;
    private readonly MiddlewareItems? _sharedItems;

    /// <summary>
    ///     Instantiates a <see cref="ProcessInvoker" /> for creating and executing processes.
    /// </summary>
    /// <param name="externalProcessFactory">The factory used to create external processes.</param>
    public ProcessInvoker(IExternalProcessFactory externalProcessFactory)
        : this(externalProcessFactory, sharedItems: null)
    {
    }

    /// <summary>
    ///     Instantiates a <see cref="ProcessInvoker" /> for creating and executing processes,
    ///     with pre-seeded middleware items shared across every invocation's chain.
    /// </summary>
    /// <param name="externalProcessFactory">The factory used to create external processes.</param>
    /// <param name="sharedItems">
    ///     Pre-seeded middleware items shared across every invocation's chain. Use this
    ///     to inject framework-level services (such as a logger)
    ///     that middleware can read from <see cref="InvocationContext.Middleware"/>.
    /// </param>
    public ProcessInvoker(
        IExternalProcessFactory externalProcessFactory,
        MiddlewareItems? sharedItems)
    {
        _externalProcessFactory = externalProcessFactory;
        _middlewares = Array.Empty<IProcessMiddleware>();
        _sharedItems = sharedItems;
        _pipeline = new ProcessInvocationPipeline(externalProcessFactory);
        _chain = sharedItems is null
            ? null
            : new MiddlewareChain(Array.Empty<IProcessMiddleware>(), RunPipelineThroughContext, sharedItems);
    }

    /// <summary>
    ///     Instantiates a <see cref="ProcessInvoker" /> for creating and executing processes
    ///     with middleware applied to every invocation.
    /// </summary>
    /// <param name="externalProcessFactory">The factory used to create external processes.</param>
    /// <param name="middlewares">The ordered middleware to apply before the terminal pipeline.</param>
    public ProcessInvoker(
        IExternalProcessFactory externalProcessFactory,
        IEnumerable<IProcessMiddleware> middlewares)
        : this(externalProcessFactory, middlewares, sharedItems: null)
    {
    }

    /// <summary>
    ///     Instantiates a <see cref="ProcessInvoker" /> for creating and executing processes
    ///     with middleware applied to every invocation, and pre-seeded middleware items
    ///     shared across every invocation's chain.
    /// </summary>
    /// <param name="externalProcessFactory">The factory used to create external processes.</param>
    /// <param name="middlewares">The ordered middleware to apply before the terminal pipeline.</param>
    /// <param name="sharedItems">
    ///     Pre-seeded middleware items shared across every invocation's chain. Use this
    ///     to inject framework-level services (such as a logger)
    ///     that middleware can read from <see cref="InvocationContext.Middleware"/>.
    /// </param>
    public ProcessInvoker(
        IExternalProcessFactory externalProcessFactory,
        IEnumerable<IProcessMiddleware> middlewares,
        MiddlewareItems? sharedItems)
    {
        ArgumentNullException.ThrowIfNull(middlewares);

        IReadOnlyList<IProcessMiddleware> materialized = middlewares.ToList();

        foreach (IProcessMiddleware middleware in materialized)
        {
            ArgumentNullException.ThrowIfNull(middleware);
        }

        _externalProcessFactory = externalProcessFactory;
        _middlewares = materialized;
        _sharedItems = sharedItems;
        _pipeline = new ProcessInvocationPipeline(externalProcessFactory);
        _chain = new MiddlewareChain(materialized, RunPipelineThroughContext, sharedItems);
    }

    /// <summary>
    ///     Terminal delegate that bridges the middleware chain to the pipeline.
    ///     Stores the typed result on the <see cref="InvocationContext.Result"/> property
    ///     so the caller can read it back after the chain completes.
    /// </summary>
    private async Task RunPipelineThroughContext(InvocationContext ctx)
    {
        ProcessResult result = ctx.Mode switch
        {
            InvocationMode.Raw => await _pipeline.InvokeAsync<ProcessResult>(ctx),
            InvocationMode.Buffered => await _pipeline.InvokeAsync<BufferedProcessResult>(ctx),
            InvocationMode.Piped => await _pipeline.InvokeAsync<PipedProcessResult>(ctx),
            _ => throw new InvalidOperationException($"Unsupported invocation mode: {ctx.Mode}")
        };

        ctx.Result = result;
    }

    /// <summary>
    ///     Executes the invocation through the middleware chain when middleware is present,
    ///     or directly through the pipeline when no middleware is configured.
    /// </summary>
    private async Task<TResult> InvokeThroughChainAsync<TResult>(InvocationContext ctx) where TResult : ProcessResult
    {
        if (_chain is not null)
        {
            await _chain.RunAsync(ctx, ctx.CancellationToken);

            if (ctx.Result is null)
            {
                throw new InvalidOperationException(
                    "The middleware chain completed without setting a result. " +
                    "Short-circuiting middleware must assign InvocationContext.Result before returning.");
            }

            return (TResult)ctx.Result;
        }

        return await _pipeline.InvokeAsync<TResult>(ctx);
    }

    /// <summary>
    ///     Runs the process asynchronously, waits for exit, and safely disposes of the Process before
    ///     returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processExitConfiguration">
    ///     The exit configuration to use for the process, or the
    ///     default if null.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Process Results from running the process.</returns>
    /// <exception cref="FileNotFoundException">
    ///     Thrown if the file, with the file name of the process to be
    ///     executed, is not found.
    /// </exception>
    /// <exception cref="ProcessNotSuccessfulException{TProcessResult}">
    ///     Thrown if the result validation requires the
    ///     process to exit with exit code zero and the process exits with a different exit code.
    /// </exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<ProcessResult> ExecuteAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        InvocationContext ctx = new InvocationContext(
            processConfiguration,
            processExitConfiguration ?? ProcessExitConfiguration.Default,
            InvocationMode.Raw,
            cancellationToken);

        return await InvokeThroughChainAsync<ProcessResult>(ctx);
    }

    /// <summary>
    ///     Runs the process asynchronously with Standard Output and Standard Error Redirection,
    ///     gets Standard Output and Standard Error as Strings, waits for exit, and safely disposes of the
    ///     Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="processExitConfiguration">
    ///     The exit configuration to use for the process, or the
    ///     default if null.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Buffered Process Results from running the process.</returns>
    /// <exception cref="ProcessNotSuccessfulException{TProcessResult}">
    ///     Thrown if the result validation requires the
    ///     process to exit with exit code zero and the process exits with a different exit code.
    /// </exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<BufferedProcessResult> ExecuteBufferedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        InvocationContext ctx = new InvocationContext(
            processConfiguration,
            processExitConfiguration ?? ProcessExitConfiguration.Default,
            InvocationMode.Buffered,
            cancellationToken);

        return await InvokeThroughChainAsync<BufferedProcessResult>(ctx);
    }

    /// <summary>
    ///     Runs the process asynchronously with Standard Output and Standard Error Redirection,
    ///     gets Standard Output and Standard Error as Streams, waits for exit, and safely disposes of the
    ///     Process before returning.
    /// </summary>
    /// <param name="processConfiguration">The configuration to use for the process.</param>
    /// <param name="exitConfiguration"></param>
    /// <param name="cancellationToken">A token to cancel the operation if required.</param>
    /// <returns>The Piped Process Results from running the process.</returns>
    /// <exception cref="ProcessNotSuccessfulException{TProcessResult}">
    ///     Thrown if the result validation requires the
    ///     process to exit with exit code zero and the process exits with a different exit code.
    /// </exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<PipedProcessResult> ExecutePipedAsync(
        ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? exitConfiguration = null, CancellationToken cancellationToken = default)
    {
        InvocationContext ctx = new InvocationContext(
            processConfiguration,
            exitConfiguration ?? ProcessExitConfiguration.Default,
            InvocationMode.Piped,
            cancellationToken);

        return await InvokeThroughChainAsync<PipedProcessResult>(ctx);
    }
}
