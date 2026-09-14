/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

#pragma warning disable CA1416

using System.Text;

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
    ///     Escapes a value so it is treated as literal data inside a PowerShell
    ///     <c>-Command</c> string. Prevents command chaining (<c>;</c>, <c>|</c>,
    ///     <c>&amp;</c>), subexpression / variable expansion (<c>$(...)</c>, <c>$var</c>),
    ///     grouping, redirection, and quoting escapes.
    /// </summary>
    /// <param name="value">The raw argument value to escape.</param>
    /// <returns>The escaped value, safe to embed in a PowerShell command.</returns>
    internal static string EscapeForPowerShell(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        StringBuilder builder = new(value.Length + 16);

        foreach (char c in value)
        {
            switch (c)
            {
                // PowerShell escape character is the backtick.
                case '`':
                case '$':
                case ';':
                case '|':
                case '&':
                case '(':
                case ')':
                case '{':
                case '}':
                case '<':
                case '>':
                case '"':
                case '\'':
                    builder.Append('`').Append(c);
                    break;
                case '\n':
                    builder.Append('`').Append('n');
                    break;
                case '\r':
                    // Drop bare carriage returns; they would otherwise terminate the line.
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    ///     Escapes a value so it is treated as literal data inside a POSIX shell
    ///     <c>-c</c> string. Prevents variable expansion (<c>$var</c>),
    ///     command substitution (<c>`...`</c>), globbing (<c>*</c>, <c>?</c>,
    ///     <c>[...]</c>), redirection (<c>&lt;</c>, <c>&gt;</c>),
    ///     command chaining (<c>;</c>, <c>&amp;</c>, <c>|</c>), and quoting breaks.
    /// </summary>
    /// <param name="value">The raw argument value to escape.</param>
    /// <returns>The escaped value, safe to embed in a POSIX shell command.</returns>
    internal static string EscapeForPosixShell(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        StringBuilder builder = new(value.Length + 16);

        foreach (char c in value)
        {
            switch (c)
            {
                // POSIX shell escape character is the backslash.
                case '\\':
                case '$':
                case '`':
                case '"':
                case '\'':
                case '!':
                case '&':
                case '|':
                case ';':
                case '(':
                case ')':
                case '<':
                case '>':
                case '{':
                case '}':
                case '[':
                case ']':
                case '~':
                case '#':
                case '*':
                case '?':
                    builder.Append('\\').Append(c);
                    break;
                case '\n':
                    // A bare newline would terminate the shell command; drop it.
                    break;
                case '\r':
                    // Drop bare carriage returns.
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    ///     Escapes a value so it is treated as literal data on the
    ///     <c>cmd.exe /c</c> command line. Prevents command chaining
    ///     (<c>&amp;</c>, <c>|</c>), redirection (<c>&lt;</c>, <c>&gt;</c>),
    ///     environment-variable expansion (<c>%VAR%</c>), and quoting breaks.
    /// </summary>
    /// <param name="value">The raw argument value to escape.</param>
    /// <returns>The escaped value, safe to embed in a cmd command.</returns>
    internal static string EscapeForCmd(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        StringBuilder builder = new(value.Length + 16);

        foreach (char c in value)
        {
            switch (c)
            {
                // cmd.exe escape character is the caret.
                case '^':
                case '&':
                case '|':
                case '<':
                case '>':
                case '%':
                    builder.Append('^').Append(c);
                    break;
                case '"':
                    // A literal quote inside a quoted cmd token is written as "".
                    builder.Append('"').Append('"');
                    break;
                case '\n':
                case '\r':
                    // A bare newline would terminate the cmd command; drop it.
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        return builder.ToString();
    }

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
    ///     Optional runner-level arguments, including shell switches owned by the caller
    ///     (e.g. <c>-NoProfile -NonInteractive -Command</c>, <c>/c</c>, or <c>-c</c>).
    ///     For the <see cref="ShellKind.PowerShell"/> and <see cref="ShellKind.Posix"/>
    ///     kinds these are tokenized and added as discrete argument-list entries before
    ///     the script entry; for the <see cref="ShellKind.Cmd"/> kind the raw string is
    ///     prepended to the composed command.
    /// </param>
    /// <param name="kind">The target shell for escaping and composition.</param>
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
        bool windowCreation,
        bool useShellExecution)
    {
        switch (kind)
        {
            case ShellKind.PowerShell:
            {
                string safePath = EscapeForPowerShell(
                    source.TargetFilePath);
                string safeArgs = EscapeForPowerShell(
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

            case ShellKind.Cmd:
            {
                string safePath = EscapeForCmd(
                    source.TargetFilePath);
                string safeArgs = EscapeForCmd(
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
                string safePath = EscapeForPosixShell(
                    source.TargetFilePath);
                string safeArgs = EscapeForPosixShell(
                    source.Arguments);
                string script = string.IsNullOrWhiteSpace(safeArgs)
                    ? $"\"{safePath}\""
                    : $"\"{safePath}\" {safeArgs}";

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
