/*
    CliInvoke

    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.

     Method signatures and field declarations from CliWrap licensed under the MIT License except where considered Copyright Fair Use by law.
     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

using System.Text;

namespace CliInvoke.Builders;

#pragma warning disable CA1416

/// <summary>
///     Builder class for creating process configurations.
/// </summary>
public class ProcessConfigurationBuilder : IProcessConfigurationBuilder, IDisposable
{
    private string _targetFilePath;
    private OutputRedirectionMode _outputRedirection;
    
    private string _workingDirectoryPath;
    
    private bool _redirectStandardInput;
    private bool _enableWindowCreation;
    private bool _useShellExecution;
    private bool _requiresAdministratorPrivileges;
    
    private Encoding _standardInputEncoding;
    private Encoding _standardOutputEncoding;
    private Encoding _standardErrorEncoding;

    private StreamWriter _standardInput;
    
    private readonly ArgumentsBuilder _argumentsBuilder;
    private readonly EnvironmentVariablesBuilder _environmentVariablesBuilder;
    private readonly ProcessResourcePolicyBuilder _processResourcePolicyBuilder;
    private readonly UserCredentialBuilder _userCredentialBuilder;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProcessConfigurationBuilder" /> class,
    ///     which is used to build and configure a process.
    /// </summary>
    /// <param name="targetFilePath">The file path of the target file to be executed.</param>
    public ProcessConfigurationBuilder(string targetFilePath)
    {
        _targetFilePath = targetFilePath;
        _argumentsBuilder = new ArgumentsBuilder();
        _environmentVariablesBuilder = new EnvironmentVariablesBuilder();
        _processResourcePolicyBuilder = new ProcessResourcePolicyBuilder();
        _userCredentialBuilder =  new UserCredentialBuilder();

        _outputRedirection = OutputRedirectionMode.None;

        _redirectStandardInput = false;
        _enableWindowCreation = false;
        _useShellExecution = false;
        _requiresAdministratorPrivileges = false;

        _standardInputEncoding = Encoding.Default;
        _standardOutputEncoding = Encoding.Default;
        _standardErrorEncoding = Encoding.Default;

        _standardInput = StreamWriter.Null;
        
        _workingDirectoryPath = Directory.GetCurrentDirectory();
    }
    
    /// <summary>
    ///     Sets the process arguments to the Process Configuration builder.
    /// </summary>
    /// <param name="arguments">The process arguments to be added or updated.</param>
    /// <param name="escapeArguments">Whether the arguments should be escaped.</param>
    /// <returns>A reference to this builder with the added arguments, allowing method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref cref="arguments" /> is null.</exception>
    public IProcessConfigurationBuilder SetArguments(
        IEnumerable<string> arguments,
        bool escapeArguments = true)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        
        _argumentsBuilder.Clear();
        _argumentsBuilder.AddEnumerable(arguments, escapeArguments);
        
        return this;
    }

    /// <summary>
    ///     Sets process arguments to the Process Configuration builder.
    /// </summary>
    /// <param name="arguments">The argument string to be added.</param>
    /// <param name="escapeArguments"></param>
    /// <returns>A reference to this builder with the added string arguments, allowing method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="arguments" /> is null or empty.</exception>
    public IProcessConfigurationBuilder SetArguments(string arguments, bool escapeArguments = true)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        _argumentsBuilder.Clear();
        _argumentsBuilder.Add(arguments, escapeArguments);
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configureArguments"></param>
    /// <returns></returns>
    public IProcessConfigurationBuilder ConfigureArguments(Action<IArgumentsBuilder> configureArguments)
    {
        configureArguments.Invoke(_argumentsBuilder);
        
        return this;
    }

    /// <summary>
    ///     Sets the target file path for the process configuration.
    /// </summary>
    /// <param name="targetFilePath">The file path where the process configuration will be saved.</param>
    /// <returns>A reference to this builder with the updated target file path, allowing method chaining.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown if the <paramref name="targetFilePath" /> is null or
    ///     empty.
    /// </exception>
    public IProcessConfigurationBuilder SetTargetFilePath(string targetFilePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetFilePath);
        
        _targetFilePath = targetFilePath;

        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configureEnvironmentVariables"></param>
    /// <returns></returns>
    public IProcessConfigurationBuilder ConfigureEnvironmentVariables(Action<IEnvironmentVariablesBuilder> configureEnvironmentVariables)
    {
        configureEnvironmentVariables.Invoke(_environmentVariablesBuilder);
        
        return this;
    }

    /// <summary>
    ///     Configures the process to run with administrator privileges.
    /// </summary>
    /// <returns>
    ///     A reference to this builder with the updated administrator privileges,
    ///     allowing method chaining.
    /// </returns>
    public IProcessConfigurationBuilder RequireAdministratorPrivileges()
    {
        _requiresAdministratorPrivileges = true;

        return this;
    }

    /// <summary>
    ///     Sets the working directory path for the process configuration.
    /// </summary>
    /// <param name="workingDirectoryPath">The file system path where the process will be executed.</param>
    /// <returns>A reference to this builder, allowing method chaining.</returns>
    public IProcessConfigurationBuilder SetWorkingDirectory(string workingDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(workingDirectoryPath);

        if (!Directory.Exists(workingDirectoryPath))
            throw new DirectoryNotFoundException(
                $"Directory '{workingDirectoryPath}' could not be found or does not exist.");

        _workingDirectoryPath = workingDirectoryPath;

        return this;
    }

    /// <summary>
    ///     Configures the process to use a user credential.
    /// </summary>
    /// <param name="credential">The user credential to be used for authentication.</param>
    /// <returns>A reference to this builder with an updated user credential, allowing method chaining.</returns>
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("android")]
    public IProcessConfigurationBuilder SetUserCredential(UserCredential credential)
    {
        return ConfigureUserCredential(configureCredential =>
        {
            if(credential.LoadUserProfile is not null)
                configureCredential.LoadUserProfile((bool)credential.LoadUserProfile);
            
            if(credential.Domain is not null)
                configureCredential.SetDomain(credential.Domain);
            
            if(credential.UserName is not null)
                configureCredential.SetUsername(credential.UserName);
            
            if(credential.Password is not null)
                configureCredential.SetPassword(credential.Password);
        });
    }

    /// <summary>
    ///     Sets the credentials for the Command to be executed.
    /// </summary>
    /// <param name="configureCredential">The CredentialsBuilder configuration.</param>
    /// <returns>The new CommandBuilder with the specified Credentials.</returns>
    /// <remarks>
    ///     Credentials are only supported with the Process class on Windows. This is a limitation of
    ///     .NET's Process class.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="configureCredential" /> is null.</exception>
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("android")]
    public IProcessConfigurationBuilder ConfigureUserCredential(Action<IUserCredentialBuilder> configureCredential)
    {
        ArgumentNullException.ThrowIfNull(configureCredential);
        
        configureCredential(_userCredentialBuilder);

        return this;
    }

    /// <summary>
    ///     Configures whether the standard input of the process should be redirected.
    /// </summary>
    /// <param name="redirectStandardInput">
    ///     A value indicating whether standard input redirection is
    ///     enabled.
    /// </param>
    /// <returns>An instance of <see cref="IProcessConfigurationBuilder" /> with the updated configuration.</returns>
    public IProcessConfigurationBuilder RedirectStandardInput(bool redirectStandardInput)
    {
        _redirectStandardInput = redirectStandardInput;

        return this;
    }

    /// <summary>
    ///     Sets the Standard Input Pipe source.
    /// </summary>
    /// <param name="source">The source to use for the Standard Input pipe.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified Standard Input pipe source.</returns>
    /// <remarks>
    ///     Using Shell Execution whilst also Redirecting Standard Input will throw an Exception.
    ///     This is a known issue with the System Process class.
    /// </remarks>
    /// <seealso
    ///     href="https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardinput" />
    public IProcessConfigurationBuilder SetStandardInputPipe(StreamWriter source)
    {
        ArgumentNullException.ThrowIfNull(source);

        _standardInput = source;

        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="outputRedirectionMode"></param>
    /// <returns></returns>
    public IProcessConfigurationBuilder SetOutputRedirectionMode(OutputRedirectionMode outputRedirectionMode)
    {
        _outputRedirection = outputRedirectionMode;
        
        return this;
    }

    /// <summary>
    ///     Sets the policy for managing process resources.
    /// </summary>
    /// <param name="configureResourcePolicy">The policy that determines how the process resource is managed.</param>
    /// <returns>
    ///     A reference to this builder with the updated Process Resource Policy,
    ///     allowing method chaining.
    /// </returns>
    public IProcessConfigurationBuilder ConfigureProcessResourcePolicy(
        Action<IProcessResourcePolicyBuilder> configureResourcePolicy)
    {
        ArgumentNullException.ThrowIfNull(configureResourcePolicy);
        
        configureResourcePolicy.Invoke(_processResourcePolicyBuilder);
        
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="processResourcePolicy"></param>
    /// <returns></returns>
    public IProcessConfigurationBuilder SetProcessResourcePolicy(
        ProcessResourcePolicy processResourcePolicy)
    {
        return ConfigureProcessResourcePolicy(configureResourcePolicy =>
        {
            configureResourcePolicy.SetPriorityClass(processResourcePolicy.PriorityClass)
                .ConfigurePriorityBoost(processResourcePolicy.EnablePriorityBoost);

            if (processResourcePolicy.MinWorkingSet is not null)
                configureResourcePolicy.SetMinWorkingSet((nint)processResourcePolicy.MinWorkingSet);
            
            if(processResourcePolicy.MaxWorkingSet is not null)
                configureResourcePolicy.SetMaxWorkingSet((nint)processResourcePolicy.MaxWorkingSet);
            
            if(processResourcePolicy.ProcessorAffinity is not null)
                configureResourcePolicy.SetProcessorAffinity((nint)processResourcePolicy.ProcessorAffinity);
        });
    }

    /// <summary>
    ///     Configures whether shell execution should be used for the process.
    /// </summary>
    /// <param name="useShellExecution">True to use shell execution, false otherwise.</param>
    /// <returns>The updated Process Configuration builder with the updated configuration information.</returns>
    /// <remarks>
    ///     Using Shell Execution whilst also Redirecting Standard Input will throw an Exception.
    ///     This is a known issue with the System Process class.
    /// </remarks>
    /// <seealso
    ///     href="https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardinput" />
    public IProcessConfigurationBuilder UseShellExecution(bool useShellExecution)
    {
        _useShellExecution = useShellExecution;
        
        return this;
    }

    /// <summary>
    ///     Configures the process builder to enable or disable window creation.
    /// </summary>
    /// <param name="enableWindowCreation">
    ///     A boolean indicating whether to enable or disable window
    ///     creation.
    /// </param>
    /// <returns>The updated Process Configuration builder with the updated window creation configuration.</returns>
    public IProcessConfigurationBuilder EnableWindowCreation(bool enableWindowCreation)
    {
        _enableWindowCreation = enableWindowCreation;
        return this;
    }

    /// <summary>
    ///     Configures the process builder to use specific encoding schemes for standard input, output, and
    ///     error streams.
    /// </summary>
    /// <param name="standardInputEncoding">
    ///     The encoding scheme to use for standard input.
    ///     Uses the Default Encoding if null.
    /// </param>
    /// <param name="standardOutputEncoding"></param>
    /// <param name="standardErrorEncoding"></param>
    /// <returns>
    ///     The updated Process Configuration builder with the updated encoding scheme configuration
    ///     information.
    /// </returns>
    public IProcessConfigurationBuilder SetEncoding(Encoding? standardInputEncoding = null,
        Encoding? standardOutputEncoding = null, Encoding? standardErrorEncoding = null)
    {
        if(standardInputEncoding is not null)
            _standardInputEncoding = standardInputEncoding;
        
        if(standardOutputEncoding is not null)
            _standardOutputEncoding = standardOutputEncoding;
        
        if(standardErrorEncoding is not null)
            _standardErrorEncoding = standardErrorEncoding;

        return this;
    }

    /// <summary>
    ///     Builds and returns a ProcessConfiguration object with the specified properties.
    /// </summary>
    /// <returns>The configured ProcessConfiguration object.</returns>
    [Pure]
    public ProcessConfiguration Build()
    {
        string arguments = _argumentsBuilder.ToString();
        
        IReadOnlyDictionary<string, string> environmentVariables = _environmentVariablesBuilder.Build();
        
        ProcessResourcePolicy resourcePolicy = _processResourcePolicyBuilder.Build();
        UserCredential credential = _userCredentialBuilder.Build();

        ProcessConfigurationWrapper configuration = new ProcessConfigurationWrapper(_targetFilePath, arguments,
            _redirectStandardInput, _outputRedirection,
            _workingDirectoryPath, _requiresAdministratorPrivileges, environmentVariables,
            credential, _standardInput, _standardInputEncoding, _standardOutputEncoding, _standardErrorEncoding, resourcePolicy, _enableWindowCreation,
            _useShellExecution);

        return configuration;
    }

    public void Dispose()
    {
        _userCredentialBuilder.Dispose();
        _standardInput.Dispose();
    }
}

internal class ProcessConfigurationWrapper : ProcessConfiguration
{
    internal ProcessConfigurationWrapper(string targetFilePath, string arguments,
        bool redirectStandardInput,
        OutputRedirectionMode outputRedirection = OutputRedirectionMode.None,
        string? workingDirectoryPath = null, bool requiresAdministrator = false,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        UserCredential? credential = null, StreamWriter? standardInput = null,
        Encoding? standardInputEncoding = null, Encoding? standardOutputEncoding = null,
        Encoding? standardErrorEncoding = null, ProcessResourcePolicy? processResourcePolicy = null,
        bool windowCreation = false, bool useShellExecution = false) : base(targetFilePath,
        arguments, redirectStandardInput, outputRedirection, workingDirectoryPath,
        requiresAdministrator, environmentVariables, credential, standardInput,
        standardInputEncoding, standardOutputEncoding, standardErrorEncoding, processResourcePolicy,
        windowCreation, useShellExecution)
    {
    }

    internal ProcessConfigurationWrapper(string targetFilePath, bool redirectStandardInput,
        OutputRedirectionMode outputRedirection = OutputRedirectionMode.None,
        string? arguments = null, string? workingDirectoryPath = null,
        bool requiresAdministrator = false,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        UserCredential? credential = null, StreamWriter? standardInput = null,
        Encoding? standardInputEncoding = null, Encoding? standardOutputEncoding = null,
        Encoding? standardErrorEncoding = null, ProcessResourcePolicy? processResourcePolicy = null,
        bool windowCreation = false, bool useShellExecution = false) : base(targetFilePath,
        redirectStandardInput, outputRedirection, arguments, workingDirectoryPath,
        requiresAdministrator, environmentVariables, credential, standardInput,
        standardInputEncoding, standardOutputEncoding, standardErrorEncoding, processResourcePolicy,
        windowCreation, useShellExecution)
    {
    }
}

#pragma warning restore CA1416