/*
    CliInvoke.Core
     
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace CliInvoke.Core.Processes;

/// <summary>
/// A contract for an external process that can be run.
/// </summary>
public interface IExternalProcess : IDisposable
{
    /// <summary>
    /// Represents the configuration settings used by an external process.
    /// </summary>
    ProcessConfiguration Configuration { get; set; }

    /// <summary>
    /// Represents the configuration for handling external process exit.
    /// </summary>
    ProcessExitConfiguration ExitConfiguration { get; set; }

    /// <summary>
    /// Indicates whether the external process has exited.
    /// </summary>
    bool HasExited { get; }

    /// <summary>
    /// Indicates whether the external process has started.
    /// </summary>
    bool HasStarted { get; }

    /// <summary>
    /// Represents an event that occurs when the external process starts.
    /// </summary>
    event EventHandler Started;

    /// <summary>
    /// Represents an event that occurs when the external process exits.
    /// </summary>
    event EventHandler Exited;

    /// <summary>
    /// Asynchronously starts the external process using the specified configuration.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A task representing the asynchronous operation. The result contains the buffered process result when the method completes.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Starts the external process asynchronously using the specified configuration.
    /// </summary>
    /// <param name="configuration">The configuration settings for starting the external process.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A task representing the asynchronous operation. The result contains the buffered process result when the method completes.</returns>
    Task StartAsync(ProcessConfiguration configuration, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously waits for the process to exit or a specified timeout period elapses.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the buffered process result when the method completes.</returns>
    Task<ProcessResult> WaitForExitOrTimeoutAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously waits for the external process to exit or a specified timeout period elapses.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the buffered process result when the method completes.</returns>
    Task<BufferedProcessResult> WaitForBufferedExitOrTimeoutAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Terminates the associated external process based on the specified exit configuration.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid value is provided for ExitConfiguration.TimeoutPolicy.CancellationMode.</exception>
    Task Kill();
}