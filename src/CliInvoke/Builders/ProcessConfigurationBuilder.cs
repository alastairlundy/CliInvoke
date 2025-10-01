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
using System.Text;

using AlastairLundy.CliInvoke.Core.Primitives;

#nullable enable

using AlastairLundy.CliInvoke.Core.Builders;


using System.Runtime.Versioning;



namespace AlastairLundy.CliInvoke.Builders;

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
        _configuration = new ProcessConfiguration(targetFilePath, false,
            true, true);
    }
        
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessConfigurationBuilder"/> class,
    /// which is used to build and configure a process.
    /// </summary>
    /// <param name="processStartInfo">The start information for the process configuration.</param>
    public ProcessConfigurationBuilder(ProcessStartInfo processStartInfo)
    {
        _configuration = new ProcessConfiguration(processStartInfo, processStartInfo.RedirectStandardInput,
            processStartInfo.RedirectStandardOutput, processStartInfo.RedirectStandardError);
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessConfigurationBuilder"/> class,
    /// which is used to build and configure a process.
    /// </summary>
    /// <param name="configuration">A process configuration to update.</param>
    public ProcessConfigurationBuilder(ProcessConfiguration configuration)
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
        return WithArguments(arguments,
            false);
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
            .Add(arguments,
                escapeArguments);

        string args = argumentsBuilder.ToString();
        
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                args,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution));
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
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution));
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
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution));
    }

    /// <summary>
    /// Sets environment variables for the process configuration.
    /// </summary>
    /// <param name="environmentVariables">The environment variables to be added to the process configuration.</param>
    /// <returns>A reference to this builder with the updated target file path, allowing method chaining.</returns>
    [Pure]
    public IProcessConfigurationBuilder WithEnvironmentVariables(IDictionary<string, string> environmentVariables)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                environmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution));
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
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                runAsAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution));
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
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                workingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution));
    }

    /// <summary>
    /// Configures the process to use a user credential.
    /// </summary>
    /// <param name="credential">The user credential to be used for authentication.</param>
    /// <returns>A reference to this builder with an updated user credential, allowing method chaining.</returns>
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("android")]
    [Pure]
    public IProcessConfigurationBuilder WithUserCredential(UserCredential credential)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                credential,
                _configuration.StandardInput,
                _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution));
    }

    
    /// <summary>
    /// Sets the credentials for the Command to be executed.
    /// </summary>
    /// <param name="configure">The CredentialsBuilder configuration.</param>
    /// <returns>The new CommandBuilder with the specified Credentials.</returns>
    /// <remarks>Credentials are only supported with the Process class on Windows. This is a limitation of .NET's Process class.</remarks>
    [Pure]
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("android")]
    public IProcessConfigurationBuilder WithUserCredential(Action<IUserCredentialBuilder> configure)
    {
        UserCredential credential = _configuration.Credential ?? UserCredential.Null;
        
        IUserCredentialBuilder credentialBuilder = new UserCredentialBuilder()
            .SetDomain(credential.Domain)
            .SetPassword(credential.Password)
            .SetUsername(credential.UserName);

        configure(credentialBuilder);

        return WithUserCredential(credentialBuilder.Build());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="redirectStandardInput"></param>
    /// <returns></returns>
    [Pure]
    public IProcessConfigurationBuilder RedirectStandardInput(bool redirectStandardInput)
    {
            return new ProcessConfigurationBuilder(
                new ProcessConfiguration(_configuration.TargetFilePath,
                    redirectStandardInput,
                    _configuration.RedirectStandardOutput,
                    _configuration.RedirectStandardError,
                    _configuration.Arguments,
                    _configuration.WorkingDirectoryPath,
                    _configuration.RequiresAdministrator,
                    _configuration.EnvironmentVariables,
                    _configuration.Credential,
                    _configuration.StandardInput ?? StreamWriter.Null,
                    _configuration.StandardOutput,
                    _configuration.StandardError,
                    _configuration.StandardInputEncoding,
                    _configuration.StandardOutputEncoding,
                    _configuration.StandardErrorEncoding,
                    _configuration.ResourcePolicy,
                    windowCreation: _configuration.WindowCreation,
                    useShellExecution: _configuration.UseShellExecution));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="redirectStandardOutput"></param>
    /// <returns></returns>
    [Pure]
    public IProcessConfigurationBuilder RedirectStandardOutput(bool redirectStandardOutput)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                redirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardOutput ?? StreamReader.Null,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="redirectStandardError"></param>
    /// <returns></returns>
    [Pure]
    public IProcessConfigurationBuilder RedirectStandardError(bool redirectStandardError)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                redirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardOutput,
                _configuration.StandardError ??  StreamReader.Null,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution));
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
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                source,
                _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution));
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
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                target,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution));
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
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardOutput,
                target,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution));
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
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                processResourcePolicy,
                windowCreation: _configuration.WindowCreation,
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
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardOutput,
                _configuration.StandardError,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
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
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardOutput,
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
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardOutput,
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
    public ProcessConfiguration Build() => _configuration;
}