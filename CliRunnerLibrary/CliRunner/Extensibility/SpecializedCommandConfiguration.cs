﻿/*
    CliRunner 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CliRunner.Abstractions;

namespace CliRunner.Extensibility;

public abstract class SpecializedCommandConfiguration : ICommandConfiguration
{
    /// <summary>
    /// Initializes a new instance of the Specialized Command Configuration class.
    /// </summary>
    /// <param name="targetFilePath">The path to the command executable file.</param>
    /// <param name="arguments">The arguments to be passed to the command.</param>
    /// <param name="workingDirectoryPath">The working directory for the command.</param>
    /// <param name="requiresAdministrator">Indicates whether the command requires administrator privileges.</param>
    /// <param name="environmentVariables">A dictionary of environment variables to be set for the command.</param>
    /// <param name="credentials">The user credentials to be used when running the command.</param>
    /// <param name="commandResultValidation">The validation criteria for the command result.</param>
    /// <param name="standardInput">The stream for the standard input.</param>
    /// <param name="standardOutput">The stream for the standard output.</param>
    /// <param name="standardError">The stream for the standard error.</param>
    /// <param name="standardInputEncoding">The encoding for the standard input stream.</param>
    /// <param name="standardOutputEncoding">The encoding for the standard output stream.</param>
    /// <param name="standardErrorEncoding">The encoding for the standard error stream.</param>
    /// <param name="processorAffinity">The processor affinity for the command.</param>
    /// <param name="useShellExecute">Indicates whether to use the shell to execute the command.</param>
    /// <param name="windowCreation">Indicates whether to create a new window for the command.</param>
    public SpecializedCommandConfiguration(string targetFilePath, string arguments = null,
        string workingDirectoryPath = null, bool requiresAdministrator = false,
        IReadOnlyDictionary<string, string> environmentVariables = null, UserCredentials credentials = null,
        CommandResultValidation commandResultValidation = CommandResultValidation.ExitCodeZero,
        StreamWriter standardInput = null, StreamReader standardOutput = null, StreamReader standardError = null,
        Encoding standardInputEncoding = default, Encoding standardOutputEncoding = default,
        Encoding standardErrorEncoding = default, IntPtr processorAffinity = default(IntPtr),
        bool useShellExecute = false, bool windowCreation = false)
    {
        TargetFilePath = targetFilePath;
        Arguments = arguments;
        WorkingDirectoryPath = workingDirectoryPath;
        RequiresAdministrator = requiresAdministrator;
        EnvironmentVariables = environmentVariables;
        Credentials = credentials;
        ResultValidation = commandResultValidation;
        UseShellExecution = useShellExecute;
        WindowCreation = windowCreation;
        
        StandardInput = standardInput ?? StreamWriter.Null;
        StandardOutput = standardOutput ?? StreamReader.Null;
        StandardError = standardError ?? StreamReader.Null;
        
        StandardInputEncoding = standardInputEncoding ?? Encoding.Default;
        StandardOutputEncoding = standardOutputEncoding ?? Encoding.Default;
        StandardErrorEncoding = standardErrorEncoding ?? Encoding.Default;
        
#pragma warning disable CA1416
        ProcessorAffinity = processorAffinity;
#pragma warning restore CA1416
    }

    public bool RequiresAdministrator { get; }
    public string TargetFilePath { get; }
    public string WorkingDirectoryPath { get; }
    public string Arguments { get; }
    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }
    public UserCredentials Credentials { get; }
    public CommandResultValidation ResultValidation { get; }
    public StreamWriter StandardInput { get; }
    public StreamReader StandardOutput { get; }
    public StreamReader StandardError { get; }
    public bool UseShellExecution { get; }
    public bool WindowCreation { get; }
    public Encoding StandardInputEncoding { get; }
    public Encoding StandardOutputEncoding { get; }
    public Encoding StandardErrorEncoding { get; }
    public IntPtr ProcessorAffinity { get; }
}