/*
    AlastairLundy.CliInvoke.Core
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AlastairLundy.CliInvoke.Core.Piping;

/// <summary>
/// An interface to allow for a standardized way of Process pipe handling.
/// </summary>
public interface IProcessPipeHandler
{
    /// <summary>
    /// Asynchronously pipes the standard input from a source stream to a specified process.
    /// </summary>
    /// <param name="source">The stream from which to read the standard input data.</param>
    /// <param name="destination">The process to which the standard input will be piped.</param>
    /// <returns>A task that represents the asynchronous operation, containing the destination process.</returns>
    Task<bool> PipeStandardInputAsync(Stream source, Process destination);

    /// <summary>
    /// Asynchronously retrieves the standard output stream from a specified process.
    /// </summary>
    /// <param name="source">The process from which to read the standard output data.</param>
    /// <returns>A task that represents the asynchronous operation, containing the standard output stream.</returns>
    Task<Stream> PipeStandardOutputAsync(Process source);

    /// <summary>
    /// Asynchronously retrieves the standard error stream from a specified process.
    /// </summary>
    /// <param name="source">The process from which to read the standard error data.</param>
    /// <returns>A task that represents the asynchronous operation, containing the standard error stream.</returns>
    Task<Stream> PipeStandardErrorAsync(Process source);
}
