/*
    AlastairLundy.CliInvoke.Core
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

#if NET5_0_OR_GREATER
#nullable enable
#endif

using System;
using System.Diagnostics;

using AlastairLundy.CliInvoke.Internal.Localizations;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable MemberCanBePrivate.Global

namespace AlastairLundy.CliInvoke.Exceptions;

/// <summary>
/// 
/// </summary>
public sealed class ProcessNotSuccessfulException : Exception
{

    /// <summary>
    /// The command that was executed.
    /// </summary>
    public Process? ExecutedProcess { get; private set; }

    /// <summary>
    /// The exit code of the Command that was executed.
    /// </summary>
    public int ExitCode { get; private set; }
        
    /// <summary>
    /// Thrown when an executed Process exited with a non-zero exit code.
    /// </summary>
    /// <param name="exitCode">The exit code of the Process that was executed.</param>
    public ProcessNotSuccessfulException(int exitCode) : base(Resources.Exceptions_ProcessNotSuccessful_Generic.Replace("{x}", exitCode.ToString()))
    {
        ExitCode = exitCode;
            
        ExecutedProcess = null;
    }

    /// <summary>
    /// Thrown when an executed Process exited with a non-zero exit code.
    /// </summary>
    /// <param name="exitCode">The exit code of the Process that was executed.</param>
    /// <param name="process">The Process that was executed.</param>
    public ProcessNotSuccessfulException(int exitCode, Process process) : base(Resources.Exceptions_ProcessNotSuccessful_Specific.Replace("{y}", exitCode.ToString()
        .Replace("{x}", process.StartInfo.FileName)))
    {
        ExecutedProcess = process;

        if (ExecutedProcess is not null)
        {
            Source = ExecutedProcess.StartInfo.FileName;
        }
            
        ExitCode = exitCode;
    }
}