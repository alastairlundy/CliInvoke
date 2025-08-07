using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Primitives;

namespace AlastairLundy.CliInvoke.Builders;

public class ProcessStartInfoBuilder : IProcessStartInfoBuilder
{
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

    public ProcessStartInfo Build()
    {
        
    }

    public IProcessStartInfoBuilder WithProcessResourcePolicy(ProcessResourcePolicy processResourcePolicy)
    {
        
    }

    public IProcessStartInfoBuilder WithUserCredential(UserCredential credentials)
    {
        
    }
}