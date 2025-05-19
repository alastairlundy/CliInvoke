using System.Collections.Generic;
using System.IO;
using System.Text;

using AlastairLundy.CliInvoke.Core.Abstractions.Builders;
using AlastairLundy.CliInvoke.Core.Primitives;
using AlastairLundy.CliInvoke.Core.Primitives.Policies;
using AlastairLundy.CliInvoke.Core.Primitives.Results;

namespace AlastairLundy.CliInvoke.Builders;

public class ProcessConfigurationBuilder : IProcessConfigurationBuilder
{
    private readonly ProcessConfiguration _configuration;

    public ProcessConfigurationBuilder()
    {
        
    }
    
    protected ProcessConfigurationBuilder(ProcessConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public IProcessConfigurationBuilder WithArguments(IEnumerable<string> arguments)
    {
        throw new System.NotImplementedException();
    }

    public IProcessConfigurationBuilder WithArguments(IEnumerable<string> arguments, bool escapeArguments)
    {
        throw new System.NotImplementedException();
    }

    public IProcessConfigurationBuilder WithArguments(string arguments)
    {
        throw new System.NotImplementedException();
    }

    public IProcessConfigurationBuilder WithTargetFile(string targetFilePath)
    {
        throw new System.NotImplementedException();
    }

    public IProcessConfigurationBuilder WithEnvironmentVariables(IReadOnlyDictionary<string, string> environmentVariables)
    {
        throw new System.NotImplementedException();
    }

    public IProcessConfigurationBuilder WithAdministratorPrivileges(bool runAsAdministrator)
    {
        throw new System.NotImplementedException();
    }

    public IProcessConfigurationBuilder WithWorkingDirectory(string workingDirectoryPath)
    {
        throw new System.NotImplementedException();
    }

    public IProcessConfigurationBuilder WithUserCredential(UserCredential credentials)
    {
        throw new System.NotImplementedException();
    }

    public IProcessConfigurationBuilder WithValidation(ProcessResultValidation validation)
    {
        throw new System.NotImplementedException();
    }

    public IProcessConfigurationBuilder WithStandardInputPipe(StreamWriter source)
    {
        throw new System.NotImplementedException();
    }

    public IProcessConfigurationBuilder WithStandardOutputPipe(StreamReader target)
    {
        throw new System.NotImplementedException();
    }

    public IProcessConfigurationBuilder WithStandardErrorPipe(StreamReader target)
    {
        throw new System.NotImplementedException();
    }

    public IProcessConfigurationBuilder WithProcessResourcePolicy(ProcessResourcePolicy processResourcePolicy)
    {
        throw new System.NotImplementedException();
    }

    public IProcessConfigurationBuilder WithShellExecution(bool useShellExecution)
    {
        throw new System.NotImplementedException();
    }

    public IProcessConfigurationBuilder WithWindowCreation(bool enableWindowCreation)
    {
        throw new System.NotImplementedException();
    }

    public IProcessConfigurationBuilder WithEncoding(Encoding standardInputEncoding = null,
        Encoding standardOutputEncoding = null, Encoding standardErrorEncoding = null)
    {
        throw new System.NotImplementedException();
    }

    public ProcessConfiguration Build()
    {
        return _configuration;
    }
}