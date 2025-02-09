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

namespace CliRunner.Builders.Abstractions;

/// <summary>
/// An interface that defines the fluent builder methods all CommandBuilder classes must implement. 
/// </summary>
public interface ICommandBuilder
{
    /// <summary>
    /// Sets the arguments to pass to the executable.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the Command.</param>
    /// <returns>The updated ICommandBuilder object with the specified arguments.</returns>
    ICommandBuilder WithArguments(IEnumerable<string> arguments);
    
    /// <summary>
    /// Sets the arguments to pass to the executable.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the executable.</param>
    /// <param name="escapeArguments">Whether to escape the arguments if escape characters are detected.</param>
    /// <returns>The new ICommandBuilder object with the specified arguments.</returns>
    ICommandBuilder WithArguments(IEnumerable<string> arguments, bool escapeArguments);

    /// <summary>
    /// Sets the arguments to pass to the executable.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the executable.</param>
    /// <returns>The new ICommandBuilder object with the specified arguments.</returns>
    ICommandBuilder WithArguments(string arguments);
    
    /// <summary>
    /// Sets the Target File Path of the Command Executable.
    /// </summary>
    /// <param name="targetFilePath">The target file path of the Command.</param>
    /// <returns>The Command with the updated Target File Path.</returns>
    ICommandBuilder WithTargetFile(string targetFilePath);
    
    /// <summary>
    /// Sets the environment variables to be configured.
    /// </summary>
    /// <param name="environmentVariables">The environment variables to be configured.</param>
    /// <returns>The new CommandBuilder with the specified environment variables.</returns>
    ICommandBuilder WithEnvironmentVariables(IReadOnlyDictionary<string, string> environmentVariables);

    /// <summary>
    /// Sets the environment variables for the Command to be executed.
    /// </summary>
    /// <param name="configure">The environment variables to be configured</param>
    /// <returns>The new CommandBuilder with the specified environment variables.</returns>
    ICommandBuilder WithEnvironmentVariables(Action<IEnvironmentVariablesBuilder> configure);
    
    /// <summary>
    /// Sets whether to execute the Command with Administrator Privileges.
    /// </summary>
    /// <param name="runAsAdministrator">Whether to execute the Command with Administrator Privileges.</param>
    /// <returns>The new CommandBuilder with the specified Administrator Privileges settings.</returns>
    ICommandBuilder WithAdministratorPrivileges(bool runAsAdministrator);

    /// <summary>
    /// Sets the working directory to be used for the Command.
    /// </summary>
    /// <param name="workingDirectoryPath">The working directory to be used.</param>
    /// <returns>The new CommandBuilder with the specified working directory.</returns>
    ICommandBuilder WithWorkingDirectory(string workingDirectoryPath);
    
    /// <summary>
    /// Sets the specified Credentials to be used.
    /// </summary>
    /// <param name="credentials">The credentials to be used.</param>
    /// <returns>The new CommandBuilder with the specified Credentials.</returns>
    ICommandBuilder WithUserCredential(UserCredential credentials);
    
    /// <summary>
    /// Sets the credentials for the Command to be executed.
    /// </summary>
    /// <param name="configure">The CredentialsBuilder configuration.</param>
    /// <returns>The new CommandBuilder with the specified Credentials.</returns>
    ICommandBuilder WithCredential(Action<UserCredentialBuilder> configure);
    
    /// <summary>
    /// Sets the Result Validation whether to throw an exception or not if the Command does not execute successfully.
    /// </summary>
    /// <param name="validation">The result validation behaviour to be used.</param>
    /// <returns>The new CommandBuilder object with the configured Result Validation behaviour.</returns>
    ICommandBuilder WithValidation(CommandResultValidation validation);
    
    /// <summary>
    /// Sets the Standard Input Pipe source.
    /// </summary>
    /// <param name="source">The source to use for the Standard Input pipe.</param>
    /// <returns>The new CommandBuilder with the specified Standard Input pipe source.</returns>
    ICommandBuilder WithStandardInputPipe(StreamWriter source);
    
    /// <summary>
    /// Sets the Standard Output Pipe target.
    /// </summary>
    /// <param name="target">The target to send the Standard Output to.</param>
    /// <returns>The new CommandBuilder with the specified Standard Output Pipe Target.</returns>
    ICommandBuilder WithStandardOutputPipe(StreamReader target);
    
    /// <summary>
    /// Sets the Standard Error Pipe target.
    /// </summary>
    /// <param name="target">The target to send the Standard Error to.</param>
    /// <returns>The new CommandBuilder with the specified Standard Error Pipe Target.</returns>
    ICommandBuilder WithStandardErrorPipe(StreamReader target);
    
    /// <summary>
    /// Sets the Process Resource Policy to be used for this command.
    /// </summary>
    /// <param name="processResourcePolicy">The process resource policy to use.</param>
    /// <returns>The new CommandBuilder with the specified Process Resource Policy.</returns>
    ICommandBuilder WithProcessResourcePolicy(ProcessResourcePolicy processResourcePolicy);
    
    /// <summary>
    /// Enables or disables command execution via Shell Execution.
    /// </summary>
    /// <param name="useShellExecution">Whether to enable or disable shell execution.</param>
    /// <returns>The new CommandBuilder with the specified shell execution behaviour.</returns>
    ICommandBuilder WithShellExecution(bool useShellExecution);
    
    /// <summary>
    /// Enables or disables Window creation for the wrapped executable.
    /// </summary>
    /// <param name="enableWindowCreation">Whether to enable or disable window creation for the wrapped executable.</param>
    /// <returns>The new CommandBuilder with the specified window creation behaviour.</returns>
    ICommandBuilder WithWindowCreation(bool enableWindowCreation);

    /// <summary>
    /// Sets the Encoding types to be used for Standard Input, Output, and Error.
    /// </summary>
    /// <param name="standardInputEncoding">The encoding type to be used for the Standard Input.</param>
    /// <param name="standardOutputEncoding">The encoding type to be used for the Standard Output.</param>
    /// <param name="standardErrorEncoding">The encoding type to be used for the Standard Error.</param>
    /// <returns>The new ICommandBuilder with the specified Pipe Encoding types.</returns>
    ICommandBuilder WithEncoding(Encoding standardInputEncoding = null,
        Encoding standardOutputEncoding = null,
        Encoding standardErrorEncoding = null);

    /// <summary>
    /// Builds the Command with the configured parameters.
    /// </summary>
    /// <returns>The newly configured Command.</returns>
    Command Build();

    /// <summary>
    /// Builds the ICommandConfiguration with the configured parameters.
    /// </summary>
    /// <returns>The newly configured ICommandConfiguration.</returns>
    ICommandConfiguration BuildConfiguration();
}