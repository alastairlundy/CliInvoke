/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// ReSharper disable RedundantBoolCompare

namespace CliInvoke.Specializations.Configurations;

/// <summary>
/// A Command configuration to make running commands through cross-platform PowerShell easier.
/// </summary>
[SupportedOSPlatform("windows")]
[SupportedOSPlatform("macos")]
[SupportedOSPlatform("maccatalyst")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("freebsd")]
[UnsupportedOSPlatform("browser")]
[UnsupportedOSPlatform("android")]
[UnsupportedOSPlatform("ios")]
[UnsupportedOSPlatform("tvos")]
[UnsupportedOSPlatform("watchos")]
public class PowershellProcessConfiguration : ProcessConfiguration
{
    /// <summary>
    /// Initializes a new instance of the PowershellCommandConfiguration class.
    /// </summary>
    /// <param name="filePathResolver"></param>
    /// <param name="arguments">The arguments to be passed to the command.</param>
    /// <param name="workingDirectoryPath">The working directory for the command.</param>
    /// <param name="requiresAdministrator">Indicates whether the command requires administrator privileges.</param>
    /// <param name="environmentVariables">A dictionary of environment variables to be set for the command.</param>
    /// <param name="credentials">The user credentials to be used when running the command.</param>
    /// <param name="standardInput">The stream for the standard input.</param>
    /// <param name="standardOutput">The stream for the standard output.</param>
    /// <param name="standardError">The stream for the standard error.</param>
    /// <param name="standardInputEncoding">The encoding for the standard input stream.</param>
    /// <param name="standardOutputEncoding">The encoding for the standard output stream.</param>
    /// <param name="standardErrorEncoding">The encoding for the standard error stream.</param>
    /// <param name="processResourcePolicy">The processor resource policy for the command.</param>
    /// <param name="useShellExecution">Indicates whether to use the shell to execute the command.</param>
    /// <param name="windowCreation">Indicates whether to create a new window for the command.</param>
    /// <param name="redirectStandardInput"></param>
    /// <param name="redirectStandardOutput"></param>
    /// <param name="redirectStandardError"></param>
    public PowershellProcessConfiguration(IFilePathResolver filePathResolver, string arguments,
        bool redirectStandardInput, bool redirectStandardOutput, bool redirectStandardError,
        string? workingDirectoryPath = null, bool requiresAdministrator = false,
        Dictionary<string, string>? environmentVariables = null, UserCredential? credentials = null,
        StreamWriter? standardInput = null, StreamReader? standardOutput = null, StreamReader? standardError = null,
        Encoding? standardInputEncoding = null, Encoding? standardOutputEncoding = null,
        Encoding? standardErrorEncoding = null, ProcessResourcePolicy? processResourcePolicy = null,
        bool useShellExecution = false, bool windowCreation = false) : base("",
        redirectStandardInput, redirectStandardOutput, redirectStandardError,
        arguments, workingDirectoryPath,
        requiresAdministrator, environmentVariables,
        credentials,
        standardInput, standardOutput, standardError,
        standardInputEncoding, standardOutputEncoding,
        standardErrorEncoding, processResourcePolicy,
        windowCreation: windowCreation,
        useShellExecution: useShellExecution)
    {
        string filePath;

        if (OperatingSystem.IsWindows())
        {
            try
            {
                filePath = filePathResolver.ResolveFilePath("pwsh.exe");
            }
            catch
            {
                filePath = $"{GetInstallLocationOnWindows()}";
            }
        }
        else if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() ||
                 OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            filePath = filePathResolver.ResolveFilePath("pwsh");
        }
        else
        {
            throw new PlatformNotSupportedException(Resources.Exceptions_Powershell_OnlySupportedOnDesktop);
        }

        TargetFilePath = filePath;
        base.TargetFilePath = TargetFilePath;
    }

    /// <summary>
    /// The target file path of cross-platform PowerShell.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Thrown if run on an operating system besides Windows, macOS, Linux, and FreeBSD.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("watchos")]
    public new string TargetFilePath
    {
        get; private set;
    }

    private static string GetInstallLocationOnWindows()
    {
        string programFiles = Environment.GetFolderPath(Environment.Is64BitOperatingSystem == true
            ? Environment.SpecialFolder.ProgramFiles
            : Environment.SpecialFolder.ProgramFilesX86);

        IEnumerable<string> directories = Directory.EnumerateDirectories(
                $"{programFiles}{Path.DirectorySeparatorChar}Powershell")
            .Where(d => Regex.IsMatch(d, @"v\d+"))
            .OrderByDescending(d => int.TryParse(d.Substring(1), out int _));

        foreach (string directory in directories)
        {
            string expectedFilePath = $"{directory}{Path.DirectorySeparatorChar}pwsh.exe";
            
            if (File.Exists(expectedFilePath))
                return expectedFilePath;
        }

        throw new FileNotFoundException(Resources.Exceptions_Powershell_NotInstalled);
    }
}