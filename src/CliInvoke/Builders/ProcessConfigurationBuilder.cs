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

using CliInvoke.Core.Configuration;

namespace CliInvoke.Builders;

#pragma warning disable CA1416

/// <summary>
///     Builder class for creating process configurations.
/// </summary>
public sealed class ProcessConfigurationBuilder : IProcessConfigurationBuilder, IDisposable
{
    private string _targetFilePath;
    private bool _outputRedirection;
    
    private string _workingDirectoryPath;
    
    private bool _redirectStandardInput;
    private bool _enableWindowCreation;
    private bool _useShellExecution;
    private bool _requiresAdministratorPrivileges;
    
    private Encoding _standardInputEncoding;
    private Encoding _standardOutputEncoding;
    private Encoding _standardErrorEncoding;

    private StreamWriter _standardInput;

    private readonly ArgumentsSpec _argumentsSpec;
    private readonly EnvironmentVariablesSpec _environmentVariablesSpec;
    private readonly ProcessResourcePolicySpec _processResourcePolicySpec;
    private readonly UserCredentialSpec _userCredentialSpec;

    /// <summary>
    ///     Initialises a new instance of the <see cref="ProcessConfigurationBuilder" /> class,
    ///     which is used to build and configure a process.
    /// </summary>
    /// <param name="targetFilePath">The file path of the target file to be executed.</param>
    /// <param name="argumentValidationLogic">
    ///     Optional validation logic applied to each argument added to the configuration.
    ///     When omitted, a default null-check validation is used.
    /// </param>
    public ProcessConfigurationBuilder(
        string targetFilePath,
        Func<string, bool>? argumentValidationLogic = null)
    {
        _targetFilePath = targetFilePath;
        _argumentsSpec = argumentValidationLogic is not null
            ? new ArgumentsSpec(argumentValidationLogic)
            : new ArgumentsSpec();
        _environmentVariablesSpec = new EnvironmentVariablesSpec();
        _processResourcePolicySpec = new ProcessResourcePolicySpec();
        _userCredentialSpec = new UserCredentialSpec();

        _outputRedirection = false;

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
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="arguments" /> is null.</exception>
    public IProcessConfigurationBuilder SetArguments(
        IEnumerable<string> arguments,
        bool escapeArguments = true)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        _argumentsSpec.Clear();

        List<string> argumentsList = [.. arguments];
        
        if (argumentsList.Count == 0)
            return this;

        _argumentsSpec.AddEnumerable(argumentsList, escape: escapeArguments);
        
        return this;
    }

    /// <summary>
    ///     Sets process arguments to the Process Configuration builder.
    /// </summary>
    /// <param name="arguments">The raw command-line text to be added, stored verbatim without additional quoting or escaping.</param>
    /// <returns>A reference to this builder with the added string arguments, allowing method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="arguments" /> is null or empty.</exception>
    public IProcessConfigurationBuilder SetArguments(string arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        _argumentsSpec.Clear();

        // A single raw string is treated as ready-to-use command-line text and is
        // not wrapped/escaped, matching the former ArgumentsBuilder behaviour.
        _argumentsSpec.Add(arguments, escape: false);

        return this;
    }

