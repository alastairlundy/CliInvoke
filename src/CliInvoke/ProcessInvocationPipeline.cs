/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Factories;
using CliInvoke.Core.Processes;

namespace CliInvoke;

/// <summary>
///     The internal pipeline that owns the five-line execution skeleton
///     (factory, start, wait or capture, dispose). The four invoker modules
///     collapse to wrappers that call this class.
/// </summary>
internal class ProcessInvocationPipeline
{
    private readonly IExternalProcessFactory _externalProcessFactory;

    /// <summary>
    ///     Initialises a new instance of the <see cref="ProcessInvocationPipeline"/> class.
    /// </summary>
    /// <param name="externalProcessFactory">The factory used to create external processes.</param>
    internal ProcessInvocationPipeline(IExternalProcessFactory externalProcessFactory)
    {
        _externalProcessFactory = externalProcessFactory;
    }

    /// <summary>
    ///     Invokes a process using the specified invocation context and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The type of process result to return.</typeparam>
    /// <param name="ctx">The process invocation context containing configuration and mode.</param>
    /// <returns>The process result of type <typeparamref name="TResult"/>.</returns>
    public async Task<TResult> InvokeAsync<TResult>(InvocationContext ctx) where TResult : ProcessResult
    {
        IExternalProcess externalProcess = _externalProcessFactory.CreateExternalProcess(
            ctx.Configuration, ctx.ExitConfiguration);

        try
        {
            if (ctx.Mode == InvocationMode.FireAndForget)
            {
                if (typeof(TResult) != typeof(ProcessResult))
                {
                    throw new InvalidOperationException(
                        $"FireAndForget mode only supports {nameof(ProcessResult)} results, not {typeof(TResult).Name}.");
                }

                int processId = externalProcess.Start();

                return (TResult)new ProcessResult(
                    externalProcess.Configuration.TargetFilePath,
                    0,
                    processId,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    canceled: false,
                    signal: null);
            }

            await externalProcess.StartAsync(ctx.CancellationToken);

            // FireAndForget returns above (lines 46-63) without waiting.
            // Only Raw, Buffered, and Piped reach this switch.
            return ctx.Mode switch
            {
                InvocationMode.Raw => (TResult)await externalProcess.WaitForExitOrTimeoutAsync(ctx.CancellationToken),
                InvocationMode.Buffered => (TResult)(object)await externalProcess.CaptureBufferedResultAsync(ctx.CancellationToken),
                InvocationMode.Piped => (TResult)(object)await externalProcess.CapturePipedResultAsync(ctx.CancellationToken),
                _ => throw new InvalidOperationException($"Unsupported invocation mode: {ctx.Mode}")
            };
        }
        finally
        {
            externalProcess.Dispose();
        }
    }
}
