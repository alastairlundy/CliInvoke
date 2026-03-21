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
/// Builder class for creating process configurations.
/// </summary>
public class ProcessConfigurationBuilder : IProcessConfigurationBuilder, IDisposable
{
    private readonly ProcessConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessConfigurationBuilder"/> class,
    /// which is used to build and configure a process.
    /// </summary>
    /// <param name="targetFilePath">The file path of the target file to be executed.</param>
    public ProcessConfigurationBuilder(string targetFilePath)
    {
        _configuration = new(targetFilePath,
            false, false,
            false);
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
    public IProcessConfigurationBuilder SetArguments(IEnumerable<string> arguments) 
        => SetArguments(arguments, false);

    /// <summary>
    /// Adds process arguments to the Process Configuration builder.
    /// </summary>
    /// <param name="arguments">The process arguments to be added or updated.</param>
    /// <param name="escapeArguments">Whether the arguments should be escaped.</param>
    /// <returns>A reference to this builder with the added arguments, allowing method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref cref="arguments"/> is null.</exception>
    [Pure]
    public IProcessConfigurationBuilder SetArguments(
        IEnumerable<string> arguments,
        bool escapeArguments
    )
    {
        ArgumentNullException.ThrowIfNull(arguments);

        IArgumentsBuilder argumentsBuilder = new ArgumentsBuilder().AddEnumerable(
            arguments,
            escapeArguments
        );

        string args = argumentsBuilder.ToString();

        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                _configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                args,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution
            )
        );
    }

    /// <summary>
    /// Adds process arguments to the Process Configuration builder.
    /// </summary>
    /// <param name="arguments">The argument string to be added.</param>
    /// <returns>A reference to this builder with the added string arguments, allowing method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="arguments"/> is null or empty.</exception>
    [Pure]
    public IProcessConfigurationBuilder SetArguments(string arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                _configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution
            )
        );
    }

    /// <summary>
    /// Sets the target file path for the process configuration.
    /// </summary>
    /// <param name="targetFilePath">The file path where the process configuration will be saved.</param>
    /// <returns>A reference to this builder with the updated target file path, allowing method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="targetFilePath"/> is null or empty.</exception>
    [Pure]
    public IProcessConfigurationBuilder SetTargetFilePath(string targetFilePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFilePath);
        
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                targetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution
            )
        );
    }

    /// <summary>
    /// Sets environment variables for the process configuration.
    /// </summary>
    /// <param name="environmentVariables">The environment variables to be added to the process configuration.</param>
    /// <returns>A reference to this builder with the updated target file path, allowing method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="environmentVariables"/> is null.</exception>
    [Pure]
    public IProcessConfigurationBuilder SetEnvironmentVariables(
        IReadOnlyDictionary<string, string> environmentVariables
    )
    {
        ArgumentNullException.ThrowIfNull(environmentVariables);

        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                _configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                environmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution
            )
        );
    }

    /// <summary>
    /// Configures the process to run with administrator privileges.
    /// </summary>
    /// <returns>A reference to this builder with the updated administrator privileges,
    /// allowing method chaining.</returns>
    [Pure]
    public IProcessConfigurationBuilder RequireAdministratorPrivileges()
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                _configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                true,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution
            )
        );
    }

    /// <summary>
    /// Sets the working directory path for the process configuration.
    /// </summary>
    /// <param name="workingDirectoryPath">The file system path where the process will be executed.</param>
    /// <returns>A reference to this builder, allowing method chaining.</returns>
    [Pure]
    public IProcessConfigurationBuilder SetWorkingDirectory(string workingDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(workingDirectoryPath);
        
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                _configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                workingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution
            )
        );
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
    public IProcessConfigurationBuilder SetUserCredential(UserCredential credential)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                _configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                credential,
                _configuration.StandardInput,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution
            )
        );
    }

    /// <summary>
    /// Sets the credentials for the Command to be executed.
    /// </summary>
    /// <param name="configure">The CredentialsBuilder configuration.</param>
    /// <returns>The new CommandBuilder with the specified Credentials.</returns>
    /// <remarks>Credentials are only supported with the Process class on Windows. This is a limitation of .NET's Process class.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="configure"/> is null.</exception>
    [Pure]
    [SupportedOSPlatform("windows")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("android")]
    public IProcessConfigurationBuilder SetUserCredential(Action<IUserCredentialBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        
        IUserCredentialBuilder credentialBuilder = new UserCredentialBuilder();
        
        if (_configuration.Credential.Domain is not null) 
            credentialBuilder.SetDomain(_configuration.Credential.Domain);

        if (_configuration.Credential.Password is not null)
            credentialBuilder.SetPassword(_configuration.Credential.Password);
                
        if(_configuration.Credential.UserName is not null)
                credentialBuilder.SetUsername(_configuration.Credential.UserName);
        
        credentialBuilder.LoadUserProfile(_configuration.Credential.LoadUserProfile ?? false);

        configure(credentialBuilder);

        return SetUserCredential(credentialBuilder.Build());
    }

    /// <summary>
    /// Configures whether the standard input of the process should be redirected.
    /// </summary>
    /// <param name="redirectStandardInput">A value indicating whether standard input redirection is enabled.</param>
    /// <returns>An instance of <see cref="IProcessConfigurationBuilder"/> with the updated configuration.</returns>
    [Pure]
    public IProcessConfigurationBuilder RedirectStandardInput(bool redirectStandardInput) =>
        new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                _configuration.TargetFilePath,
                redirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput ?? StreamWriter.Null,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution
            )
        );

    /// <summary>
    /// Configures whether the standard output of the process should be redirected.
    /// </summary>
    /// <param name="redirectStandardOutput">A boolean value indicating whether to redirect the standard output of the process.</param>
    /// <returns>An instance of <see cref="IProcessConfigurationBuilder"/> with the updated configuration.</returns>
    [Pure]
    public IProcessConfigurationBuilder RedirectStandardOutput(bool redirectStandardOutput) =>
        new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                _configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                redirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution
            )
        );

    /// <summary>
    /// Configures the process to redirect the standard error stream.
    /// </summary>
    /// <param name="redirectStandardError">Defines whether the standard error stream should be redirected.</param>
    /// <returns>An instance of <see cref="IProcessConfigurationBuilder"/> with the updated configuration.</returns>
    [Pure]
    public IProcessConfigurationBuilder RedirectStandardError(bool redirectStandardError) =>
        new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                _configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                redirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution
            )
        );

    /// <summary>
    /// Sets the Standard Input Pipe source.
    /// </summary>
    /// <param name="source">The source to use for the Standard Input pipe.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified Standard Input pipe source.</returns>
    /// <remarks>Using Shell Execution whilst also Redirecting Standard Input will throw an Exception.
    /// This is a known issue with the System Process class.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardinput"/>
    [Pure]
    public IProcessConfigurationBuilder SetStandardInputPipe(StreamWriter source)
    {
        ArgumentNullException.ThrowIfNull(source);
        
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                _configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                source,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution
            )
        );
    }

    /// <summary>
    /// Sets the policy for managing process resources.
    /// </summary>
    /// <param name="processResourcePolicy">The policy that determines how the process resource is managed.</param>
    /// <returns>A reference to this builder with the updated Process Resource Policy,
    /// allowing method chaining.</returns>
    [Pure]
    public IProcessConfigurationBuilder SetProcessResourcePolicy(
        ProcessResourcePolicy processResourcePolicy
    )
    {
        ArgumentNullException.ThrowIfNull(processResourcePolicy);
        
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                _configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                processResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution
            )
        );
    }

    /// <summary>
    /// Configures whether shell execution should be used for the process.
    /// </summary>
    /// <param name="useShellExecution">True to use shell execution, false otherwise.</param>
    /// <returns>The updated Process Configuration builder with the updated configuration information.</returns>
    /// <remarks>Using Shell Execution whilst also Redirecting Standard Input will throw an Exception.
    /// This is a known issue with the System Process class.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardinput"/>
    [Pure]
    public IProcessConfigurationBuilder ConfigureShellExecution(bool useShellExecution)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                _configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: useShellExecution
            )
        );
    }

    /// <summary>
    /// Configures the process builder to enable or disable window creation.
    /// </summary>
    /// <param name="enableWindowCreation">A boolean indicating whether to enable or disable window creation.</param>
    /// <returns>The updated Process Configuration builder with the updated window creation configuration.</returns>
    [Pure]
    public IProcessConfigurationBuilder ConfigureWindowCreation(bool enableWindowCreation)
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                _configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                _configuration.ResourcePolicy,
                windowCreation: enableWindowCreation,
                useShellExecution: _configuration.UseShellExecution
            )
        );
    }

    /// <summary>
    /// Configures the process builder to use specific encoding schemes for standard input, output, and error streams.
    /// </summary>
    /// <param name="standardInputEncoding">The encoding scheme to use for standard input.
    /// Uses the Default Encoding if null.</param>
    /// <returns>The updated Process Configuration builder with the updated encoding scheme configuration information.</returns>
    [Pure]
    public IProcessConfigurationBuilder SetStandardInputEncoding(
        Encoding? standardInputEncoding = null
    )
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                _configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                standardInputEncoding ?? Encoding.Default,
                _configuration.StandardOutputEncoding,
                _configuration.StandardErrorEncoding,
                processResourcePolicy: _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution
            )
        );
    }

    /// <summary>
    /// Configures the process builder to use specific encoding schemes for Standard output and error streams.
    /// </summary>
    /// <param name="standardOutputEncoding">The encoding scheme to use for standard output.
    /// Uses the Default Encoding if null.</param>
    /// <returns>The updated Process Configuration builder with the updated encoding scheme configuration information.</returns>
    [Pure]
    public IProcessConfigurationBuilder SetStandardOutputEncoding(
        Encoding? standardOutputEncoding = null
    )
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                _configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardInputEncoding,
                standardOutputEncoding ?? Encoding.Default,
                _configuration.StandardErrorEncoding,
                processResourcePolicy: _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution
            )
        );
    }

    /// <summary>
    /// Configures the process builder to use specific encoding schemes for Standard Error streams.
    /// </summary>
    /// <param name="standardErrorEncoding">The encoding scheme to use for standard error.
    /// Uses the Default Encoding if null.</param>
    /// <returns>The updated Process Configuration builder with the updated encoding scheme configuration information.</returns>
    [Pure]
    public IProcessConfigurationBuilder SetStandardErrorEncoding(
        Encoding? standardErrorEncoding = null
    )
    {
        return new ProcessConfigurationBuilder(
            new ProcessConfiguration(
                _configuration.TargetFilePath,
                _configuration.RedirectStandardInput,
                _configuration.RedirectStandardOutput,
                _configuration.RedirectStandardError,
                _configuration.Arguments,
                _configuration.WorkingDirectoryPath,
                _configuration.RequiresAdministrator,
                _configuration.EnvironmentVariables,
                _configuration.Credential,
                _configuration.StandardInput,
                _configuration.StandardInputEncoding,
                _configuration.StandardOutputEncoding,
                standardErrorEncoding ?? Encoding.Default,
                processResourcePolicy: _configuration.ResourcePolicy,
                windowCreation: _configuration.WindowCreation,
                useShellExecution: _configuration.UseShellExecution
            )
        );
    }

    /// <summary>
    /// Builds and returns a ProcessConfiguration object with the specified properties.
    /// </summary>
    /// <returns>The configured ProcessConfiguration object.</returns>
    public ProcessConfiguration Build() => _configuration;

    /// <summary>
    /// Releases all resources used by the <see cref="ProcessConfigurationBuilder"/> instance.
    /// This includes disposing of the associated <see cref="ProcessConfiguration"/> object.
    /// </summary>
    public void Dispose()
    {
        _configuration.Dispose();
    }
}

#pragma warning restore CA1416
