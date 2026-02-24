/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Linq;

using CliInvoke.Core.Factories;

using DotExtensions.Platforms;
using DotExtensions.Versions;

using WhatExec.Lib.Abstractions;

namespace CliInvoke;

/// <summary>
/// Represents a detector for resolving the default shell on various operating systems.
/// </summary>
public class ShellDetector : IShellDetector
{
    private readonly IProcessInvoker _processInvoker;
    private readonly IExecutableFileResolver _executableFileResolver;
    private readonly IProcessConfigurationFactory _processConfigurationFactory;

    /// <summary>
    /// Represents a detector for resolving the default shell on various operating systems.
    /// </summary>
    public ShellDetector(IProcessInvoker processInvoker,
        IExecutableFileResolver executableFileResolver,
        IProcessConfigurationFactory processConfigurationFactory)
    {
        _processInvoker = processInvoker;
        _executableFileResolver = executableFileResolver;
        _processConfigurationFactory = processConfigurationFactory;
    }

    /// <summary>
    /// Resolves the default shell asynchronously on supported operating systems.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation, returning a ShellInformation object with details about the detected shell.</returns>
    [UnsupportedOSPlatform("IOS")]
    [UnsupportedOSPlatform("tvOS")]
    [UnsupportedOSPlatform("browser")]
    public async Task<ShellInformation> ResolveDefaultShellAsync(
        CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsUnix())
            return await ResolveDefaultShellOnUnixAsync(cancellationToken);
     
        if(OperatingSystem.IsWindows())
            return await ResolveDefaultShellOnWindowsAsync(cancellationToken);
        
        throw new PlatformNotSupportedException();
    }
    
    private async Task<ShellInformation> ResolveDefaultShellOnUnixAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.Register(() => throw new TaskCanceledException());
        
        using ProcessConfiguration execConfiguration = _processConfigurationFactory
            .Create("ps", "-p $$ -o comm=");

        BufferedProcessResult execResult = await _processInvoker.ExecuteBufferedAsync(
            execConfiguration, ProcessExitConfiguration.Default, false,
            cancellationToken);

        FileInfo shellExeInfo = await _executableFileResolver.LocateExecutableAsync(execResult.StandardOutput.Split(Environment.NewLine).First(),
            SearchOption.AllDirectories, cancellationToken);

        using ProcessConfiguration shellInfoProcessConfig = _processConfigurationFactory
            .Create(shellExeInfo.FullName, "--version");

        BufferedProcessResult shellInfoResult = await _processInvoker.ExecuteBufferedAsync(
            shellInfoProcessConfig, ProcessExitConfiguration.Default, false,
            cancellationToken);
        
        string versionLine = shellInfoResult.StandardOutput.Split(Environment.NewLine).First(l => l.ToLower().Contains("version") &&
            l.Any(c => char.IsDigit(c)));

        string[] commaSplit = versionLine.Split(',');
        
        string shellPrettyName = commaSplit.First();

        string versionString = commaSplit.Last().Replace(".", string.Empty);
        
        Version shellVersion = Version.GracefulParse(versionString);
        
        return new ShellInformation(shellPrettyName, 
            shellExeInfo, shellVersion);
    }

    [SupportedOSPlatform("windows")]
    private async Task<ShellInformation> ResolveDefaultShellOnWindowsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            FileInfo powershell5PlusFileInfo = await _executableFileResolver.LocateExecutableAsync("pwsh.exe", SearchOption.AllDirectories, cancellationToken);
            
            using ProcessConfiguration powershellConfig = _processConfigurationFactory
                .Create(powershell5PlusFileInfo.FullName, "");
            
            BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(powershellConfig, 
                ProcessExitConfiguration.Default, false,  cancellationToken);
            
            string[] powershellResults = result.StandardOutput.Replace("v", string.Empty).Split(' ');

            string versionString = powershellResults.Last();
            versionString = versionString.Substring(0, versionString.LastIndexOf('.') + 1);
            
            Version version = Version.GracefulParse(versionString);

            return new ShellInformation(powershellResults.First(), powershell5PlusFileInfo,
                version);
        }
        catch
        {
            FileInfo cmdExeInfo = await _executableFileResolver.LocateExecutableAsync("cmd.exe",  SearchOption.AllDirectories, cancellationToken);

            using ProcessConfiguration cmdConfig = _processConfigurationFactory
                .Create(cmdExeInfo.FullName, "");
            
            BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(cmdConfig,
                ProcessExitConfiguration.Default, false,  cancellationToken);
            
            string line = result.StandardOutput.Split(Environment.NewLine).First();

            string versionString = line.Replace("Microsoft", string.Empty).Replace("Windows", string.Empty).Replace("]", string.Empty);
            Version cmdVersion = Version.GracefulParse(versionString.Split('[')[1]
                .Replace("Version", "")
                .Replace(" ", string.Empty));

            return new ShellInformation("cmd", cmdExeInfo, cmdVersion);
        }
    }
}