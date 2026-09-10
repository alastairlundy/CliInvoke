/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CliInvoke.Specializations.Configurations;

/// <summary>
///     A Command configuration to make running commands through cross-platform PowerShell easier.
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
    ///     Gets the resolved file path of the PowerShell executable for the current operating system,
    ///     delegating to the base configuration's per-OS resolution.
    /// </summary>
    public new string TargetFilePath => base.TargetFilePath;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PowershellProcessConfiguration"/> class.
    /// </summary>
    /// <param name="arguments">The arguments to be passed to the command.</param>
    /// <param name="outputRedirection">Whether to redirect standard output and error streams.</param>
    /// <param name="workingDirectoryPath">The working directory for the command.</param>
    /// <param name="requiresAdministrator"> Indicates whether the command requires administrator privileges.</param>
    /// <param name="environmentVariables">A dictionary of environment variables to be set for the command.</param>
    /// <param name="credentials">The user credentials to be used when running the command.</param>
    /// <param name="standardInput">The stream for the standard input.</param>
    /// <param name="standardInputEncoding">The encoding for the standard input stream.</param>
    /// <param name="standardOutputEncoding">The encoding for the standard output stream.</param>
    /// <param name="standardErrorEncoding">The encoding for the standard error stream.</param>
    /// <param name="processResourcePolicy">The processor resource policy for the command.</param>
    /// <param name="useShellExecution">Indicates whether to use the shell to execute the command.</param>
    /// <param name="windowCreation">Indicates whether to create a new window for the command.</param>
    /// <param name="redirectStandardInput">Whether to redirect standard input to the process.</param>
    /// <param name="argumentList">
    ///     An optional verbatim argument list emitted via <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList"/>.
    ///     Used by the middleware to deliver the PowerShell wrapper without OS re-tokenization.
    /// </param>
    [SetsRequiredMembers]
    public PowershellProcessConfiguration(
        string arguments,
        bool redirectStandardInput, bool outputRedirection = true,
        string? workingDirectoryPath = null, bool requiresAdministrator = false,
        Dictionary<string, string>? environmentVariables = null, UserCredential? credentials = null,
        StreamWriter? standardInput = null,
        Encoding? standardInputEncoding = null, Encoding? standardOutputEncoding = null,
        Encoding? standardErrorEncoding = null, ProcessResourcePolicy? processResourcePolicy = null,
        bool useShellExecution = false, bool windowCreation = false,
        IEnumerable<string>? argumentList = null) :
        base(OperatingSystem.IsWindows() ? "pwsh.exe" : "pwsh", arguments, outputRedirection)
    {
        RedirectStandardInput = redirectStandardInput;
        RequiresAdministrator = requiresAdministrator;
        Credential = credentials ?? UserCredential.Null;

        if (standardInput is not null)
            StandardInput = standardInput;

        StandardInputEncoding = standardInputEncoding ?? Encoding.Default;
        StandardOutputEncoding = standardOutputEncoding ?? Encoding.Default;
        StandardErrorEncoding = standardErrorEncoding ?? Encoding.Default;
        ResourcePolicy = processResourcePolicy ?? ProcessResourcePolicy.Default;
        UseShellExecution = useShellExecution;
        WindowCreation = windowCreation;

        if (workingDirectoryPath is not null)
            WorkingDirectoryPath = workingDirectoryPath;

        if (environmentVariables is not null)
            EnvironmentVariables = environmentVariables;

        if (argumentList is not null)
            ArgumentList = argumentList.ToArray();
    }
}