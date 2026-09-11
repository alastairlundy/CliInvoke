/*
    CliInvoke Specializations
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Internal;

namespace CliInvoke.Specializations.Middleware;

/// <summary>
/// 
/// </summary>
[UnsupportedOSPlatform("browser")]
[UnsupportedOSPlatform("ios")]
[UnsupportedOSPlatform("tvos")]
[UnsupportedOSPlatform("watchos")]
internal sealed class DefaultShellMiddleware : IProcessMiddleware
{
    private readonly IShellDetector _shellDetector;
    private readonly ShellMiddlewareOptions _options;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="shellDetector"></param>
    /// <param name="options"></param>
    public DefaultShellMiddleware(IShellDetector shellDetector, ShellMiddlewareOptions? options = null)
    {
        _shellDetector = shellDetector;
        _options = options ?? ShellMiddlewareOptions.Default;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task InvokeAsync(InvocationContext context, Func<InvocationContext, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        ThrowIfUnsupported();
        
        ShellInformation shell = await _shellDetector.ResolveDefaultShellAsync(context.CancellationToken).ConfigureAwait(false);
        
        string originalPath = context.Configuration.TargetFilePath;
        string originalArgs = context.Configuration.Arguments;

        string shellName = Path.GetFileNameWithoutExtension(shell.TargetFilePath.Name);

        bool isCmd = shellName.Equals("cmd", StringComparison.OrdinalIgnoreCase);
        bool isPowerShell =
            shellName.Equals("pwsh", StringComparison.OrdinalIgnoreCase) ||
            shellName.Equals("powershell", StringComparison.OrdinalIgnoreCase);

        // Escape both the target and the arguments so they are passed to the
        // wrapped command as literal data. Without this, shell metacharacters in
        // the arguments (e.g. ';', '|', '&', '$(...)') would be re-interpreted by
        // the shell as additional commands — a command-injection risk.
        string safePath;
        string safeArgs;

        if (isCmd)
        {
            safePath = ShellArgumentEscaper.EscapeForCmd(originalPath);
            safeArgs = ShellArgumentEscaper.EscapeForCmd(originalArgs);
        }
        else if (isPowerShell)
        {
            safePath = ShellArgumentEscaper.EscapeForPowerShell(originalPath);
            safeArgs = ShellArgumentEscaper.EscapeForPowerShell(originalArgs);
        }
        else
        {
            safePath = ShellArgumentEscaper.EscapeForPosixShell(originalPath);
            safeArgs = ShellArgumentEscaper.EscapeForPosixShell(originalArgs);
        }

        string innerCommand;
        IReadOnlyList<string> argumentList;

        if (isPowerShell)
        {
            // PowerShell uses the & call operator to invoke the command.
            innerCommand = string.IsNullOrWhiteSpace(safeArgs)
                ? $"& \"{safePath}\""
                : $"& \"{safePath}\" {safeArgs}";

            // Emit the wrapper as a verbatim ArgumentList so the OS command-line parser does NOT
            // re-tokenise it before PowerShell parses it. A single re-tokenised Arguments string
            // would let a '"' in the value break the OS-level quoting and let PowerShell reassemble
            // a second command (command-injection). ArgumentList is passed through unchanged.
            argumentList = [
                "-NoProfile",
                "-NonInteractive",
                "-Command",
                innerCommand,
            ];
        }
        else if (isCmd)
        {
            innerCommand = string.IsNullOrWhiteSpace(safeArgs)
                ? $"\"{safePath}\""
                : $"\"{safePath}\" {safeArgs}";

            argumentList = [
                "/c",
                innerCommand,
            ];
        }
        else
        {
            // POSIX shells use -c to execute a command string.
            innerCommand = string.IsNullOrWhiteSpace(safeArgs)
                ? $"\"{safePath}\""
                : $"\"{safePath}\" {safeArgs}";

            argumentList = [
                "-c",
                innerCommand,
            ];
        }

        ProcessConfiguration src = context.Configuration;
        ProcessConfiguration newConfig = new()
        {
            TargetFilePath = shell.TargetFilePath.FullName,
            RedirectStandardInput = src.RedirectStandardInput,
            OutputRedirection = context.Mode != InvocationMode.Raw,
            WorkingDirectoryPath = src.WorkingDirectoryPath,
            Credential = src.Credential,
            StandardErrorEncoding =  src.StandardErrorEncoding,
            StandardInputEncoding =  src.StandardInputEncoding,
            StandardOutputEncoding =  src.StandardOutputEncoding,
            StandardInput = src.StandardInput,
            ResourcePolicy = src.ResourcePolicy,
            EnvironmentVariables = new Dictionary<string, string>(src.EnvironmentVariables),
            RequiresAdministrator = src.RequiresAdministrator,
            WindowCreation = _options.WindowCreation,
            UseShellExecution =  _options.UseShellExecution,
            ArgumentList = argumentList
        };

        InvocationContext newContext = context.WithConfiguration(newConfig);

        await next(newContext).ConfigureAwait(false);

        // The terminal ran against the rewritten context, so propagate its result back to the
        // original chain context that the caller reads from.
        context.Result = newContext.Result;   
    }
    
    private static void ThrowIfUnsupported()
    {
        if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() ||
            OperatingSystem.IsBrowser() || OperatingSystem.IsWatchOS())
        {
            throw new PlatformNotSupportedException(Resources
                .Exceptions_Powershell_OnlySupportedOnDesktop);
        }
    }
}