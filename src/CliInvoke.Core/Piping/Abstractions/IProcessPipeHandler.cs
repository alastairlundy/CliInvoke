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

namespace AlastairLundy.CliInvoke.Core.Piping.Abstractions
{
    /// <summary>
    /// An interface to allow for a standardized way of Process pipe handling.
    /// </summary>
    public interface IProcessPipeHandler
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        Task<Process> PipeStandardInputAsync(Stream source, Process destination);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        Task<Stream> PipeStandardOutputAsync(Process source);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        Task<Stream> PipeStandardErrorAsync(Process source);
    }
}