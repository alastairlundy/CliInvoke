/*
    AlastairLundy.CliInvoke
     
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

     Method signatures and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using AlastairLundy.CliInvoke.Core.Abstractions.Builders;
using AlastairLundy.CliInvoke.Core.Builders.Abstractions;
using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Policies;
using AlastairLundy.CliInvoke.Core.Primitives.Results;

namespace AlastairLundy.CliInvoke.Core.Builders;

/// <summary>
/// Builder class for creating process configurations.
/// </summary>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class ProcessConfigurationBuilder : IProcessConfigurationBuilder
{
    private readonly ProcessConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessConfigurationBuilder"/> class,
    /// which is used to build and configure a process.
    /// </summary>
    /// <param name="targetFilePath">The file path of the target file to be executed.</param>
    public ProcessConfigurationBuilder(string targetFilePath)
    {
        _configuration = new ProcessConfiguration(targetFilePath);
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessConfigurationBuilder"/> class,
    /// which is used to build and configure a process.
    /// </summary>
    /// <param name="processStartInfo">The start information for the process configuration.</param>
    public ProcessConfigurationBuilder(ProcessStartInfo processStartInfo)
    {
        _configuration = new ProcessConfiguration(processStartInfo);
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessConfigurationBuilder"/> class,
    /// which is used to build and configure a process.
    /// </summary>
    /// <param name="configuration">A process configuration to update.</param>
    protected ProcessConfigurationBuilder(ProcessConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    /// <summary>
    /// Adds process arguments to the Process Configuration builder.
    /// </summary>
    /// <param name="arguments">The process arguments to be added.</param>
    /// <returns>A reference to this builder with the added arguments, allowing method chaining.</returns>
    [Pure]
    public IProcessConfigurationBuilder WithArguments(IEnumerable<string> arguments)
    {
        return WithArguments(arguments, false);
    }

    /// <summary>
    /// Adds process arguments to the Process Configuration builder.
    /// </summary>
    /// <param name="arguments">The process arguments to be added or updated.</param>
    /// <param name="escapeArguments">Whether the arguments should be escaped.</param>
    /// <returns>A reference to this builder with the added arguments, allowing method chaining.</returns>
    [Pure]
    public IProcessConfigurationBuilder WithArguments(IEnumerable<string> arguments, bool escapeArguments)
    {
        IArgumentsBuilder argumentsBuilder = new ArgumentsBuilder()
            .Add(arguments, escapeArguments);

        string args = argumentsBuilder.ToString();
        
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                args,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator, _configuration.EnvironmentVariables,
                _configuration.Credential, _configuration.ResultValidation,
                _configuration.StandardInput, _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                _configuration.WindowCreation,
                _configuration.UseShellExecution));
    }

    /// <summary>
    /// Adds process arguments to the Process Configuration builder.
    /// </summary>
    /// <param name="arguments">The argument string to be added.</param>
    /// <returns>A reference to this builder with the added string arguments, allowing method chaining.</returns>
    [Pure]
    public IProcessConfigurationBuilder WithArguments(string arguments)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator, _configuration.EnvironmentVariables,
                _configuration.Credential, _configuration.ResultValidation,
                _configuration.StandardInput, _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                _configuration.WindowCreation,
                _configuration.UseShellExecution));
    }

    /// <summary>
    /// Sets the target file path for the process configuration.
    /// </summary>
    /// <param name="targetFilePath">The file path where the process configuration will be saved.</param>
    /// <returns>A reference to this builder with the updated target file path, allowing method chaining.</returns>
    [Pure]
    public IProcessConfigurationBuilder WithTargetFile(string targetFilePath)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(targetFilePath,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator, _configuration.EnvironmentVariables,
                _configuration.Credential, _configuration.ResultValidation,
                _configuration.StandardInput, _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                _configuration.WindowCreation,
                _configuration.UseShellExecution));
    }

    /// <summary>
    /// Sets environment variables for the process configuration.
    /// </summary>
    /// <param name="environmentVariables">The environment variables to be added to the process configuration.</param>
    /// <returns>A reference to this builder with the updated target file path, allowing method chaining.</returns>
    [Pure]
    public IProcessConfigurationBuilder WithEnvironmentVariables(IReadOnlyDictionary<string, string> environmentVariables)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator, environmentVariables,
                _configuration.Credential, _configuration.ResultValidation,
                _configuration.StandardInput, _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                _configuration.WindowCreation,
                _configuration.UseShellExecution));
    }

    /// <summary>
    /// Configures the process to run with administrator privileges.
    /// </summary>
    /// <param name="runAsAdministrator">Whether the process should be executed as an administrator.</param>
    /// <returns>A reference to this builder with the updated administrator privileges,
    /// allowing method chaining.</returns>
    [Pure]
    public IProcessConfigurationBuilder WithAdministratorPrivileges(bool runAsAdministrator)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                runAsAdministrator, _configuration.EnvironmentVariables,
                _configuration.Credential, _configuration.ResultValidation,
                _configuration.StandardInput, _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                _configuration.WindowCreation,
                _configuration.UseShellExecution));
    }

    /// <summary>
    /// Sets the working directory path for the process configuration.
    /// </summary>
    /// <param name="workingDirectoryPath">The file system path where the process will be executed.</param>
    /// <returns>A reference to this builder, allowing method chaining.</returns>
    [Pure]
    public IProcessConfigurationBuilder WithWorkingDirectory(string workingDirectoryPath)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.Arguments,
                workingDirectoryPath,
                _configuration.RequiresAdministrator, _configuration.EnvironmentVariables,
                _configuration.Credential, _configuration.ResultValidation,
                _configuration.StandardInput, _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                _configuration.WindowCreation,
                _configuration.UseShellExecution));
    }

    /// <summary>
    /// Configures the process to use a user credential.
    /// </summary>
    /// <param name="credential">The user credential to be used for authentication.</param>
    /// <returns>A reference to this builder with an updated user credential, allowing method chaining.</returns>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("android")]
#endif
    [Pure]
    public IProcessConfigurationBuilder WithUserCredential(UserCredential credential)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator, _configuration.EnvironmentVariables,
                credential, _configuration.ResultValidation,
                _configuration.StandardInput, _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                _configuration.WindowCreation,
                _configuration.UseShellExecution));
    }

    
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
    public IProcessConfigurationBuilder WithUserCredential(Action<IUserCredentialBuilder> configure)
    {
        UserCredential credential;

        if (_configuration.Credential is null)
        {
            credential = UserCredential.Null;
        }
        else
        {
            credential = _configuration.Credential;
        }
        
        IUserCredentialBuilder credentialBuilder = new UserCredentialBuilder()
            .SetDomain(credential.Domain)
            .SetPassword(credential.Password)
            .SetUsername(credential.UserName);

        configure(credentialBuilder);

        return WithUserCredential(credentialBuilder.Build());
    }

    /// <summary>
    /// Enables or disables Process Result Validation (i.e. whether to throw an exception if Exit Code is not 0).
    /// </summary>
    /// <param name="validation">The validation mode to be used for the process result.</param>
    /// <returns>A reference to this builder with the specified validation configuration,
    /// allowing method chaining.</returns>
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
    public IProcessConfigurationBuilder WithValidation(ProcessResultValidation validation)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator, _configuration.EnvironmentVariables,
                _configuration.Credential, validation,
                _configuration.StandardInput, _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                _configuration.WindowCreation,
                _configuration.UseShellExecution));
    }

    /// <summary>
    /// Sets the Standard Input Pipe source.
    /// </summary>
    /// <param name="source">The source to use for the Standard Input pipe.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified Standard Input pipe source.</returns>
    /// <remarks>Using Shell Execution whilst also Redirecting Standard Input will throw an Exception.
    /// This is a known issue with the System Process class.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardinput"/>
    [Pure]
    public IProcessConfigurationBuilder WithStandardInputPipe(StreamWriter source)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator, _configuration.EnvironmentVariables,
                _configuration.Credential, _configuration.ResultValidation,
                source, _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                _configuration.WindowCreation,
                _configuration.UseShellExecution));
    }

    /// <summary>
    /// Sets the Standard Output Pipe target.
    /// </summary>
    /// <param name="target">The target to send the Standard Output to.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified Standard Output Pipe Target.</returns>
    /// <remarks>Using Shell Execution whilst also Redirecting Standard Output will throw an Exception. This is a known issue with the System Process class.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardoutput"/>
    [Pure]
    public IProcessConfigurationBuilder WithStandardOutputPipe(StreamReader target)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator, _configuration.EnvironmentVariables,
                _configuration.Credential, _configuration.ResultValidation,
                _configuration.StandardInput, target,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                _configuration.WindowCreation,
                _configuration.UseShellExecution));
    }

    /// <summary>
    /// Sets the Standard Error Pipe target.
    /// </summary>
    /// <param name="target">The target to send the Standard Error to.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified Standard Error Pipe Target.</returns>
    /// <remarks>Using Shell Execution whilst also Redirecting Standard Error will throw an Exception. This is a known issue with the System Process class.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandarderror"/>
    [Pure]
    public IProcessConfigurationBuilder WithStandardErrorPipe(StreamReader target)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator, _configuration.EnvironmentVariables,
                _configuration.Credential, _configuration.ResultValidation,
                _configuration.StandardInput, _configuration.StandardOutput,
                target,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                _configuration.WindowCreation,
                _configuration.UseShellExecution));
    }

    /// <summary>
    /// Sets the policy for managing process resources.
    /// </summary>
    /// <param name="processResourcePolicy">The policy that determines how the process resource is managed.</param>
    /// <returns>A reference to this builder with the updated Process Resource Policy,
    /// allowing method chaining.</returns>
    [Pure]
    public IProcessConfigurationBuilder WithProcessResourcePolicy(ProcessResourcePolicy processResourcePolicy)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator, _configuration.EnvironmentVariables,
                _configuration.Credential, _configuration.ResultValidation,
                _configuration.StandardInput, _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                processResourcePolicy,
                _configuration.WindowCreation,
        useShellExecution: _configuration.UseShellExecution));
    }

    /// <summary>
    /// Configures whether shell execution should be used for the process.
    /// </summary>
    /// <param name="useShellExecution">True to use shell execution, false otherwise.</param>
    /// <returns>The updated Process Configuration builder with the updated configuration info.</returns>
    /// <remarks>Using Shell Execution whilst also Redirecting Standard Input will throw an Exception.
    /// This is a known issue with the System Process class.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardinput"/>
    [Pure]
    public IProcessConfigurationBuilder WithShellExecution(bool useShellExecution)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator, _configuration.EnvironmentVariables,
                _configuration.Credential, _configuration.ResultValidation,
                _configuration.StandardInput, _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                _configuration.WindowCreation,
                useShellExecution: useShellExecution));
    }

    /// <summary>
    /// Configures the process builder to enable or disable window creation.
    /// </summary>
    /// <param name="enableWindowCreation">A boolean indicating whether to enable or disable window creation.</param>
    /// <returns>The updated Process Configuration builder with the updated window creation configuration.</returns>
    [Pure]
    public IProcessConfigurationBuilder WithWindowCreation(bool enableWindowCreation)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator, _configuration.EnvironmentVariables,
                _configuration.Credential, _configuration.ResultValidation,
                _configuration.StandardInput, _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: enableWindowCreation,
        useShellExecution: _configuration.UseShellExecution));
    }

    /// <summary>
    /// Configures the process builder to use specific encoding schemes for standard input, output, and error streams.
    /// </summary>
    /// <param name="standardInputEncoding">The encoding scheme to use for standard input.
    /// Uses the Default Encoding if null.</param>
    /// <param name="standardOutputEncoding">The encoding scheme to use for standard output.
    /// Uses the Default Encoding if null.</param>
    /// <param name="standardErrorEncoding">The encoding scheme to use for standard error.
    /// Uses the Default Encoding if null.</param>
    /// <returns>The updated Process Configuration builder with the updated encoding scheme configuration info.</returns>
    [Pure]
    public IProcessConfigurationBuilder WithEncoding(Encoding? standardInputEncoding = null,
        Encoding? standardOutputEncoding = null, Encoding? standardErrorEncoding = null)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator, _configuration.EnvironmentVariables,
                _configuration.Credential, _configuration.ResultValidation,
                _configuration.StandardInput, _configuration.StandardOutput,
                _configuration.StandardError,
                standardInputEncoding: standardInputEncoding ?? Encoding.Default,
                standardOutputEncoding: standardOutputEncoding ?? Encoding.Default,
                standardErrorEncoding: standardErrorEncoding ?? Encoding.Default,
                processResourcePolicy: _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution));
    }

    /// <summary>
    /// Builds and returns a ProcessConfiguration object with the specified properties.
    /// </summary>
    /// <returns>The configured ProcessConfiguration object.</returns>
    public ProcessConfiguration Build()
    {
        return _configuration;
    }
}