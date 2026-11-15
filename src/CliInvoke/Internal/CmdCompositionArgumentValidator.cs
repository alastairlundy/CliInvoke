/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace CliInvoke.Internal;

/// <summary>
///     Validates argument tokens that cmd.exe will parse itself, rather than leaving to
///     the operating system's argument-splitting rules.
/// </summary>
/// <remarks>
///     When cmd.exe (or a batch file) receives a command line, it scans the text for its
///     own control characters before any argument splitting happens. Characters with
///     meaning at that layer are re-interpreted by cmd.exe even when they arrive
///     inside a discrete argument token, so quoting at the launch layer cannot
///     keep them literal. Those characters are separators, redirection, the
///     escape character, and expansion syntax.
///     Values containing them are therefore rejected rather than escaped, and callers
///     who own their quoting can compose a raw command line through
///     <c>CmdProcessConfiguration</c> instead.
/// </remarks>
internal static class CmdCompositionArgumentValidator
{
    /// <summary>
    ///     The characters cmd.exe interprets itself when parsing a command line:
    ///     <c>&amp;</c> and <c>|</c> separate commands, <c>&lt;</c> and <c>&gt;</c> redirect,
    ///     <c>^</c> escapes the next character at each parse layer, <c>%</c> expands
    ///     environment variables, <c>"</c> toggles cmd.exe's own quote handling, and
    ///     <c>!</c> expands delayed-expansion variables when enabled.
    /// </summary>
    private static readonly char[] Metacharacters = ['&', '|', '<', '>', '^', '%', '"', '!'];

    /// <summary>
    ///     Determines whether the given target file path refers to cmd.exe.
    /// </summary>
    /// <param name="targetFilePath">The target file path to check.</param>
    /// <returns>True when the path names cmd.exe; otherwise false.</returns>
    /// <remarks>
    ///     <para>
    ///         Runners are identified by file name. Invoke shell runners by their canonical
    ///         names (<c>cmd.exe</c>, <c>pwsh</c>/<c>powershell</c>). A renamed shell binary is
    ///         treated as an ordinary target and receives tokenized delivery; shell-specific
    ///         composition and its validation do not apply.
    ///     </para>
    /// </remarks>
    internal static bool IsCmdRunner(string targetFilePath)
    {
        string fileName = Path.GetFileName(targetFilePath.Trim());

        return fileName.Equals("cmd", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("cmd.exe", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Determines whether the given target file path refers to cmd.exe or a batch file,
    ///     both of which parse their arguments with cmd.exe's own rules.
    /// </summary>
    /// <param name="targetFilePath">The target file path to check.</param>
    /// <returns>True when the path names cmd.exe or a .bat/.cmd file; otherwise false.</returns>
    internal static bool IsCmdOrBatchTarget(string targetFilePath)
    {
        if (IsCmdRunner(targetFilePath))
            return true;

        string extension = Path.GetExtension(targetFilePath.Trim());

        return extension.Equals(".bat", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Throws an <see cref="ArgumentException" /> if any of the given tokens contains
    ///     a character that cmd.exe interprets itself when parsing a command line.
    /// </summary>
    /// <param name="tokens">The argument tokens to validate.</param>
    /// <param name="paramName">The name of the caller's argument the tokens came from.</param>
    /// <exception cref="ArgumentException">A token contains a character cmd.exe parses itself.</exception>
    internal static void Validate(IEnumerable<string> tokens, string paramName)
    {
        foreach (string token in tokens)
        {
            int metacharacterIndex = token.IndexOfAny(Metacharacters);

            if (metacharacterIndex >= 0)
            {
                throw new ArgumentException(
                    Resources.Exceptions_Arguments_CmdMetacharacter
                        .Replace("{x}", token)
                        .Replace("{y}", token[metacharacterIndex].ToString()),
                    paramName);
            }
        }
    }
}
