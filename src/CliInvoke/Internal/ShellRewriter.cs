/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

#pragma warning disable CA1416

using CliInvoke.Core.Internal;

namespace CliInvoke.Internal;

/// <summary>
///     Owns escaping-by-shell-kind, inner-command composition, and configuration
///     rewriting for shell-wrapped process invocations.
/// </summary>
/// <remarks>
///     The <c>RunnerConfigurationFactory</c> and shell middleware delegate their
///     escape-plus-compose logic here so that the duplicated wiring collapses onto
///     one testable module. The non-shell runner path must not enter this type.
/// </remarks>
internal static class ShellRewriter
{
    /// <summary>
    ///     Rewrites a <see cref="ProcessConfiguration"/> by escaping and composing the
    ///     inner command for the specified shell kind and delivery method.
    /// </summary>
    /// <param name="source">The source configuration whose properties are forwarded.</param>
    /// <param name="shellTargetPath">
    ///     The executable path of the target process (may differ from
    ///     <paramref name="source"/>.<see cref="ProcessConfiguration.TargetFilePath"/>
    ///     when the target has already been resolved).
    /// </param>
    /// <param name="runnerArgs">
    ///     Optional runner-level arguments to prepend to the composed inner command.
    ///     For <see cref="ShellDelivery.ArgumentList"/> delivery these are appended as
    ///     discrete tokens before the script entry; for
    ///     <see cref="ShellDelivery.Arguments"/> delivery the raw string is prepended.
    /// </param>
    /// <param name="kind">The target shell for escaping and composition.</param>
    /// <param name="delivery">How the composed command is delivered to the target process.</param>
    /// <param name="windowCreation">Whether to enable window creation for the spawned process.</param>
    /// <param name="useShellExecution">Whether to use shell execution for the spawned process.</param>
    /// <returns>
    ///     A plain <see cref="ProcessConfiguration"/> whose
    ///     <see cref="ProcessConfiguration.TargetFilePath"/> is the shell executable,
    ///     and whose arguments embody the escaped inner command for the requested delivery.
    /// </returns>
    [Pure]
    internal static ProcessConfiguration Rewrite(
        ProcessConfiguration source,
        string shellTargetPath,
        string runnerArgs,
        ShellKind kind,
        ShellDelivery delivery,
        bool windowCreation,
        bool useShellExecution)
    {
        switch (kind)
        {
            case ShellKind.PowerShell:
            {
                string safePath = ShellArgumentEscaper.EscapeForPowerShell(
                    source.TargetFilePath);
                string safeArgs = ShellArgumentEscaper.EscapeForPowerShell(
                    source.Arguments);
                string script = string.IsNullOrWhiteSpace(safeArgs)
                    ? $"& \"{safePath}\""
                    : $"& \"{safePath}\" {safeArgs}";

                IReadOnlyList<string> runnerArgList =
                    !string.IsNullOrWhiteSpace(runnerArgs)
                        ? ArgumentTokenizer.Tokenize(runnerArgs)
                        : Array.Empty<string>();

                List<string> argumentList = new(runnerArgList.Count + 4);
                argumentList.AddRange(runnerArgList);
                argumentList.Add("-NoProfile");
                argumentList.Add("-NonInteractive");
                argumentList.Add("-Command");
                argumentList.Add(script);

                return new ProcessConfiguration
                {
                    TargetFilePath = shellTargetPath,
                    Arguments = string.Empty,
                    ArgumentList = argumentList,
                    RequiresAdministrator = source.RequiresAdministrator,
                    WorkingDirectoryPath = source.WorkingDirectoryPath,
                    WindowCreation = windowCreation,
                    EnvironmentVariables = source.EnvironmentVariables,
                    Credential = source.Credential,
                    UseShellExecution = useShellExecution,
                    StandardInput = source.StandardInput,
                    RedirectStandardInput = source.RedirectStandardInput,
                    OutputRedirection = source.OutputRedirection,
                    ResourcePolicy = source.ResourcePolicy,
                    StandardInputEncoding = source.StandardInputEncoding,
                    StandardOutputEncoding = source.StandardOutputEncoding,
                    StandardErrorEncoding = source.StandardErrorEncoding
                };
            }

            case ShellKind.Cmd:
            {
                string safePath = ShellArgumentEscaper.EscapeForCmd(
                    source.TargetFilePath);
                string safeArgs = ShellArgumentEscaper.EscapeForCmd(
                    source.Arguments);
                string innerCommand = string.IsNullOrWhiteSpace(safeArgs)
                    ? $"\"{safePath}\""
                    : $"\"{safePath}\" {safeArgs}";

                string arguments = string.IsNullOrWhiteSpace(runnerArgs)
                    ? innerCommand
                    : $"{runnerArgs} {innerCommand}";

                return new ProcessConfiguration
                {
                    TargetFilePath = shellTargetPath,
                    Arguments = arguments,
                    RequiresAdministrator = source.RequiresAdministrator,
                    WorkingDirectoryPath = source.WorkingDirectoryPath,
                    WindowCreation = windowCreation,
                    EnvironmentVariables = source.EnvironmentVariables,
                    Credential = source.Credential,
                    UseShellExecution = useShellExecution,
                    StandardInput = source.StandardInput,
                    RedirectStandardInput = source.RedirectStandardInput,
                    OutputRedirection = source.OutputRedirection,
                    ResourcePolicy = source.ResourcePolicy,
                    StandardInputEncoding = source.StandardInputEncoding,
                    StandardOutputEncoding = source.StandardOutputEncoding,
                    StandardErrorEncoding = source.StandardErrorEncoding
                };
            }

            case ShellKind.Posix:
            {
                string safePath = ShellArgumentEscaper.EscapeForPosixShell(
                    source.TargetFilePath);
                string safeArgs = ShellArgumentEscaper.EscapeForPosixShell(
                    source.Arguments);
                string script = string.IsNullOrWhiteSpace(safeArgs)
                    ? $"& \"{safePath}\""
                    : $"& \"{safePath}\" {safeArgs}";

                IReadOnlyList<string> runnerArgList =
                    !string.IsNullOrWhiteSpace(runnerArgs)
                        ? ArgumentTokenizer.Tokenize(runnerArgs)
                        : Array.Empty<string>();

                List<string> argumentList = new(runnerArgList.Count + 1);
                argumentList.AddRange(runnerArgList);
                argumentList.Add(script);

                return new ProcessConfiguration
                {
                    TargetFilePath = shellTargetPath,
                    Arguments = string.Empty,
                    ArgumentList = argumentList,
                    RequiresAdministrator = source.RequiresAdministrator,
                    WorkingDirectoryPath = source.WorkingDirectoryPath,
                    WindowCreation = windowCreation,
                    EnvironmentVariables = source.EnvironmentVariables,
                    Credential = source.Credential,
                    UseShellExecution = useShellExecution,
                    StandardInput = source.StandardInput,
                    RedirectStandardInput = source.RedirectStandardInput,
                    OutputRedirection = source.OutputRedirection,
                    ResourcePolicy = source.ResourcePolicy,
                    StandardInputEncoding = source.StandardInputEncoding,
                    StandardOutputEncoding = source.StandardOutputEncoding,
                    StandardErrorEncoding = source.StandardErrorEncoding
                };
            }

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(kind), kind, "Unsupported shell kind.");
        }
    }
}

#pragma warning restore CA1416
