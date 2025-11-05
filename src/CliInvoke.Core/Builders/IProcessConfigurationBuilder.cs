/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AlastairLundy.CliInvoke.Core.Builders;

/// <summary>
/// An interface that defines fluent builder methods for configuring a Process Configuration.
/// </summary>
public interface IProcessConfigurationBuilder
{
    /// <summary>
    /// Sets the arguments to pass to the executable.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the Process.</param>
    /// <returns>The updated IProcessConfigurationBuilder object with the specified arguments.</returns>
    IProcessConfigurationBuilder SetArguments(IEnumerable<string> arguments);

    /// <summary>
    /// Sets the arguments to pass to the executable.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the executable.</param>
    /// <param name="escapeArguments">Whether to escape the arguments if escape characters are detected.</param>
    /// <returns>The new IProcessConfigurationBuilder object with the specified arguments.</returns>
    IProcessConfigurationBuilder SetArguments(IEnumerable<string> arguments, bool escapeArguments);

    /// <summary>
    /// Sets the arguments to pass to the executable.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the executable.</param>
    /// <returns>The new IProcessConfigurationBuilder object with the specified arguments.</returns>
    IProcessConfigurationBuilder SetArguments(string arguments);

    /// <summary>
    /// Sets the Target File Path of the Process Executable.
    /// </summary>
    /// <param name="targetFilePath">The target file path of the Process.</param>
    /// <returns>The ProcessConfigurationBuilder with the updated Target File Path.</returns>
    IProcessConfigurationBuilder SetTargetFilePath(string targetFilePath);

    /// <summary>
    /// Sets the environment variables to be configured.
    /// </summary>
    /// <param name="environmentVariables">The environment variables to be configured.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified environment variables.</returns>
    IProcessConfigurationBuilder SetEnvironmentVariables(
        IReadOnlyDictionary<string, string> environmentVariables
    );

    /// <summary>
    /// Enables using Administrator Privileges.
    /// </summary>
    /// <returns>The new ProcessConfigurationBuilder with the specified Administrator Privileges settings.</returns>
    IProcessConfigurationBuilder RequireAdministratorPrivileges();

    /// <summary>
    /// Sets the working directory to be used for the Process.
    /// </summary>
    /// <param name="workingDirectoryPath">The working directory to be used.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified working directory.</returns>
    IProcessConfigurationBuilder SetWorkingDirectory(string workingDirectoryPath);

    /// <summary>
    /// Sets the specified Credentials to be used.
    /// </summary>
    /// <param name="credentials">The credentials to be used.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified Credentials.</returns>
    IProcessConfigurationBuilder SetUserCredential(UserCredential credentials);

    /// <summary>
    /// Sets the credentials for the Process to be executed.
    /// </summary>
    /// <param name="configure">The CredentialsBuilder configuration.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified Credentials.</returns>
    IProcessConfigurationBuilder SetUserCredential(Action<IUserCredentialBuilder> configure);

    /// <summary>
    /// Configures whether the standard input of the process should be redirected.
    /// </summary>
    /// <param name="redirectStandardInput">A value that specifies whether the standard input of the process should be redirected.</param>
    /// <returns>The updated IProcessConfigurationBuilder instance with the configured standard input redirection setting.</returns>
    IProcessConfigurationBuilder RedirectStandardInput(bool redirectStandardInput);

    /// <summary>
    /// Configures whether the standard output of the process should be redirected.
    /// </summary>
    /// <param name="redirectStandardOutput">A boolean value indicating whether to redirect the standard output.</param>
    /// <returns>The updated IProcessConfigurationBuilder object with the specified standard output redirection setting.</returns>
    IProcessConfigurationBuilder RedirectStandardOutput(bool redirectStandardOutput);

    /// <summary>
    /// Configures whether to redirect the standard error stream of the process.
    /// </summary>
    /// <param name="redirectStandardError">A boolean value indicating whether the standard error stream should be redirected.</param>
    /// <returns>The updated IProcessConfigurationBuilder object with the specified standard error redirection setting.</returns>
    IProcessConfigurationBuilder RedirectStandardError(bool redirectStandardError);

    /// <summary>
    /// Sets the Standard Input Pipe source.
    /// </summary>
    /// <param name="source">The source to use for the Standard Input pipe.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified Standard Input pipe source.</returns>
    IProcessConfigurationBuilder SetStandardInputPipe(StreamWriter source);

    /// <summary>
    /// Sets the Standard Output Pipe target.
    /// </summary>
    /// <param name="target">The target to send the Standard Output to.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified Standard Output Pipe Target.</returns>
    IProcessConfigurationBuilder SetStandardOutputPipe(StreamReader target);

    /// <summary>
    /// Sets the Standard Error Pipe target.
    /// </summary>
    /// <param name="target">The target to send the Standard Error to.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified Standard Error Pipe Target.</returns>
    IProcessConfigurationBuilder SetStandardErrorPipe(StreamReader target);

    /// <summary>
    /// Sets the Process Resource Policy to be used for this Process.
    /// </summary>
    /// <param name="processResourcePolicy">The process resource policy to use.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified Process Resource Policy.</returns>
    IProcessConfigurationBuilder SetProcessResourcePolicy(
        ProcessResourcePolicy processResourcePolicy
    );

    /// <summary>
    /// Enables or disables Process execution via Shell Execution.
    /// </summary>
    /// <param name="useShellExecution">Whether to enable or disable shell execution.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified shell execution behaviour.</returns>
    IProcessConfigurationBuilder ConfigureShellExecution(bool useShellExecution);

    /// <summary>
    /// Enables or disables Window creation for the wrapped executable.
    /// </summary>
    /// <param name="enableWindowCreation">Whether to enable or disable window creation for the wrapped executable.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified window creation behaviour.</returns>
    IProcessConfigurationBuilder ConfigureWindowCreation(bool enableWindowCreation);

    /// <summary>
    /// Sets the Encoding types to be used for Standard Input.
    /// </summary>
    /// <param name="standardInputEncoding">The encoding type to be used for the Standard Input.</param>
    /// <returns>The new IProcessConfigurationBuilder with the specified Pipe Encoding types.</returns>
    IProcessConfigurationBuilder SetStandardInputEncoding(Encoding? standardInputEncoding = null);

    /// <summary>
    /// Sets the Encoding types to be used for Standard Output.
    /// </summary>
    /// <param name="standardOutputEncoding">The encoding type to be used for the Standard Output.</param>
    /// <returns>The new IProcessConfigurationBuilder with the specified Pipe Encoding types.</returns>
    IProcessConfigurationBuilder SetStandardOutputEncoding(Encoding? standardOutputEncoding = null);

    /// <summary>
    /// Sets the Encoding types to be used for Standard Error.
    /// </summary>
    /// <param name="standardErrorEncoding">The encoding type to be used for the Standard Error.</param>
    /// <returns>The new IProcessConfigurationBuilder with the specified Pipe Encoding types.</returns>
    IProcessConfigurationBuilder SetStandardErrorEncoding(Encoding? standardErrorEncoding = null);

    /// <summary>
    /// Builds the Process configuration with the configured parameters.
    /// </summary>
    /// <returns>The newly configured Process configuration.</returns>
    ProcessConfiguration Build();
}
