/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

// ReSharper disable RedundantBoolCompare
// ReSharper disable NullableWarningSuppressionIsUsed


using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;

using AlastairLundy.CliInvoke.Builders.Abstractions;
using AlastairLundy.CliInvoke.Internal.Localizations;

using AlastairLundy.Extensions.Processes.Abstractions;
using AlastairLundy.Extensions.Processes.Abstractions.Builders;
using AlastairLundy.Extensions.Processes.Builders;


#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace AlastairLundy.CliInvoke.Builders;

/// <summary>
/// A class to build Command Configurations with a Fluent configuration interface. 
/// </summary>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class CliCommandConfigurationBuilder : ICliCommandConfigurationBuilder
{
    private readonly CliCommandConfiguration _commandConfiguration;
    
    /// <summary>
    /// Instantiates the CommandBuilder with a specified target file path of an ICommandConfiguration.
    /// </summary>
    /// <param name="targetFilePath">The target file path of a Command to be executed.</param>
    public CliCommandConfigurationBuilder(string targetFilePath)
    {
        _commandConfiguration = new CliCommandConfiguration(targetFilePath, processResourcePolicy: ProcessResourcePolicy.Default);
    }

    /// <summary>
    /// Instantiates the CommandBuilder with a specified CommandConfiguration configuration
    /// </summary>
    /// <param name="commandConfiguration">The configuration to be used when building the Command.</param>
    public CliCommandConfigurationBuilder(CliCommandConfiguration commandConfiguration)
    {
        _commandConfiguration = commandConfiguration;
    }
    
    /// <summary>
    /// Sets the arguments to pass to the executable.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the Command.</param>
    /// <returns>The updated ICommandBuilder object with the specified arguments.</returns>
    [Pure]
    public ICliCommandConfigurationBuilder WithArguments(IEnumerable<string> arguments) =>
        new CliCommandConfigurationBuilder(
            new CliCommandConfiguration(_commandConfiguration.TargetFilePath,
                string.Join(" ", arguments),
                _commandConfiguration.WorkingDirectoryPath,
                _commandConfiguration.RequiresAdministrator,
                _commandConfiguration.EnvironmentVariables,
                _commandConfiguration.Credential,
                _commandConfiguration.ResultValidation,
                _commandConfiguration.StandardInput,
                _commandConfiguration.StandardOutput,
                _commandConfiguration.StandardError,
                _commandConfiguration.StandardInputEncoding,
                _commandConfiguration.StandardOutputEncoding,
                _commandConfiguration.StandardErrorEncoding,
                _commandConfiguration.ResourcePolicy,
                _commandConfiguration.WindowCreation,
                _commandConfiguration.UseShellExecution));

    /// <summary>
    /// Sets the arguments to pass to the executable.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the executable.</param>
    /// <param name="escapeArguments">Whether to escape the arguments if escape characters are detected.</param>
    /// <returns>The new ICommandBuilder object with the specified arguments.</returns>
    [Pure]
    public ICliCommandConfigurationBuilder WithArguments(IEnumerable<string> arguments, bool escapeArguments)
    {
        IArgumentsBuilder argumentsBuilder = new ArgumentsBuilder();
        argumentsBuilder.Add(arguments, escapeArguments);
        
        string args = argumentsBuilder.ToString();

        return new CliCommandConfigurationBuilder(
            new CliCommandConfiguration(_commandConfiguration.TargetFilePath,
            args,
            _commandConfiguration.WorkingDirectoryPath,
            _commandConfiguration.RequiresAdministrator,
            _commandConfiguration.EnvironmentVariables,
            _commandConfiguration.Credential,
            _commandConfiguration.ResultValidation,
            _commandConfiguration.StandardInput,
            _commandConfiguration.StandardOutput,
            _commandConfiguration.StandardError,
            _commandConfiguration.StandardInputEncoding,
            _commandConfiguration.StandardOutputEncoding,
            _commandConfiguration.StandardErrorEncoding,
            _commandConfiguration.ResourcePolicy,
            _commandConfiguration.WindowCreation,
            _commandConfiguration.UseShellExecution));
    }
    
    /// <summary>
    /// Sets the arguments to pass to the executable.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the executable.</param>
    /// <returns>The new ICommandBuilder object with the specified arguments.</returns>
    [Pure]
    public ICliCommandConfigurationBuilder WithArguments(string arguments) =>
        new CliCommandConfigurationBuilder(
            new CliCommandConfiguration(_commandConfiguration.TargetFilePath,
                arguments,
                _commandConfiguration.WorkingDirectoryPath,
                _commandConfiguration.RequiresAdministrator,
                _commandConfiguration.EnvironmentVariables,
                _commandConfiguration.Credential,
                _commandConfiguration.ResultValidation,
                _commandConfiguration.StandardInput,
                _commandConfiguration.StandardOutput,
                _commandConfiguration.StandardError,
                _commandConfiguration.StandardInputEncoding,
                _commandConfiguration.StandardOutputEncoding,
                _commandConfiguration.StandardErrorEncoding,
                _commandConfiguration.ResourcePolicy,
                _commandConfiguration.WindowCreation,
                _commandConfiguration.UseShellExecution));

    /// <summary>
    /// Sets the Target File Path of the Command Executable.
    /// </summary>
    /// <param name="targetFilePath">The target file path of the Command.</param>
    /// <returns>The Command with the updated Target File Path.</returns>
    [Pure]
    public ICliCommandConfigurationBuilder WithTargetFile(string targetFilePath) =>
        new CliCommandConfigurationBuilder(
            new CliCommandConfiguration(targetFilePath,
                _commandConfiguration.Arguments,
                _commandConfiguration.WorkingDirectoryPath,
                _commandConfiguration.RequiresAdministrator,
                _commandConfiguration.EnvironmentVariables,
                _commandConfiguration.Credential,
                _commandConfiguration.ResultValidation,
                _commandConfiguration.StandardInput,
                _commandConfiguration.StandardOutput,
                _commandConfiguration.StandardError,
                _commandConfiguration.StandardInputEncoding,
                _commandConfiguration.StandardOutputEncoding,
                _commandConfiguration.StandardErrorEncoding,
                _commandConfiguration.ResourcePolicy,
                _commandConfiguration.WindowCreation,
                _commandConfiguration.UseShellExecution));
    
    /// <summary>
    /// Sets the environment variables to be configured.
    /// </summary>
    /// <param name="environmentVariables">The environment variables to be configured.</param>
    /// <returns>The new CommandBuilder with the specified environment variables.</returns>
    [Pure]
    public ICliCommandConfigurationBuilder WithEnvironmentVariables(IReadOnlyDictionary<string, string> environmentVariables) =>
        new CliCommandConfigurationBuilder(
            new CliCommandConfiguration(_commandConfiguration.TargetFilePath,
                _commandConfiguration.Arguments,
                _commandConfiguration.WorkingDirectoryPath,
                _commandConfiguration.RequiresAdministrator,
                environmentVariables,
                _commandConfiguration.Credential,
                _commandConfiguration.ResultValidation,
                _commandConfiguration.StandardInput,
                _commandConfiguration.StandardOutput,
                _commandConfiguration.StandardError,
                _commandConfiguration.StandardInputEncoding,
                _commandConfiguration.StandardOutputEncoding,
                _commandConfiguration.StandardErrorEncoding,
                _commandConfiguration.ResourcePolicy,
                _commandConfiguration.WindowCreation,
                _commandConfiguration.UseShellExecution));

    /// <summary>
    /// Sets the environment variables for the Command to be executed.
    /// </summary>
    /// <param name="configure">The environment variables to be configured</param>
    /// <returns>The new CommandBuilder with the specified environment variables.</returns>
    [Pure]
    public ICliCommandConfigurationBuilder WithEnvironmentVariables(Action<IEnvironmentVariablesBuilder> configure)
    {
        IEnvironmentVariablesBuilder environmentVariablesBuilder = new EnvironmentVariablesBuilder()
            .Set(_commandConfiguration.EnvironmentVariables);

        configure(environmentVariablesBuilder);

        return WithEnvironmentVariables(environmentVariablesBuilder.Build());
    }

    /// <summary>
    /// Sets whether to execute the Command with Administrator Privileges.
    /// </summary>
    /// <param name="runAsAdministrator">Whether to execute the Command with Administrator Privileges.</param>
    /// <returns>The new CommandBuilder with the specified Administrator Privileges settings.</returns>
    [Pure]
    public ICliCommandConfigurationBuilder WithAdministratorPrivileges(bool runAsAdministrator) =>
        new CliCommandConfigurationBuilder(
            new CliCommandConfiguration(_commandConfiguration.TargetFilePath,
                _commandConfiguration.Arguments,
                _commandConfiguration.WorkingDirectoryPath,
                runAsAdministrator,
                _commandConfiguration.EnvironmentVariables,
                _commandConfiguration.Credential,
                _commandConfiguration.ResultValidation,
                _commandConfiguration.StandardInput,
                _commandConfiguration.StandardOutput,
                _commandConfiguration.StandardError,
                _commandConfiguration.StandardInputEncoding,
                _commandConfiguration.StandardOutputEncoding,
                _commandConfiguration.StandardErrorEncoding,
                _commandConfiguration.ResourcePolicy,
                _commandConfiguration.WindowCreation,
                _commandConfiguration.UseShellExecution));

    /// <summary>
    /// Sets the working directory to be used for the Command.
    /// </summary>
    /// <param name="workingDirectoryPath">The working directory to be used.</param>
    /// <returns>The new CommandBuilder with the specified working directory.</returns>
    [Pure]
    public ICliCommandConfigurationBuilder WithWorkingDirectory(string workingDirectoryPath) =>
        new CliCommandConfigurationBuilder(
            new CliCommandConfiguration(_commandConfiguration.TargetFilePath,
                _commandConfiguration.Arguments,
                workingDirectoryPath,
                _commandConfiguration.RequiresAdministrator,
                _commandConfiguration.EnvironmentVariables,
                _commandConfiguration.Credential,
                _commandConfiguration.ResultValidation,
                _commandConfiguration.StandardInput,
                _commandConfiguration.StandardOutput,
                _commandConfiguration.StandardError,
                _commandConfiguration.StandardInputEncoding,
                _commandConfiguration.StandardOutputEncoding,
                _commandConfiguration.StandardErrorEncoding,
                _commandConfiguration.ResourcePolicy,
                _commandConfiguration.WindowCreation,
                _commandConfiguration.UseShellExecution));

    /// <summary>
    /// Sets the specified Credentials to be used.
    /// </summary>
    /// <param name="credentials">The credentials to be used.</param>
    /// <returns>The new CommandBuilder with the specified Credentials.</returns>
    /// <remarks>Credentials are only supported with the Process class on Windows. This is a limitation of .NET's Process class.</remarks>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("android")]
#endif
    [Pure]
    public ICliCommandConfigurationBuilder WithUserCredential(UserCredential credentials) =>
        new CliCommandConfigurationBuilder(
            new CliCommandConfiguration(_commandConfiguration.TargetFilePath,
                _commandConfiguration.Arguments,
                _commandConfiguration.WorkingDirectoryPath,
                _commandConfiguration.RequiresAdministrator,
                _commandConfiguration.EnvironmentVariables,
                credentials,
                _commandConfiguration.ResultValidation,
                _commandConfiguration.StandardInput,
                _commandConfiguration.StandardOutput,
                _commandConfiguration.StandardError,
                _commandConfiguration.StandardInputEncoding,
                _commandConfiguration.StandardOutputEncoding,
                _commandConfiguration.StandardErrorEncoding,
                _commandConfiguration.ResourcePolicy,
                _commandConfiguration.WindowCreation,
                _commandConfiguration.UseShellExecution));

    /// <summary>
    /// Sets the credentials for the Command to be executed.
    /// </summary>
    /// <param name="configure">The CredentialsBuilder configuration.</param>
    /// <returns>The new CommandBuilder with the specified Credentials.</returns>
    /// <remarks>Credentials are only supported with the Process class on Windows. This is a limitation of .NET's Process class.</remarks>
    [Pure]
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("android")]
#endif
    public ICliCommandConfigurationBuilder WithUserCredential(Action<IUserCredentialBuilder> configure)
    {
        UserCredential credential;

        if (_commandConfiguration.Credential is null)
        {
            credential = UserCredential.Null;
        }
        else
        {
            credential = _commandConfiguration.Credential;
        }
        
        IUserCredentialBuilder credentialBuilder = new UserCredentialBuilder()
            .SetDomain(credential.Domain!)
            .SetPassword(credential.Password!)
            .SetUsername(credential.UserName!);

        configure(credentialBuilder);

        return WithUserCredential(credentialBuilder.Build());
    }

    /// <summary>
    /// Sets the Result Validation whether to throw an exception or not if the Command does not execute successfully.
    /// </summary>
    /// <param name="validation">The result validation behaviour to be used.</param>
    /// <returns>The new CommandBuilder object with the configured Result Validation behaviour.</returns>
    [Pure]
    public ICliCommandConfigurationBuilder WithValidation(ProcessResultValidation validation) =>
        new CliCommandConfigurationBuilder(
            new CliCommandConfiguration(_commandConfiguration.TargetFilePath,
                _commandConfiguration.Arguments,
                _commandConfiguration.WorkingDirectoryPath,
                _commandConfiguration.RequiresAdministrator,
                _commandConfiguration.EnvironmentVariables,
                _commandConfiguration.Credential,
                validation,
                _commandConfiguration.StandardInput,
                _commandConfiguration.StandardOutput,
                _commandConfiguration.StandardError,
                _commandConfiguration.StandardInputEncoding,
                _commandConfiguration.StandardOutputEncoding,
                _commandConfiguration.StandardErrorEncoding,
                _commandConfiguration.ResourcePolicy,
                _commandConfiguration.WindowCreation,
                _commandConfiguration.UseShellExecution));

    /// <summary>
    /// Sets the Standard Input Pipe source.
    /// </summary>
    /// <param name="source">The source to use for the Standard Input pipe.</param>
    /// <returns>The new CommandBuilder with the specified Standard Input pipe source.</returns>
    /// <remarks>Using Shell Execution whilst also Redirecting Standard Input will throw an Exception. This is a known issue with the System Process class.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardinput"/>
    [Pure]
    public ICliCommandConfigurationBuilder WithStandardInputPipe(StreamWriter source)
    {
        if (_commandConfiguration.UseShellExecution == true && source != StreamWriter.Null)
        {
            throw new ArgumentException(Resources.CommandBuilder_ArgumentConflict_ShellExecution_Input);
        }
        else
        {
            return new CliCommandConfigurationBuilder(
                new CliCommandConfiguration(
                    _commandConfiguration.TargetFilePath,
                    _commandConfiguration.Arguments,
                    _commandConfiguration.WorkingDirectoryPath,
                    _commandConfiguration.RequiresAdministrator,
                    _commandConfiguration.EnvironmentVariables,
                    _commandConfiguration.Credential,
                    _commandConfiguration.ResultValidation,
                    source,
                    _commandConfiguration.StandardOutput,
                    _commandConfiguration.StandardError,
                    _commandConfiguration.StandardInputEncoding,
                    _commandConfiguration.StandardOutputEncoding,
                    _commandConfiguration.StandardErrorEncoding,
                    _commandConfiguration.ResourcePolicy,
                    _commandConfiguration.WindowCreation,
                    _commandConfiguration.UseShellExecution));
        }
    }

    /// <summary>
    /// Sets the Standard Output Pipe target.
    /// </summary>
    /// <param name="target">The target to send the Standard Output to.</param>
    /// <returns>The new CommandBuilder with the specified Standard Output Pipe Target.</returns>
    /// <remarks>Using Shell Execution whilst also Redirecting Standard Output will throw an Exception. This is a known issue with the System Process class.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardoutput"/>
    [Pure]
    public ICliCommandConfigurationBuilder WithStandardOutputPipe(StreamReader target)
    {
        if (_commandConfiguration.UseShellExecution == true && target != StreamReader.Null)
        {
            throw new ArgumentException(Resources.CommandBuilder_ArgumentConflict_ShellExecution_Output);
        }
        else
        {
            return new CliCommandConfigurationBuilder(
                new CliCommandConfiguration(
                    _commandConfiguration.TargetFilePath,
                    _commandConfiguration.Arguments,
                    _commandConfiguration.WorkingDirectoryPath,
                    _commandConfiguration.RequiresAdministrator,
                    _commandConfiguration.EnvironmentVariables,
                    _commandConfiguration.Credential,
                    _commandConfiguration.ResultValidation,
                    _commandConfiguration.StandardInput,
                    target,
                    _commandConfiguration.StandardError,
                    _commandConfiguration.StandardInputEncoding,
                    _commandConfiguration.StandardOutputEncoding,
                    _commandConfiguration.StandardErrorEncoding,
                    _commandConfiguration.ResourcePolicy,
                    _commandConfiguration.WindowCreation,
                    _commandConfiguration.UseShellExecution));
        }
    }

    /// <summary>
    /// Sets the Standard Error Pipe target.
    /// </summary>
    /// <param name="target">The target to send the Standard Error to.</param>
    /// <returns>The new CommandBuilder with the specified Standard Error Pipe Target.</returns>
    /// <remarks>Using Shell Execution whilst also Redirecting Standard Error will throw an Exception. This is a known issue with the System Process class.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandarderror"/>
    [Pure]
    public ICliCommandConfigurationBuilder WithStandardErrorPipe(StreamReader target)
    {
        if (_commandConfiguration.UseShellExecution == true && target != StreamReader.Null)
        {
            throw new ArgumentException(Resources.CommandBuilder_ArgumentConflict_ShellExecution_Error);
        }
        else
        {
           return new CliCommandConfigurationBuilder(
                new CliCommandConfiguration(
                    _commandConfiguration.TargetFilePath,
                    _commandConfiguration.Arguments,
                    _commandConfiguration.WorkingDirectoryPath,
                    _commandConfiguration.RequiresAdministrator,
                    _commandConfiguration.EnvironmentVariables,
                    _commandConfiguration.Credential,
                    _commandConfiguration.ResultValidation,
                    _commandConfiguration.StandardInput,
                    _commandConfiguration.StandardOutput,
                    target,
                    _commandConfiguration.StandardInputEncoding,
                    _commandConfiguration.StandardOutputEncoding,
                    _commandConfiguration.StandardErrorEncoding,
                    _commandConfiguration.ResourcePolicy,
                    _commandConfiguration.WindowCreation,
                    _commandConfiguration.UseShellExecution));
        }
    }

    /// <summary>
    /// Sets the Process Resource Policy to be used for this command.
    /// </summary>
    /// <param name="processResourcePolicy">The process resource policy to use.</param>
    /// <returns>The new CommandBuilder with the specified Process Resource Policy.</returns>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
