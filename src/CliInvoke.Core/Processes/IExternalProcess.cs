/*
    CliInvoke.Core

    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace CliInvoke.Core.Processes;

/// <summary>
///     A contract for an external process that can be run.
/// </summary>
public interface IExternalProcess : IDisposable
{
    /// <summary>
    ///     Represents the configuration settings used by an external process.
    /// </summary>
    ProcessConfiguration Configuration { get; init; }

    /// <summary>
    ///     Represents the configuration for handling external process exit.
    /// </summary>
    ProcessExitConfiguration ExitConfiguration { get; }

    /// <summary>
    ///     Indicates whether the external process has exited.
    /// </summary>
    bool HasExited { get; }

    /// <summary>
    ///     Indicates whether the external process has started.
    /// </summary>
    bool HasStarted { get; }

    /// <summary>
    ///     Represents an event that occurs when the external process starts.
    /// </summary>
    event EventHandler Started;

    /// <summary>
    ///     Represents an event that occurs when the external process exits.
    /// </summary>
    event EventHandler Exited;

    /// <summary>
    ///     Synchronously starts the external process and returns its process ID.
    ///     Stdin piping is not performed by this method.
    /// </summary>
    /// <returns>The process ID of the started process.</returns>
    int Start();

    /// <summary>
    ///     Asynchronously starts the external process, feeding the configured standard
    ///     input to the child when redirection is enabled.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used by other objects or threads
    ///     to receive notice of cancellation.
    /// </param>
    /// <returns>
    ///     A task that completes once the process has been launched and any configured
    ///     standard input has been piped to the child. The method does not wait for the
    ///     process to exit and produces no process result; obtain the result separately
    ///     via <see cref="WaitForExitOrTimeoutAsync(CancellationToken)"/> or
    ///     <see cref="CaptureBufferedResultAsync(CancellationToken, long?, long?)"/>.
    /// </returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Starts the external process asynchronously using the specified configuration,
    ///     feeding that configuration's standard input to the child.
    /// </summary>
    /// <param name="configuration">The configuration settings for starting the external process.</param>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used by other objects or threads
    ///     to receive notice of cancellation.
    /// </param>
    /// <returns>
    ///     A task that completes once the process has been launched and the supplied
    ///     configuration's standard input has been piped to the child. The method does
    ///     not wait for the process to exit and produces no process result; obtain the
    ///     result separately via <see cref="WaitForExitOrTimeoutAsync(CancellationToken)"/>
    ///     or <see cref="CaptureBufferedResultAsync(CancellationToken, long?, long?)"/>.
    /// </returns>
    Task StartAsync(ProcessConfiguration configuration, CancellationToken cancellationToken);

    /// <summary>
    ///     Asynchronously waits for the process to exit or a specified timeout period to elapse.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used by other objects or threads
    ///     to receive notice of cancellation.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The result contains the buffered process result when the method completes.
    /// </returns>
    Task<ProcessResult> WaitForExitOrTimeoutAsync(CancellationToken cancellationToken);
    
    /// <summary>
    ///     Asynchronously feeds the configured standard input to the child when redirection
    ///     is enabled, captures output, and waits for the external process to exit or a
    ///     specified timeout period to elapse.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A cancellation token that can be used by other objects or threads
    ///     to receive notice of cancellation.
    /// </param>
    /// <param name="maxStandardOutputBytes">
    ///     An optional maximum number of bytes to capture from standard output before truncating.
    ///     <c>null</c> means no cap is applied.
    /// </param>
    /// <param name="maxStandardErrorBytes">
    ///     An optional maximum number of bytes to capture from standard error before truncating.
    ///     <c>null</c> means no cap is applied.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The result contains the buffered process result when the method completes.
    /// </returns>
    Task<BufferedProcessResult> CaptureBufferedResultAsync(
        CancellationToken cancellationToken,
        long? maxStandardOutputBytes = null,
        long? maxStandardErrorBytes = null);

    /// <summary>
    ///     Terminates the associated external process.
    /// </summary>
    Task Kill();
}