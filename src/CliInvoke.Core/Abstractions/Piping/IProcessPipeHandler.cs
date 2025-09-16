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

namespace AlastairLundy.CliInvoke.Core.Abstractions.Piping;

/// <summary>
/// An interface to allow for a standardised way of Process pipe handling.
/// </summary>
public interface IProcessPipeHandler
{
    /// <summary>
    /// Asynchronously copies the Stream to the process' standard input.
    /// </summary>
    /// <param name="source">The Stream to be copied from.</param>
    /// <param name="destination">The process to be copied to</param>
    Task PipeStandardInputAsync(Stream source, Process destination);

    /// <summary>
    /// Asynchronously copies the process' Standard Output to a Stream.
    /// </summary>
    /// <param name="source">The process to be copied from.</param>
    /// <param name="destination">The Stream to be copied to</param>
    Task PipeStandardOutputAsync(Process source, Stream destination);

    /// <summary>
    /// Asynchronously copies the process' Standard Error to a Stream.
    /// </summary>
    /// <param name="source">The process to be copied from.</param>
    /// <param name="destination">The Stream to be copied to</param>
    Task PipeStandardErrorAsync(Process source, Stream destination);
}