    /// <summary>
    ///     Configures the process arguments using the provided configuration action.
    /// </summary>
    /// <param name="configureArguments">An action that accepts an <see cref="ArgumentsSpec" /> and is used to configure the arguments.</param>
    /// <returns>An instance of <see cref="IProcessConfigurationBuilder" /> for further configuration.</returns>
    public IProcessConfigurationBuilder ConfigureArguments(Action<ArgumentsSpec> configureArguments)
    {
        configureArguments.Invoke(_argumentsSpec);

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
    /// Configures the environment variables for the process using the provided configuration action.
    /// </summary>
    /// <param name="configureEnvironmentVariables">An action that accepts an <see cref="EnvironmentVariablesSpec" /> and is used to configure the environment variables.</param>
    /// <returns>An instance of <see cref="IProcessConfigurationBuilder" /> for further configuration.</returns>
    public IProcessConfigurationBuilder ConfigureEnvironmentVariables(
        Action<EnvironmentVariablesSpec> configureEnvironmentVariables)
    {
        configureEnvironmentVariables.Invoke(_environmentVariablesSpec);

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
        return ConfigureUserCredential(spec =>
        {
            if(credential.LoadUserProfile is not null)
                spec.SetUserProfileLoading((bool)credential.LoadUserProfile);
            
            if(credential.Domain is not null)
                spec.SetDomain(credential.Domain);
            
            if(credential.UserName is not null)
                spec.SetUsername(credential.UserName);
            
            if(credential.Password is not null)
                spec.SetPassword(credential.Password);
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
    public IProcessConfigurationBuilder ConfigureUserCredential(Action<UserCredentialSpec> configureCredential)
    {
        ArgumentNullException.ThrowIfNull(configureCredential);

        configureCredential.Invoke(_userCredentialSpec);

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
    /// <param name="outputRedirection"></param>
    /// <returns></returns>
    public IProcessConfigurationBuilder SetOutputRedirection(bool outputRedirection)
    {
        _outputRedirection = outputRedirection;
        
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
        Action<ProcessResourcePolicySpec> configureResourcePolicy)
    {
        ArgumentNullException.ThrowIfNull(configureResourcePolicy);

        configureResourcePolicy.Invoke(_processResourcePolicySpec);

        return this;
    }

    /// <summary>
    /// Configures the resource policy for the process, adjusting settings such as
    /// priority class, priority boost, working set, and processor affinity.
    /// </summary>
    /// <param name="processResourcePolicy">
    /// An instance of <see cref="ProcessResourcePolicy" /> that specifies the configuration
    /// details of the process's resource utilisation.
    /// </param>
    /// <returns>
    /// The current instance of <see cref="IProcessConfigurationBuilder" /> with the updated
    /// resource policy configuration.
    /// </returns>
    public IProcessConfigurationBuilder SetProcessResourcePolicy(
        ProcessResourcePolicy processResourcePolicy)
    {
        return ConfigureProcessResourcePolicy(spec =>
        {
            spec.SetPriorityClass(processResourcePolicy.PriorityClass)
                .ConfigurePriorityBoost(processResourcePolicy.EnablePriorityBoost);

            spec.SetMinWorkingSet(processResourcePolicy.MinWorkingSet);
            spec.SetMaxWorkingSet(processResourcePolicy.MaxWorkingSet);

            spec.SetProcessorAffinity(processResourcePolicy.ProcessorAffinity ??
                                      (nint)ProcessResourcePolicy.Default.ProcessorAffinity);
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
    /// <exception cref="ArgumentException">
    ///     Thrown if shell execution is enabled while standard input is redirected.
    /// </exception>
    [Pure]
    public ProcessConfiguration Build()
    {
        if (_useShellExecution && (_redirectStandardInput || _standardInput != StreamWriter.Null))
            throw new ArgumentException("Using shell execution whilst also redirecting standard input is not supported.");

        string arguments = _argumentsSpec.Build();
        
        IReadOnlyDictionary<string, string> environmentVariables = _environmentVariablesSpec.Build();
        
        ProcessResourcePolicy resourcePolicy = _processResourcePolicySpec.Build();
        UserCredential credential = _userCredentialSpec.Build();

        ProcessConfiguration configuration = new(_targetFilePath, arguments,
            _redirectStandardInput, _outputRedirection,
            _workingDirectoryPath, _requiresAdministratorPrivileges, environmentVariables,
            credential, _standardInput, _standardInputEncoding, _standardOutputEncoding, _standardErrorEncoding, resourcePolicy, _enableWindowCreation,
            _useShellExecution);

        return configuration;
    }
    
    /// <inheritdoc/>
    public void Dispose()
    {
        _userCredentialSpec.Dispose();
        _standardInput.Dispose();
        GC.SuppressFinalize(this);
    }
}

#pragma warning restore CA1416