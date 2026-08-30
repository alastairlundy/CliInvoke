/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Exceptions;
using CliInvoke.Core.Factories;
using CliInvoke.Core.Processes;
using CliInvoke.Core.Validation;

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
        long? GetTruncationCap()
        {
            var middleware = ctx.Middleware;

            if (middleware is not null &&
                middleware.Items.TryGet<long>(TruncationDefaults.MaxBytesPerStreamKey, out long cap))
                return cap;

            return null;
        }

        long? truncationCap = GetTruncationCap();

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

            // Raw awaits process exit (it does not drain redirected output). Buffered must be
            // started WITHOUT awaiting exit so the capture methods can read the redirected streams
            // concurrently with waiting for exit; awaiting exit first would deadlock when a child writes
            // more than the OS pipe buffer and nothing is draining it yet.
            if (ctx.Mode == InvocationMode.Raw)
                await externalProcess.StartAsync(ctx.CancellationToken);
            else
                externalProcess.Start();

            // FireAndForget returns above (lines 46-63) without waiting.
            // Only Raw and Buffered reach this switch.
            TResult result = ctx.Mode switch
            {
                InvocationMode.Raw => (TResult)await externalProcess.WaitForExitOrTimeoutAsync(ctx.CancellationToken),
                InvocationMode.Buffered => (TResult)(object)await externalProcess.CaptureBufferedResultAsync(
                    ctx.CancellationToken, truncationCap, truncationCap),
                _ => throw new InvalidOperationException($"Unsupported invocation mode: {ctx.Mode}")
            };

            ValidateResult(result, ctx.ExitConfiguration);

            return result;
        }
        finally
        {
            externalProcess.Dispose();
        }

        /// <summary>
        ///     Evaluates the configured validation rules against a completed process result and throws
        ///     when any rule fails.
        /// </summary>
        /// <typeparam name="TResult">The type of process result being validated.</typeparam>
        /// <param name="result">The process result produced by the invocation.</param>
        /// <param name="exitConfiguration">
        ///     The exit configuration whose <see cref="ProcessExitConfiguration.ValidationRules" /> are
        ///     evaluated, or <c>null</c> when no validation is configured.
        /// </param>
        /// <exception cref="ProcessValidationException">
        ///     Thrown when <paramref name="exitConfiguration" /> declares a rule that the result fails.
        /// </exception>
        static void ValidateResult<TResult>(TResult result, ProcessExitConfiguration? exitConfiguration)
            where TResult : ProcessResult
        {
            if (exitConfiguration is null)
                return;

            ValidationRule<ProcessResult>[] rules = exitConfiguration.ValidationRules;

            if (rules is null)
                return;

            foreach (ValidationRule<ProcessResult> rule in rules)
            {
                if (!rule.Predicate(result))
                    throw new ProcessValidationException(result, rule.GetFailureMessage(result));
            }
        }
    }
}
