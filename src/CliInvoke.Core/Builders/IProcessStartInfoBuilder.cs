/*
CliInvoke
Copyright (C) 2024-2025  Alastair Lundy

This Source Code Form is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using AlastairLundy.CliInvoke.Core.Primitives;


namespace AlastairLundy.CliInvoke.Core.Builders;

/// <summary>
/// An interface that defines fluent builder methods for configuring Process Start Information. 
/// </summary>
public interface IProcessStartInfoBuilder
{
    /// <summary>
    /// Sets the arguments to pass to the executable.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the Process.</param>
    /// <returns>The updated IProcessStartInfoBuilder object with the specified arguments.</returns>
    IProcessStartInfoBuilder WithArguments(IEnumerable<string> arguments);
    
    /// <summary>
    /// Sets the arguments to pass to the executable.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the executable.</param>
    /// <param name="escapeArguments">Whether to escape the arguments if escape characters are detected.</param>
    /// <returns>The new IProcessStartInfoBuilder object with the specified arguments.</returns>
    IProcessStartInfoBuilder WithArguments(IEnumerable<string> arguments, bool escapeArguments);

    /// <summary>
    /// Sets the arguments to pass to the executable.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the executable.</param>
    /// <returns>The new IProcessStartInfoBuilder object with the specified arguments.</returns>
    IProcessStartInfoBuilder WithArguments(string arguments);
    
    /// <summary>
    /// Sets the Target File Path of the Process Executable.
    /// </summary>
    /// <param name="targetFilePath">The target file path of the Process.</param>
    /// <returns>The ProcessStartInfoBuilder with the updated Target File Path.</returns>
    IProcessStartInfoBuilder WithTargetFile(string targetFilePath);
    
    /// <summary>
    /// Sets the environment variables to be configured.
    /// </summary>
    /// <param name="environmentVariables">The environment variables to be configured.</param>
    /// <returns>The new ProcessStartInfoBuilder with the specified environment variables.</returns>
    IProcessStartInfoBuilder WithEnvironmentVariables(IReadOnlyDictionary<string, string> environmentVariables);
    
    /// <summary>
    /// Sets whether to execute the Process with Administrator Privileges.
    /// </summary>
    /// <param name="runAsAdministrator">Whether to execute the Process with Administrator Privileges.</param>
    /// <returns>The new ProcessStartInfoBuilder with the specified Administrator Privileges settings.</returns>
    IProcessStartInfoBuilder WithAdministratorPrivileges(bool runAsAdministrator);

    /// <summary>
    /// Sets the working directory to be used for the Process.
    /// </summary>
    /// <param name="workingDirectoryPath">The working directory to be used.</param>
    /// <returns>The new ProcessStartInfoBuilder with the specified working directory.</returns>
    IProcessStartInfoBuilder WithWorkingDirectory(string workingDirectoryPath);
    
    /// <summary>
    /// Sets the specified Credentials to be used.
    /// </summary>
    /// <param name="credentials">The credentials to be used.</param>
    /// <returns>The new ProcessStartInfoBuilder with the specified Credentials.</returns>
    IProcessStartInfoBuilder WithUserCredential(UserCredential credentials);

    /// <summary>
    /// Sets the credentials for the Process to be executed.
    /// </summary>
    /// <param name="configure">The CredentialsBuilder configuration.</param>
    /// <returns>The new ProcessStartInfoBuilder with the specified Credentials.</returns>
    IProcessStartInfoBuilder WithUserCredential(Action<IUserCredentialBuilder> configure);

    /// <summary>
    /// Sets the Standard Input Pipe source.
    /// </summary>
    /// <param name="source">The source to use for the Standard Input pipe.</param>
    /// <returns>The new ProcessStartInfoBuilder with the specified Standard Input pipe source.</returns>
    IProcessStartInfoBuilder WithStandardInputPipe(StreamWriter source);
    
    /// <summary>
    /// Sets the Standard Output Pipe target.
    /// </summary>
    /// <param name="target">The target to send the Standard Output to.</param>
    /// <returns>The new ProcessStartInfoBuilder with the specified Standard Output Pipe Target.</returns>
    IProcessStartInfoBuilder WithStandardOutputPipe(StreamReader target);
    
    /// <summary>
    /// Sets the Standard Error Pipe target.
    /// </summary>
    /// <param name="target">The target to send the Standard Error to.</param>
    /// <returns>The new ProcessStartInfoBuilder with the specified Standard Error Pipe Target.</returns>
    IProcessStartInfoBuilder WithStandardErrorPipe(StreamReader target);
    
    /// <summary>
    /// Sets the Process Resource Policy to be used for this Process.
    /// </summary>
    /// <param name="processResourcePolicy">The process resource policy to use.</param>
    /// <returns>The new ProcessStartInfoBuilder with the specified Process Resource Policy.</returns>
    IProcessStartInfoBuilder WithProcessResourcePolicy(ProcessResourcePolicy processResourcePolicy);
    
    /// <summary>
    /// Enables or disables Process execution via Shell Execution.
    /// </summary>
    /// <param name="useShellExecution">Whether to enable or disable shell execution.</param>
    /// <returns>The new ProcessStartInfoBuilder with the specified shell execution behaviour.</returns>
    IProcessStartInfoBuilder WithShellExecution(bool useShellExecution);
    
    /// <summary>
    /// Enables or disables Window creation for the wrapped executable.
    /// </summary>
    /// <param name="enableWindowCreation">Whether to enable or disable window creation for the wrapped executable.</param>
    /// <returns>The new ProcessStartInfoBuilder with the specified window creation behaviour.</returns>
    IProcessStartInfoBuilder WithWindowCreation(bool enableWindowCreation);

    /// <summary>
    /// Sets the Encoding types to be used for Standard Input, Output, and Error.
    /// </summary>
    /// <param name="standardInputEncoding">The encoding type to be used for the Standard Input.</param>
    /// <param name="standardOutputEncoding">The encoding type to be used for the Standard Output.</param>
    /// <param name="standardErrorEncoding">The encoding type to be used for the Standard Error.</param>
    /// <returns>The new IProcessStartInfoBuilder with the specified Pipe Encoding types.</returns>
    IProcessStartInfoBuilder WithEncoding(Encoding? standardInputEncoding = null,
        Encoding? standardOutputEncoding = null,
        Encoding? standardErrorEncoding = null);

    /// <summary>
    /// Builds the Process configuration with the configured parameters.
    /// </summary>
    /// <returns>The newly configured Process configuration.</returns>
    ProcessStartInfo Build();
}