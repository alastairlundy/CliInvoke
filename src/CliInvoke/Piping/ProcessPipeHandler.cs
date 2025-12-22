/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Piping;

namespace CliInvoke.Piping;

/// <summary>
/// An implementation of IProcessPipeHandler. Pipes Process Standard Input, Output, and Error as required.
/// </summary>
public class ProcessPipeHandler : IProcessPipeHandler
{
    /// <summary>
    /// Asynchronously pipes the standard input from a source stream to a specified process.
    /// </summary>
    /// <param name="source">The stream from which to read the standard input data.</param>
    /// <param name="destination">The process to which the standard input will be piped.</param>
    /// <returns>A task that represents the asynchronous operation, containing the destination process.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<bool> PipeStandardInputAsync(Stream source, Process destination)
    {
        if (destination.StartInfo.RedirectStandardInput)
        {
            await destination.StandardInput.FlushAsync();
            destination.StandardInput.BaseStream.Position = 0;
            await source.CopyToAsync(destination.StandardInput.BaseStream);

            return source.Equals(destination.StandardInput.BaseStream);
        }

        return false;
    }

    /// <summary>
    /// Asynchronously retrieves the standard output stream from a specified process.
    /// </summary>
    /// <param name="source">The process from which to read the standard output data.</param>
    /// <returns>A task that represents the asynchronous operation, containing the standard output stream.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<Stream> PipeStandardOutputAsync(Process source)
    {
        Stream destination = new MemoryStream();

        if (source.StartInfo.RedirectStandardOutput)
        {
            if (source.StandardOutput != StreamReader.Null)
            {
                await source.StandardOutput.BaseStream.CopyToAsync(destination);
            }
        }

        return destination;
    }

    /// <summary>
    /// Asynchronously retrieves the standard error stream from a specified process.
    /// </summary>
    /// <param name="source">The process from which to read the standard error data.</param>
    /// <returns>A task that represents the asynchronous operation, containing the standard error stream.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<Stream> PipeStandardErrorAsync(Process source)
    {
        Stream destination = new MemoryStream();

        if (source.StartInfo.RedirectStandardError)
        {
            if (source.StandardError != StreamReader.Null)
            {
                await source.StandardError.BaseStream.CopyToAsync(destination);
            }
        }

        return destination;
    }
}
