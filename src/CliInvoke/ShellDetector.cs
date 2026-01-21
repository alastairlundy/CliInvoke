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

namespace CliInvoke;

public class ShellDetector : IShellDetector
{
    private readonly IProcessInvoker _processInvoker;
    private readonly IFilePathResolver _filePathResolver;
    private readonly IProcessConfigurationFactory _processConfigurationFactory;

    public ShellDetector(IProcessInvoker processInvoker,
        IFilePathResolver filePathResolver,
        IProcessConfigurationFactory processConfigurationFactory)
    {
        _processInvoker = processInvoker;
        _filePathResolver = filePathResolver;
        _processConfigurationFactory = processConfigurationFactory;
    }
    
    [UnsupportedOSPlatform("IOS")]
    [UnsupportedOSPlatform("tvOS")]
    [UnsupportedOSPlatform("browser")]
    public async Task<ShellInformation> ResolveDefaultShellAsync(
        CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsUnix())
            return await ResolveDefaultShellOnUnixAsync(cancellationToken);
        
        return await ResolveDefaultShellOnWindowsAsync(cancellationToken);
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

        string shellExe = _filePathResolver.ResolveFilePath(execResult.StandardOutput.Split(Environment.NewLine).First());

        using ProcessConfiguration shellInfoProcessConfig = _processConfigurationFactory
            .Create(shellExe, "--version");

        BufferedProcessResult shellInfoResult = await _processInvoker.ExecuteBufferedAsync(
            shellInfoProcessConfig,
            ProcessExitConfiguration.Default, false, cancellationToken);

        string[] infoResults = shellInfoResult.StandardOutput.Split(' ');

        string shellPrettyName;

        Version shellVersion;

        return new ShellInformation(shellPrettyName, 
            new FileInfo(shellExe), shellVersion);
    }

    private async Task<ShellInformation> ResolveDefaultShellOnWindowsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            string powershell5Plus = _filePathResolver.ResolveFilePath("pwsh.exe");
            
            using ProcessConfiguration powershellConfig = 
        }
        catch
        {
            string cmdExe = _filePathResolver.ResolveFilePath("cmd.exe");

            using ProcessConfiguration cmdConfig = _processConfigurationFactory
                .Create(cmdExe, "");

            Version cmdVersion;

            return new ShellInformation("cmd", new FileInfo(cmdExe), cmdVersion);
        }
    }
}