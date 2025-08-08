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
using System.IO;
using System.Text;

using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Primitives;

namespace AlastairLundy.CliInvoke.Builders;

/// <summary>
/// 
/// </summary>
public class ProcessStartInfoBuilder : IProcessStartInfoBuilder
{
    private readonly ProcessConfiguration _configuration;

    /// <summary>
    /// 
    /// </summary>
    public ProcessStartInfoBuilder()
    {
        _configuration = new ProcessConfiguration();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="startInfo"></param>
    protected ProcessStartInfoBuilder(ProcessStartInfo startInfo)
    {
        
    }
    
    
    public IProcessStartInfoBuilder WithArguments(IEnumerable<string> arguments)
    {
       
    }

    public IProcessStartInfoBuilder WithArguments(IEnumerable<string> arguments, bool escapeArguments)
    {
        
    }

    public IProcessStartInfoBuilder WithArguments(string arguments)
    {
        
    }

    public IProcessStartInfoBuilder WithTargetFile(string targetFilePath)
    {
        
    }

    public IProcessStartInfoBuilder WithEnvironmentVariables(IReadOnlyDictionary<string, string> environmentVariables)
    {
        
    }

    public IProcessStartInfoBuilder WithAdministratorPrivileges(bool runAsAdministrator)
    {
        
    }

    public IProcessStartInfoBuilder WithWorkingDirectory(string workingDirectoryPath)
    {
        
    }

    public IProcessStartInfoBuilder WithUserCredential(Action<IUserCredentialBuilder> configure)
    {
        
    }

    public IProcessStartInfoBuilder WithStandardInputPipe(StreamWriter source)
    {
        
    }

    public IProcessStartInfoBuilder WithStandardOutputPipe(StreamReader target)
    {
        
    }

    public IProcessStartInfoBuilder WithStandardErrorPipe(StreamReader target)
    {
        
    }

    public IProcessStartInfoBuilder WithShellExecution(bool useShellExecution)
    {
        
    }

    public IProcessStartInfoBuilder WithWindowCreation(bool enableWindowCreation)
    {
        
    }

    public IProcessStartInfoBuilder WithEncoding(Encoding standardInputEncoding = null, Encoding standardOutputEncoding = null,
        Encoding standardErrorEncoding = null)
    {
        
    }

    public IProcessStartInfoBuilder WithProcessResourcePolicy(ProcessResourcePolicy processResourcePolicy)
    {
        
    }

    public IProcessStartInfoBuilder WithUserCredential(UserCredential credentials)
    {
        
    }
    
    public ProcessStartInfo Build()
    {
        
    }
}