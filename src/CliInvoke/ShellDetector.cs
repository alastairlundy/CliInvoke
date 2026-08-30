/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Linq;

using CliInvoke.Internal.Versions;

namespace CliInvoke;

/// <summary>
///     Represents a detector for resolving the default shell on various operating systems.
/// </summary>
public class ShellDetector : IShellDetector
{
    private readonly IFilePathResolver _filePathResolver;
    private readonly IProcessInvoker _processInvoker;

    private readonly bool isUnix;

    /// <summary>
    ///     Represents a detector for resolving the default shell on various operating systems.
    /// </summary>
    public ShellDetector(IProcessInvoker processInvoker, IFilePathResolver filePathResolver)
    {
        _processInvoker = processInvoker;
        _filePathResolver = filePathResolver;
        
        isUnix = OperatingSystem.IsAndroid() || OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD() ||
                 OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst();
    }

    /// <summary>
    ///     Resolves the default shell asynchronously on supported operating systems.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the asynchronous operation.</param>
    /// <returns>
    ///     A task representing the asynchronous operation, returning a ShellInformation object with
    ///     details about the detected shell.
    /// </returns>
    [UnsupportedOSPlatform("IOS")]
    [UnsupportedOSPlatform("tvOS")]
    [UnsupportedOSPlatform("browser")]
    public async Task<ShellInformation> ResolveDefaultShellAsync(
        CancellationToken cancellationToken = default)
    {
        if (isUnix)
            return await ResolveDefaultShellOnUnixAsync(cancellationToken);

        if (OperatingSystem.IsWindows())
            return await ResolveDefaultShellOnWindowsAsync(cancellationToken);

        throw new PlatformNotSupportedException();
    }

    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    private async Task<ShellInformation> ResolveDefaultShellOnUnixAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.Register(() => throw new TaskCanceledException());

        ProcessConfiguration execConfiguration = ProcessConfigurationFactory
            .Create("ps", "-p $$ -o comm=");

        BufferedProcessResult execResult = await _processInvoker.ExecuteBufferedAsync(
            execConfiguration, ProcessExitConfiguration.CreateGraceful(), cancellationToken);

        FileInfo shellExeInfo = _filePathResolver.ResolveFilePath(
            GetFirstLine(execResult.StandardOutput));

        ProcessConfiguration shellInfoProcessConfig = ProcessConfigurationFactory
            .Create(shellExeInfo.FullName, "--version");

        BufferedProcessResult shellInfoResult = await _processInvoker.ExecuteBufferedAsync(
            shellInfoProcessConfig, ProcessExitConfiguration.CreateGraceful(), cancellationToken);

        string? versionLine = null;

        foreach (var line in shellInfoResult.StandardOutput.AsSpan().EnumerateLines())
        {
            if (line.Contains("version".AsSpan(), StringComparison.OrdinalIgnoreCase) &&
                line.IndexOfAny("0123456789".AsSpan()) >= 0)
            {
                versionLine = line.ToString();
                break;
            }
        }

        if (versionLine is null)
            throw new InvalidOperationException("No version line was found in the shell output.");

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
            FileInfo powershell5PlusFileInfo = _filePathResolver.ResolveFilePath("pwsh.exe");

            ProcessConfiguration powershellConfig = ProcessConfigurationFactory
                .Create(powershell5PlusFileInfo.FullName, "");

            BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(
                powershellConfig,
                ProcessExitConfiguration.CreateGraceful(), cancellationToken);

            string[] powershellResults =
                result.StandardOutput.Replace("v", string.Empty).Split(' ');

            string versionString = powershellResults.Last();
            versionString = versionString[..(versionString.LastIndexOf('.') + 1)];

            Version version = Version.GracefulParse(versionString);

            return new ShellInformation(powershellResults.First(), powershell5PlusFileInfo,
                version);
        }
        catch
        {
            FileInfo cmdExeInfo = _filePathResolver.ResolveFilePath("cmd.exe");

            ProcessConfiguration cmdConfig = ProcessConfigurationFactory
                .Create(cmdExeInfo.FullName, "");

            BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(cmdConfig,
                ProcessExitConfiguration.CreateGraceful(), cancellationToken);

            string line = GetFirstLine(result.StandardOutput);

            string versionString = line.Replace("Microsoft", string.Empty)
                .Replace("Windows", string.Empty).Replace("]", string.Empty);
            Version cmdVersion = Version.GracefulParse(versionString.Split('[')[1]
                .Replace("Version", "")
                .Replace(" ", string.Empty));

            return new ShellInformation("cmd", cmdExeInfo, cmdVersion);
        }
    }

    /// <summary>
    ///     Returns the first line of the supplied text without allocating a full line array.
    /// </summary>
    private static string GetFirstLine(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        foreach (var line in text.AsSpan().EnumerateLines())
            return line.ToString();

        return string.Empty;
    }
}
