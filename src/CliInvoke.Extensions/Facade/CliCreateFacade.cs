/*
    AlastairLundy.CliInvoke.Extensions
     
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Core.Builders;

namespace AlastairLundy.CliInvoke.Extensions;


public static partial class Cli
{
    /// <summary>
    /// Creates a Process configuration that can be run by a <see cref="IProcessConfigurationInvoker"/> from specified parameters.
    /// </summary>
    /// <param name="targetFilePath">The target file path of the command to be executed.</param>
    /// <param name="arguments">The arguments to pass to the Command upon execution.</param>
    /// <param name="workingDirectory">The working directory to be used.</param>
    /// <param name="redirectStandardOutput">Whether to redirect Standard Output, set to true by default.</param>
    /// <param name="redirectStandardError">Whether to redirect Standard Error, set to true by default.</param>
    /// <param name="processConfigBuilderActions">Actions to apply to the internal <see cref="IProcessConfigurationBuilder"/> if not null.</param>
    /// <returns>The <see cref="ProcessConfiguration"/> created from the configured parameters.</returns>
#if NET8_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
#endif
    public static ProcessConfiguration CreateCommand(string targetFilePath, string arguments,
        string? workingDirectory = null, bool redirectStandardOutput = true, bool redirectStandardError = true,
        Action<IProcessConfigurationBuilder>? processConfigBuilderActions = null)
        => CreateCommand(targetFilePath, [arguments], workingDirectory, redirectStandardOutput,
            redirectStandardError, processConfigBuilderActions);

    /// <summary>
    /// Creates a Process configuration that can be run by a <see cref="IProcessConfigurationInvoker"/> from specified parameters.
    /// </summary>
    /// <param name="targetFilePath">The target file path of the command to be executed.</param>
    /// <param name="arguments">The arguments to pass to the Command upon execution.</param>
    /// <param name="workingDirectory">The working directory to be used.</param>
    /// <param name="redirectStandardOutput">Whether to redirect Standard Output, set to true by default.</param>
    /// <param name="redirectStandardError">Whether to redirect Standard Error, set to true by default.</param>
    /// <param name="processConfigBuilderActions">Actions to apply to the internal <see cref="IProcessConfigurationBuilder"/> if not null.</param>
    /// <returns>The <see cref="ProcessConfiguration"/> created from the configured parameters.</returns>
#if NET8_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
#endif
    public static ProcessConfiguration CreateCommand(string targetFilePath, IEnumerable<string> arguments,
        string? workingDirectory = null, bool redirectStandardOutput = true, bool redirectStandardError = true,
        Action<IProcessConfigurationBuilder>? processConfigBuilderActions = null)
    {
        IArgumentsBuilder argumentsBuilder = new ArgumentsBuilder()
            .Add(arguments);

        IProcessConfigurationBuilder processConfigurationBuilder = new ProcessConfigurationBuilder(targetFilePath)
            .WithArguments(argumentsBuilder.ToString())
            .WithWorkingDirectory(workingDirectory ?? Environment.CurrentDirectory)
            .RedirectStandardOutput(redirectStandardOutput)
            .RedirectStandardError(redirectStandardError)
            .WithWindowCreation(false);
        
        if(processConfigBuilderActions is not null)
            processConfigBuilderActions.Invoke(processConfigurationBuilder);
        
        return processConfigurationBuilder.Build();
    }
}