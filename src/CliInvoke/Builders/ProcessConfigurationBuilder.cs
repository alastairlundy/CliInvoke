/*
    AlastairLundy.CliInvoke
     
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

     Method signatures and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

using AlastairLundy.CliInvoke.Builders.Abstractions;

using AlastairLundy.CliInvoke.Core.Abstractions.Builders;
using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Policies;
using AlastairLundy.CliInvoke.Core.Primitives.Results;

namespace AlastairLundy.CliInvoke.Builders;

/// <summary>
/// 
/// </summary>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class ProcessConfigurationBuilder : IProcessConfigurationBuilder
{
    private readonly ProcessConfiguration _configuration;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetFilePath"></param>
    public ProcessConfigurationBuilder(string targetFilePath)
    {
        _configuration = new ProcessConfiguration(targetFilePath);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="processStartInfo"></param>
    public ProcessConfigurationBuilder(ProcessStartInfo processStartInfo)
    {
        _configuration = new ProcessConfiguration(processStartInfo);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    protected ProcessConfigurationBuilder(ProcessConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
    public IProcessConfigurationBuilder WithArguments(IEnumerable<string> arguments)
    {
        return WithArguments(arguments, false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="arguments"></param>
    /// <param name="escapeArguments"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="targetFilePath"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="environmentVariables"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="runAsAdministrator"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="workingDirectoryPath"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="credentials"></param>
    /// <returns></returns>
    public IProcessConfigurationBuilder WithUserCredential(UserCredential credentials)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(_configuration.TargetFilePath,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator, _configuration.EnvironmentVariables,
                credentials, _configuration.ResultValidation,
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
    /// 
    /// </summary>
    /// <param name="validation"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="processResourcePolicy"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="useShellExecution"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="enableWindowCreation"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="standardInputEncoding"></param>
    /// <param name="standardOutputEncoding"></param>
    /// <param name="standardErrorEncoding"></param>
    /// <returns></returns>
    public IProcessConfigurationBuilder WithEncoding(Encoding standardInputEncoding = null,
        Encoding standardOutputEncoding = null, Encoding standardErrorEncoding = null)
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
    /// 
    /// </summary>
    /// <returns></returns>
    public ProcessConfiguration Build()
    {
        return _configuration;
    }
}