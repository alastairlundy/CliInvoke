/*
    AlastairLundy.CliInvoke
     
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;

using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Primitives;

namespace AlastairLundy.CliInvoke.Builders;

/// <summary>
/// 
/// </summary>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class ProcessStartInfoBuilder : IProcessStartInfoBuilder
{
    private readonly ProcessConfiguration _processConfiguration;

    /// <summary>
    /// 
    /// </summary>
    public ProcessStartInfoBuilder(string targetFilePath)
    {
      _processConfiguration = new ProcessConfiguration(targetFilePath);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="processConfiguration"></param>
    protected ProcessStartInfoBuilder(ProcessConfiguration processConfiguration)
    {
        _processConfiguration = processConfiguration;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
    [Pure]
    public IProcessStartInfoBuilder WithArguments(IEnumerable<string> arguments)
    {
        return WithArguments(arguments,
            false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="arguments"></param>
    /// <param name="escapeArguments"></param>
    /// <returns></returns>
    [Pure]
    public IProcessStartInfoBuilder WithArguments(IEnumerable<string> arguments, bool escapeArguments)
    {
        IArgumentsBuilder argumentsBuilder = new ArgumentsBuilder()
            .Add(arguments,
                escapeArguments);

        string args = argumentsBuilder.ToString();
        
        return new ProcessStartInfoBuilder(
            new ProcessConfiguration(_processConfiguration.TargetFilePath,
                args,
                _processConfiguration.WorkingDirectoryPath,
                _processConfiguration.RequiresAdministrator,
                _processConfiguration.EnvironmentVariables,
                _processConfiguration.Credential,
                _processConfiguration.StandardInput,
                _processConfiguration.StandardOutput,
                _processConfiguration.StandardError,
                _processConfiguration.StandardInputEncoding,
                _processConfiguration.StandardOutputEncoding,
                _processConfiguration.StandardErrorEncoding,
                _processConfiguration.ResourcePolicy,
                windowCreation: _processConfiguration.WindowCreation,
                useShellExecution: _processConfiguration.UseShellExecution));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
    [Pure]
    public IProcessStartInfoBuilder WithArguments(string arguments)
    {
        return new ProcessStartInfoBuilder(
            new ProcessConfiguration(_processConfiguration.TargetFilePath,
                arguments,
                _processConfiguration.WorkingDirectoryPath,
                _processConfiguration.RequiresAdministrator,
                _processConfiguration.EnvironmentVariables,
                _processConfiguration.Credential,
                _processConfiguration.StandardInput,
                _processConfiguration.StandardOutput,
                _processConfiguration.StandardError,
                _processConfiguration.StandardInputEncoding,
                _processConfiguration.StandardOutputEncoding,
                _processConfiguration.StandardErrorEncoding,
                _processConfiguration.ResourcePolicy,
                windowCreation: _processConfiguration.WindowCreation,
                useShellExecution: _processConfiguration.UseShellExecution));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetFilePath"></param>
    /// <returns></returns>
    [Pure]
    public IProcessStartInfoBuilder WithTargetFile(string targetFilePath)
    {
        return new ProcessStartInfoBuilder(
            new ProcessConfiguration(targetFilePath,
                _processConfiguration.Arguments,
                _processConfiguration.WorkingDirectoryPath,
                _processConfiguration.RequiresAdministrator,
                _processConfiguration.EnvironmentVariables,
                _processConfiguration.Credential,
                _processConfiguration.StandardInput,
                _processConfiguration.StandardOutput,
                _processConfiguration.StandardError,
                _processConfiguration.StandardInputEncoding,
                _processConfiguration.StandardOutputEncoding,
                _processConfiguration.StandardErrorEncoding,
                _processConfiguration.ResourcePolicy,
                windowCreation: _processConfiguration.WindowCreation,
                useShellExecution: _processConfiguration.UseShellExecution));
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="environmentVariables"></param>
    /// <returns></returns>
    [Pure]
    public IProcessStartInfoBuilder WithEnvironmentVariables(IReadOnlyDictionary<string, string> environmentVariables)
    {
        return new ProcessStartInfoBuilder(
            new ProcessConfiguration(_processConfiguration.TargetFilePath,
                _processConfiguration.Arguments,
                _processConfiguration.WorkingDirectoryPath,
                _processConfiguration.RequiresAdministrator,
                environmentVariables,
                _processConfiguration.Credential,
                _processConfiguration.StandardInput,
                _processConfiguration.StandardOutput,
                _processConfiguration.StandardError,
                _processConfiguration.StandardInputEncoding,
                _processConfiguration.StandardOutputEncoding,
                _processConfiguration.StandardErrorEncoding,
                _processConfiguration.ResourcePolicy,
                windowCreation: _processConfiguration.WindowCreation,
                useShellExecution: _processConfiguration.UseShellExecution));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="runAsAdministrator"></param>
    /// <returns></returns>
    [Pure]
    public IProcessStartInfoBuilder WithAdministratorPrivileges(bool runAsAdministrator)
    {
        return new ProcessStartInfoBuilder(
            new ProcessConfiguration(_processConfiguration.TargetFilePath,
                _processConfiguration.Arguments,
                _processConfiguration.WorkingDirectoryPath,
                runAsAdministrator,
                _processConfiguration.EnvironmentVariables,
                _processConfiguration.Credential,
                _processConfiguration.StandardInput,
                _processConfiguration.StandardOutput,
                _processConfiguration.StandardError,
                _processConfiguration.StandardInputEncoding,
                _processConfiguration.StandardOutputEncoding,
                _processConfiguration.StandardErrorEncoding,
                _processConfiguration.ResourcePolicy,
                windowCreation: _processConfiguration.WindowCreation,
                useShellExecution: _processConfiguration.UseShellExecution));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="workingDirectoryPath"></param>
    /// <returns></returns>
    [Pure]
    public IProcessStartInfoBuilder WithWorkingDirectory(string workingDirectoryPath)
    {
        return new ProcessStartInfoBuilder(
            new ProcessConfiguration(_processConfiguration.TargetFilePath,
                _processConfiguration.Arguments,
                workingDirectoryPath,
                _processConfiguration.RequiresAdministrator,
                _processConfiguration.EnvironmentVariables,
                _processConfiguration.Credential,
                _processConfiguration.StandardInput,
                _processConfiguration.StandardOutput,
                _processConfiguration.StandardError,
                _processConfiguration.StandardInputEncoding,
                _processConfiguration.StandardOutputEncoding,
                _processConfiguration.StandardErrorEncoding,
                _processConfiguration.ResourcePolicy,
                windowCreation: _processConfiguration.WindowCreation,
                useShellExecution: _processConfiguration.UseShellExecution));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configure"></param>
    /// <returns></returns>
    [Pure]
    public IProcessStartInfoBuilder WithUserCredential(Action<IUserCredentialBuilder> configure)
    {
        UserCredential credential;

        credential = _processConfiguration.Credential ?? UserCredential.Null;
        
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
    /// <param name="source"></param>
    /// <returns></returns>
    [Pure]
    public IProcessStartInfoBuilder WithStandardInputPipe(StreamWriter source)
    {
        return new ProcessStartInfoBuilder(
            new ProcessConfiguration(_processConfiguration.TargetFilePath,
                _processConfiguration.Arguments,
                _processConfiguration.WorkingDirectoryPath,
                _processConfiguration.RequiresAdministrator,
                _processConfiguration.EnvironmentVariables,
                _processConfiguration.Credential,
                source,
                _processConfiguration.StandardOutput,
                _processConfiguration.StandardError,
                _processConfiguration.StandardInputEncoding,
                _processConfiguration.StandardOutputEncoding,
                _processConfiguration.StandardErrorEncoding,
                _processConfiguration.ResourcePolicy,
                windowCreation: _processConfiguration.WindowCreation,
                useShellExecution: _processConfiguration.UseShellExecution));
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    [Pure]
    public IProcessStartInfoBuilder WithStandardOutputPipe(StreamReader target)
    {
        return new ProcessStartInfoBuilder(
            new ProcessConfiguration(_processConfiguration.TargetFilePath,
                _processConfiguration.Arguments,
                _processConfiguration.WorkingDirectoryPath,
                _processConfiguration.RequiresAdministrator,
                _processConfiguration.EnvironmentVariables,
                _processConfiguration.Credential,
                _processConfiguration.StandardInput,
                target,
                _processConfiguration.StandardError,
                _processConfiguration.StandardInputEncoding,
                _processConfiguration.StandardOutputEncoding,
                _processConfiguration.StandardErrorEncoding,
                _processConfiguration.ResourcePolicy,
                windowCreation: _processConfiguration.WindowCreation,
                useShellExecution: _processConfiguration.UseShellExecution));
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    [Pure]
    public IProcessStartInfoBuilder WithStandardErrorPipe(StreamReader target)
    {
        return new ProcessStartInfoBuilder(
            new ProcessConfiguration(_processConfiguration.TargetFilePath,
                _processConfiguration.Arguments,
                _processConfiguration.WorkingDirectoryPath,
                _processConfiguration.RequiresAdministrator,
                _processConfiguration.EnvironmentVariables,
                _processConfiguration.Credential,
                _processConfiguration.StandardInput,
                _processConfiguration.StandardOutput,
                target,
                _processConfiguration.StandardInputEncoding,
                _processConfiguration.StandardOutputEncoding,
                _processConfiguration.StandardErrorEncoding,
                _processConfiguration.ResourcePolicy,
                windowCreation: _processConfiguration.WindowCreation,
                useShellExecution: _processConfiguration.UseShellExecution));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="useShellExecution"></param>
    /// <returns></returns>
    [Pure]
    public IProcessStartInfoBuilder WithShellExecution(bool useShellExecution)
    {
        return new ProcessStartInfoBuilder(
            new ProcessConfiguration(_processConfiguration.TargetFilePath,
                _processConfiguration.Arguments,
                _processConfiguration.WorkingDirectoryPath,
                _processConfiguration.RequiresAdministrator,
                _processConfiguration.EnvironmentVariables,
                _processConfiguration.Credential,
                _processConfiguration.StandardInput,
                _processConfiguration.StandardOutput,
                _processConfiguration.StandardError,
                _processConfiguration.StandardInputEncoding,
                _processConfiguration.StandardOutputEncoding,
                _processConfiguration.StandardErrorEncoding,
                _processConfiguration.ResourcePolicy,
                windowCreation: _processConfiguration.WindowCreation,
                useShellExecution: useShellExecution));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="enableWindowCreation"></param>
    /// <returns></returns>
    [Pure]
    public IProcessStartInfoBuilder WithWindowCreation(bool enableWindowCreation)
    {
        return new ProcessStartInfoBuilder(
            new ProcessConfiguration(_processConfiguration.TargetFilePath,
                _processConfiguration.Arguments,
                _processConfiguration.WorkingDirectoryPath,
                _processConfiguration.RequiresAdministrator,
                _processConfiguration.EnvironmentVariables,
                _processConfiguration.Credential,
                _processConfiguration.StandardInput,
                _processConfiguration.StandardOutput,
                _processConfiguration.StandardError,
                _processConfiguration.StandardInputEncoding,
                _processConfiguration.StandardOutputEncoding,
                _processConfiguration.StandardErrorEncoding,
                _processConfiguration.ResourcePolicy,
                windowCreation: enableWindowCreation,
                useShellExecution: _processConfiguration.UseShellExecution));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="standardInputEncoding"></param>
    /// <param name="standardOutputEncoding"></param>
    /// <param name="standardErrorEncoding"></param>
    /// <returns></returns>
    [Pure]
    public IProcessStartInfoBuilder WithEncoding(Encoding standardInputEncoding = null, Encoding standardOutputEncoding = null,
        Encoding standardErrorEncoding = null)
    {
        return new ProcessStartInfoBuilder(
            new ProcessConfiguration(_processConfiguration.TargetFilePath,
                _processConfiguration.Arguments,
                _processConfiguration.WorkingDirectoryPath,
                _processConfiguration.RequiresAdministrator,
                _processConfiguration.EnvironmentVariables,
                _processConfiguration.Credential,
                _processConfiguration.StandardInput,
                _processConfiguration.StandardOutput,
                _processConfiguration.StandardError,
                standardInputEncoding: standardInputEncoding ?? Encoding.Default,
                standardOutputEncoding: standardOutputEncoding ?? Encoding.Default,
                standardErrorEncoding: standardErrorEncoding ?? Encoding.Default,
                processResourcePolicy: _processConfiguration.ResourcePolicy,
                windowCreation: _processConfiguration.WindowCreation,
                useShellExecution: _processConfiguration.UseShellExecution));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="processResourcePolicy"></param>
    /// <returns></returns>
    [Pure]
    public IProcessStartInfoBuilder WithProcessResourcePolicy(ProcessResourcePolicy processResourcePolicy)
    {
        return new ProcessStartInfoBuilder(
            new ProcessConfiguration(_processConfiguration.TargetFilePath,
                _processConfiguration.Arguments,
                _processConfiguration.WorkingDirectoryPath,
                _processConfiguration.RequiresAdministrator,
                _processConfiguration.EnvironmentVariables,
                _processConfiguration.Credential,
                _processConfiguration.StandardInput,
                _processConfiguration.StandardOutput,
                _processConfiguration.StandardError,
                _processConfiguration.StandardInputEncoding,
                _processConfiguration.StandardOutputEncoding,
                _processConfiguration.StandardErrorEncoding,
                processResourcePolicy,
                windowCreation: _processConfiguration.WindowCreation,
                useShellExecution: _processConfiguration.UseShellExecution));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="credential"></param>
    /// <returns></returns>
    [Pure]
    public IProcessStartInfoBuilder WithUserCredential(UserCredential credential)
    {
        return new ProcessStartInfoBuilder(
            new ProcessConfiguration(_processConfiguration.TargetFilePath,
                _processConfiguration.Arguments,
                _processConfiguration.WorkingDirectoryPath,
                _processConfiguration.RequiresAdministrator,
                _processConfiguration.EnvironmentVariables,
                credential,
                _processConfiguration.StandardInput,
                _processConfiguration.StandardOutput,
                _processConfiguration.StandardError,
                _processConfiguration.StandardInputEncoding,
                _processConfiguration.StandardOutputEncoding,
                _processConfiguration.StandardErrorEncoding,
                _processConfiguration.ResourcePolicy,
                windowCreation: _processConfiguration.WindowCreation,
                useShellExecution: _processConfiguration.UseShellExecution));
    }

    /// <summary>
    /// Builds and returns a ProcessStartInfo object with the specified properties.
    /// </summary>
    /// <returns>The configured ProcessStartInfo object.</returns>
    public ProcessStartInfo Build()
        => _processConfiguration.ToProcessStartInfo();
}