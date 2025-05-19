/*
    CliInvoke.Extensibility
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Policies;
using AlastairLundy.CliInvoke.Core.Primitives.Results;

// ReSharper disable UnusedType.Global

#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#nullable enable
#endif

namespace AlastairLundy.CliInvoke.Extensibility.Abstractions;
    /// <summary>
    /// An abstract class that implements ICommandConfiguration and adds a default constructor.
    /// </summary>
    /// /// <remarks>Do not use this class directly unless you are creating a specialized Command,
    /// such as one that will be run through an intermediary process like Powershell or Cmd.</remarks>
    [Obsolete]
    public abstract class SpecializedCliCommandConfiguration : CliCommandConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the Specialized Command Configuration class.
        /// </summary>
        /// <param name="targetFilePath">The path to the command executable file.</param>
        /// <param name="arguments">The arguments to be passed to the command.</param>
        /// <param name="workingDirectoryPath">The working directory for the command.</param>
        /// <param name="requiresAdministrator">Indicates whether the command requires administrator privileges.</param>
        /// <param name="environmentVariables">A dictionary of environment variables to be set for the command.</param>
        /// <param name="credential">The user credentials to be used when running the command.</param>
        /// <param name="commandResultValidation">The validation criteria for the command result.</param>
        /// <param name="standardInput">The stream for the standard input.</param>
        /// <param name="standardOutput">The stream for the standard output.</param>
        /// <param name="standardError">The stream for the standard error.</param>
        /// <param name="standardInputEncoding">The encoding for the standard input stream.</param>
        /// <param name="standardOutputEncoding">The encoding for the standard output stream.</param>
        /// <param name="standardErrorEncoding">The encoding for the standard error stream.</param>
        /// <param name="processResourcePolicy">The Process Resource Policy to be used for the command.</param>
        /// <param name="useShellExecution">Indicates whether to use the shell to execute the command.</param>
        /// <param name="windowCreation">Indicates whether to create a new window for the command.</param>
        /// <remarks>Do not use directly unless you are creating a specialized Command, such as one that will be run through an intermediary like Powershell or Cmd.</remarks>
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        [Obsolete]
        public SpecializedCliCommandConfiguration(string targetFilePath, string? arguments = null,
            string? workingDirectoryPath = null, bool requiresAdministrator = false,
            IReadOnlyDictionary<string, string>? environmentVariables = null, UserCredential? credential = null,
            ProcessResultValidation commandResultValidation = ProcessResultValidation.ExitCodeZero,
            StreamWriter? standardInput = null, StreamReader? standardOutput = null, StreamReader? standardError = null,
            Encoding? standardInputEncoding = null, Encoding? standardOutputEncoding = null,
            Encoding? standardErrorEncoding = null, ProcessResourcePolicy? processResourcePolicy = null,
            bool useShellExecution = false, bool windowCreation = false) :
            base(targetFilePath, arguments,
            workingDirectoryPath, requiresAdministrator, environmentVariables, credential, commandResultValidation,
            standardInput, standardOutput, standardError, standardInputEncoding, standardOutputEncoding,
            standardErrorEncoding, processResourcePolicy, windowCreation, useShellExecution)
        {
            TargetFilePath = targetFilePath;
        
            Arguments = arguments ?? string.Empty;
            WorkingDirectoryPath = workingDirectoryPath ?? Environment.CurrentDirectory;
            EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>();
        
            RequiresAdministrator = requiresAdministrator;

            if (credential is not null)
            {
                Credential = credential;
            }
            ResultValidation = commandResultValidation;
            UseShellExecution = useShellExecution;
            WindowCreation = windowCreation;
        
            StandardInput = standardInput ?? StreamWriter.Null;
            StandardOutput = standardOutput ?? StreamReader.Null;
            StandardError = standardError ?? StreamReader.Null;
        
            StandardInputEncoding = standardInputEncoding ?? Encoding.Default;
            StandardOutputEncoding = standardOutputEncoding ?? Encoding.Default;
            StandardErrorEncoding = standardErrorEncoding ?? Encoding.Default;

            if (processResourcePolicy is not null)
            {
                ResourcePolicy = processResourcePolicy;
            }
        }
}