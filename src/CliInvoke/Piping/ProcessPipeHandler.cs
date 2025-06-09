/*
    AlastairLundy.CliInvoke  
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using AlastairLundy.CliInvoke.Core.Abstractions.Piping;

namespace AlastairLundy.CliInvoke.Piping;

/// <summary>
/// 
/// </summary>
public class ProcessPipeHandler : IProcessPipeHandler
{
    /// <summary>
    /// Asynchronously copies the Stream to the process' standard input.
    /// </summary>
    /// <param name="source">The Stream to be copied from.</param>
    /// <param name="destination">The process to be copied to</param>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
#endif 
    public async Task PipeStandardInputAsync(Stream source, Process destination)
    {
        if (destination.StartInfo.RedirectStandardInput && destination.StandardInput != StreamWriter.Null)
        {
            await destination.StandardInput.FlushAsync();
            destination.StandardInput.BaseStream.Position = 0;
            await source.CopyToAsync(destination.StandardInput.BaseStream); 
        }
    }

    /// <summary>
    /// Asynchronously copies the process' Standard Output to a Stream.
    /// </summary>
    /// <param name="source">The process to be copied from.</param>
    /// <param name="destination">The Stream to be copied to</param>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
#endif 
    public async Task PipeStandardOutputAsync(Process source, Stream destination)
    {
        if (source.StartInfo.RedirectStandardOutput)
        {
            if (source.StandardOutput != StreamReader.Null)
            {
                await source.StandardOutput.BaseStream.CopyToAsync(destination);
            }
        }
    }

    /// <summary>
    /// Asynchronously copies the process' Standard Error to a Stream.
    /// </summary>
    /// <param name="source">The process to be copied from.</param>
    /// <param name="destination">The Stream to be copied to</param>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
#endif 
    public async Task PipeStandardErrorAsync(Process source, Stream destination)
    {
        if (source.StartInfo.RedirectStandardError)
        {
            if (source.StandardError != StreamReader.Null)
            {
                await source.StandardError.BaseStream.CopyToAsync(destination);
            }
        }
    }
}