#endif
    [Pure]
    public ICliCommandConfigurationBuilder WithProcessResourcePolicy(ProcessResourcePolicy processResourcePolicy) =>
        new CliCommandConfigurationBuilder(
            new CliCommandConfiguration(_commandConfiguration.TargetFilePath,
                _commandConfiguration.Arguments,
                _commandConfiguration.WorkingDirectoryPath,
                _commandConfiguration.RequiresAdministrator,
                _commandConfiguration.EnvironmentVariables,
                _commandConfiguration.Credential,
                _commandConfiguration.ResultValidation,
                _commandConfiguration.StandardInput,
                _commandConfiguration.StandardOutput,
                _commandConfiguration.StandardError,
                _commandConfiguration.StandardInputEncoding,
                _commandConfiguration.StandardOutputEncoding,
                _commandConfiguration.StandardErrorEncoding,
                processResourcePolicy,
                _commandConfiguration.WindowCreation,
                _commandConfiguration.UseShellExecution));
    

    /// <summary>
    /// Enables or disables command execution via Shell Execution.
    /// </summary>
    /// <param name="useShellExecution">Whether to enable or disable shell execution.</param>
    /// <returns>The new CommandBuilder with the specified shell execution behaviour.</returns>
    /// <remarks>Using Shell Execution whilst also Redirecting Standard Input will throw an Exception. This is a known issue with the System Process class.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandarderror"/>
    [Pure]
    public ICliCommandConfigurationBuilder WithShellExecution(bool useShellExecution) =>
        new CliCommandConfigurationBuilder(
            new CliCommandConfiguration(_commandConfiguration.TargetFilePath,
                _commandConfiguration.Arguments,
                _commandConfiguration.WorkingDirectoryPath,
                _commandConfiguration.RequiresAdministrator,
                _commandConfiguration.EnvironmentVariables,
                _commandConfiguration.Credential,
                _commandConfiguration.ResultValidation,
                _commandConfiguration.StandardInput,
                _commandConfiguration.StandardOutput,
                _commandConfiguration.StandardError,
                _commandConfiguration.StandardInputEncoding,
                _commandConfiguration.StandardOutputEncoding,
                _commandConfiguration.StandardErrorEncoding,
                _commandConfiguration.ResourcePolicy,
                _commandConfiguration.WindowCreation,
                useShellExecution));

    /// <summary>
    /// Enables or disables Window creation for the wrapped executable.
    /// </summary>
    /// <param name="enableWindowCreation">Whether to enable or disable window creation for the wrapped executable.</param>
    /// <returns>The new CommandBuilder with the specified window creation behaviour.</returns>
    [Pure]
    public ICliCommandConfigurationBuilder WithWindowCreation(bool enableWindowCreation) =>
        new CliCommandConfigurationBuilder(new CliCommandConfiguration(_commandConfiguration.TargetFilePath,
           _commandConfiguration.Arguments,
           _commandConfiguration.WorkingDirectoryPath,
           _commandConfiguration.RequiresAdministrator,
           _commandConfiguration.EnvironmentVariables,
           _commandConfiguration.Credential,
           _commandConfiguration.ResultValidation,
           _commandConfiguration.StandardInput,
           _commandConfiguration.StandardOutput,
           _commandConfiguration.StandardError,
           _commandConfiguration.StandardInputEncoding,
           _commandConfiguration.StandardOutputEncoding,
           _commandConfiguration.StandardErrorEncoding,
           _commandConfiguration.ResourcePolicy,
            enableWindowCreation,
           _commandConfiguration.UseShellExecution));
    
    
    /// <summary>
    /// Sets the Encoding types to be used for Standard Input, Output, and Error.
    /// </summary>
    /// <param name="standardInputEncoding">The encoding type to be used for the Standard Input.</param>
    /// <param name="standardOutputEncoding">The encoding type to be used for the Standard Output.</param>
    /// <param name="standardErrorEncoding">The encoding type to be used for the Standard Error.</param>
    /// <returns>The new ICommandBuilder with the specified Pipe Encoding types.</returns>
    [Pure]
    public ICliCommandConfigurationBuilder WithEncoding(Encoding standardInputEncoding = null,
        Encoding standardOutputEncoding = null,
        Encoding standardErrorEncoding = null) =>
        new CliCommandConfigurationBuilder(new CliCommandConfiguration(_commandConfiguration.TargetFilePath,
            _commandConfiguration.Arguments,
            _commandConfiguration.WorkingDirectoryPath,
            _commandConfiguration.RequiresAdministrator,
            _commandConfiguration.EnvironmentVariables,
            _commandConfiguration.Credential,
            _commandConfiguration.ResultValidation,
            _commandConfiguration.StandardInput,
            _commandConfiguration.StandardOutput,
            _commandConfiguration.StandardError,
            standardInputEncoding,
            standardOutputEncoding,
            standardErrorEncoding,
            _commandConfiguration.ResourcePolicy,
            _commandConfiguration.WindowCreation,
            _commandConfiguration.UseShellExecution));

    /// <summary>
    /// Builds the Command with the configured parameters.
    /// </summary>
    /// <returns>The newly configured Command.</returns>
    [Pure]
    public CliCommandConfiguration Build() => new CliCommandConfiguration(_commandConfiguration);

    /// <summary>
    /// Builds the ICommandConfiguration with the configured parameters.
    /// </summary>
    /// <returns>The new ICommandConfiguration.</returns>
    public CliCommandConfiguration BuildConfiguration() => _commandConfiguration;
}