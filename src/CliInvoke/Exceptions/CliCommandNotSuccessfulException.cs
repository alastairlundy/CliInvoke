/*
    CliInvoke 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

#if NET5_0_OR_GREATER
    #nullable enable
#endif

using System;

using AlastairLundy.CliInvoke.Core.Primitives;

using AlastairLundy.CliInvoke.Internal.Localizations;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace AlastairLundy.CliInvoke.Exceptions
{
    /// <summary>
    /// Represents errors that occur if a Command being executed by CliInvoke is unsuccessful.
    /// </summary>
    public sealed class CliCommandNotSuccessfulException : Exception
    {

#if NET5_0_OR_GREATER
        /// <summary>
        /// The command that was executed.
        /// </summary>
        public CliCommandConfiguration? ExecutedCliCommand { get; private set; }
#endif
        /// <summary>
        /// The exit code of the Command that was executed.
        /// </summary>
        public int ExitCode { get; private set; }
        
        /// <summary>
        /// Thrown when a Command that was executed exited with a non-zero exit code.
        /// </summary>
        /// <param name="exitCode">The exit code of the Command that was executed.</param>
        public CliCommandNotSuccessfulException(int exitCode) : base(Resources.Exceptions_CommandNotSuccessful_Generic.Replace("{x}", exitCode.ToString()))
        {
            ExitCode = exitCode;
            
#if NET5_0_OR_GREATER
            ExecutedCliCommand = null;
#endif
        }

        /// <summary>
        /// Thrown when a Command that was executed exited with a non-zero exit code.
        /// </summary>
        /// <param name="exitCode">The exit code of the Command that was executed.</param>
        /// <param name="command">The command that was executed.</param>
        public CliCommandNotSuccessfulException(int exitCode, CliCommandConfiguration command) : base(Resources.Exceptions_CommandNotSuccessful_Specific.Replace("{y}", exitCode.ToString()
            .Replace("{x}", command.TargetFilePath)))
        {
#if NET5_0_OR_GREATER
            ExecutedCliCommand = command;
            Source = ExecutedCliCommand.TargetFilePath;
#endif
            
            ExitCode = exitCode;
        }
    }
}