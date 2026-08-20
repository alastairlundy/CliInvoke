/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Text;

using CliInvoke.Builders;
using CliInvoke.Core.Configuration;

namespace CliInvoke;

/// <summary>
/// A factory class for creating instances of <see cref="ProcessConfiguration"/>.
/// Provides multiple overloads for creating process configurations tailored to different use cases.
/// </summary>
public static class ProcessConfigurationFactory
{
    /// <summary>
    ///     Creates a Process configuration that can be run by a <see cref="IProcessInvoker" /> from
    ///     specified parameters.
    /// </summary>
    /// <param name="targetFilePath">The target file path of the command to be executed.</param>
    /// <param name="arguments">The arguments to pass to the Command upon execution.</param>
    /// <param name="workingDirectory">The working directory for the process.</param>
    /// <param name="outputRedirection">
    ///     Whether to redirect standard output and standard error.
    ///     <remarks>
    ///     Note: <see cref="ProcessConfigurationBuilder._outputRedirection"/> defaults to <c>false</c>;
    ///     the factory's default is <c>true</c>. Set <paramref name="outputRedirection"/> to <c>false</c> if output redirection is not desired.
    ///     </remarks>
    /// </param>
    /// <param name="enableWindowCreation">Whether to enable window creation for the process.</param>
    /// <returns>The <see cref="ProcessConfiguration" /> created from the configured parameters.</returns>
    [Pure]
    public static ProcessConfiguration Create(
        string targetFilePath,
        string arguments,
        string? workingDirectory = null,
        bool outputRedirection = true,
        bool enableWindowCreation = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetFilePath);
        ArgumentNullException.ThrowIfNull(arguments);

        if (workingDirectory is not null && !Directory.Exists(workingDirectory))
            throw new ArgumentException($"Working directory '{workingDirectory}' does not exist.", nameof(workingDirectory));

        IReadOnlyDictionary<string, string> environmentVariables = new EnvironmentVariablesSpec().Build();
        ProcessResourcePolicy processResourcePolicy = new ProcessResourcePolicySpec().Build();
        UserCredential? credential = new UserCredentialSpec().Build();

        return new ProcessConfiguration(
            targetFilePath,
            arguments,
            redirectStandardInput: false,
            outputRedirection,
            workingDirectoryPath: workingDirectory,
            requiresAdministrator: false,
            environmentVariables,
            credential,
            standardInput: StreamWriter.Null,
            standardInputEncoding: Encoding.Default,
            standardOutputEncoding: Encoding.Default,
            standardErrorEncoding: Encoding.Default,
            processResourcePolicy,
            windowCreation: enableWindowCreation,
            useShellExecution: false);
    }

    /// <summary>
    ///     Creates a Process configuration that can be run by a <see cref="IProcessInvoker" /> from
    ///     specified parameters with spec-based callbacks for environment variables, resource policy, and credentials.
    /// </summary>
    /// <param name="targetFilePath">The target file path of the command to be executed.</param>
    /// <param name="arguments">The arguments to pass to the Command upon execution.</param>
    /// <param name="workingDirectory">The working directory for the process.</param>
    /// <param name="outputRedirection">
    ///     Whether to redirect standard output and standard error.
    ///     <remarks>
    ///     Note: <c>ProcessConfigurationBuilder._outputRedirection</c> defaults to <c>false</c>;
    ///     the factory's default is <c>true</c>. Use the builder when you need explicit output redirection control.
    ///     </remarks>
    /// </param>
    /// <param name="enableWindowCreation">Whether to enable window creation for the process.</param>
    /// <param name="configureEnvironmentVariables">
    ///     An optional callback to configure environment variables via <see cref="EnvironmentVariablesSpec"/>.
    ///     If non-null, a fresh spec is created, the callback is invoked, and <c>spec.Build()</c> is passed to the constructor.
    ///     Exceptions from the callback propagate unchanged.
    /// </param>
    /// <param name="configureResourcePolicy">
    ///     An optional callback to configure the process resource policy via <see cref="ProcessResourcePolicySpec"/>.
    ///     If non-null, a fresh spec is created, the callback is invoked, and <c>spec.Build()</c> is passed to the constructor.
    ///     Exceptions from the callback propagate unchanged.
    /// </param>
    /// <param name="configureCredential">
    ///     An optional callback to configure user credentials via <see cref="UserCredentialSpec"/>.
    ///     If non-null, a fresh spec is created, the callback is invoked, and <c>spec.Build()</c> is passed to the constructor.
    ///     Exceptions from the callback propagate unchanged.
    /// </param>
    /// <returns>The <see cref="ProcessConfiguration" /> created from the configured parameters.</returns>
    [Pure]
    public static ProcessConfiguration Create(
        string targetFilePath,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        bool outputRedirection = true,
        bool enableWindowCreation = false,
        Action<EnvironmentVariablesSpec>? configureEnvironmentVariables = null,
        Action<ProcessResourcePolicySpec>? configureResourcePolicy = null,
        Action<UserCredentialSpec>? configureCredential = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetFilePath);
        ArgumentNullException.ThrowIfNull(arguments);

        List<string> argumentsList = arguments.ToList();

        if (argumentsList.Count == 0)
            throw new ArgumentException("Arguments cannot be empty.", nameof(arguments));

        if (workingDirectory is not null && !Directory.Exists(workingDirectory))
            throw new ArgumentException($"Working directory '{workingDirectory}' does not exist.", nameof(workingDirectory));

        ArgumentsSpec argumentsSpec = new();
        argumentsSpec.AddEnumerable(argumentsList, escape: true);
        string builtArguments = argumentsSpec.Build();

        EnvironmentVariablesSpec environmentVariablesSpec = new();
        configureEnvironmentVariables?.Invoke(environmentVariablesSpec);
        IReadOnlyDictionary<string, string> environmentVariables = environmentVariablesSpec.Build();

        ProcessResourcePolicySpec processResourcePolicySpec = new();
        configureResourcePolicy?.Invoke(processResourcePolicySpec);
        ProcessResourcePolicy processResourcePolicy = processResourcePolicySpec.Build();

        UserCredentialSpec userCredentialSpec = new();
        configureCredential?.Invoke(userCredentialSpec);
        UserCredential? credential = userCredentialSpec.Build();

        return new ProcessConfiguration(
            targetFilePath,
            builtArguments,
            redirectStandardInput: false,
            outputRedirection,
            workingDirectoryPath: workingDirectory,
            requiresAdministrator: false,
            environmentVariables,
            credential,
            standardInput: StreamWriter.Null,
            standardInputEncoding: Encoding.Default,
            standardOutputEncoding: Encoding.Default,
            standardErrorEncoding: Encoding.Default,
            processResourcePolicy,
            windowCreation: enableWindowCreation,
            useShellExecution: false);
    }
}