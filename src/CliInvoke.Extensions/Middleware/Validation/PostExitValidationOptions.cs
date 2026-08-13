/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Text.RegularExpressions;

namespace CliInvoke.Extensions.Middleware.Validation;

/// <summary>
///     Options that describe a post-exit validation rule applied to a <see cref="ProcessResult"/>.
/// </summary>
public sealed class PostExitValidationOptions
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PostExitValidationOptions"/> class.
    /// </summary>
    /// <param name="rule">
    ///     A function that receives the <see cref="ProcessResult"/> and returns <c>null</c> when
    ///     the rule passes or an error message when the rule fails.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rule"/> is <c>null</c>.</exception>
    public PostExitValidationOptions(Func<ProcessResult, string?> rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        Rule = rule;
    }

    /// <summary>
    ///     Gets the validation rule applied to the process result.
    /// </summary>
    public Func<ProcessResult, string?> Rule { get; }

    /// <summary>
    ///     Creates an options instance that validates the process exited with a zero exit code.
    /// </summary>
    /// <returns>A <see cref="PostExitValidationOptions"/> instance that enforces a zero exit code.</returns>
    public static PostExitValidationOptions ExitCodeIsZero()
    {
        return new PostExitValidationOptions(result =>
        {
            if (result.ExitCode == 0)
                return null;

            return $"Process exited with code {result.ExitCode}.";
        });
    }

    /// <summary>
    ///     Creates an options instance that validates the standard output matches the supplied
    ///     regular expression.
    /// </summary>
    /// <param name="regex">
    ///     The regular expression pattern to evaluate against <see cref="BufferedProcessResult.StandardOutput"/>.
    /// </param>
    /// <returns>A <see cref="PostExitValidationOptions"/> instance that enforces a stdout regex match.</returns>
    public static PostExitValidationOptions StdoutMatches(string regex)
    {
        ArgumentException.ThrowIfNullOrEmpty(regex);

        Regex compiled = new Regex(regex, RegexOptions.Compiled, TimeSpan.FromSeconds(2));

        return new PostExitValidationOptions(result =>
        {
            if (result is not BufferedProcessResult bufferedResult)
                return "Result does not expose standard output text.";

            if (compiled.IsMatch(bufferedResult.StandardOutput))
                return null;

            return "Standard output does not match the expected pattern.";
        });
    }

    /// <summary>
    ///     Creates an options instance that validates the standard error output is empty or
    ///     contains only whitespace.
    /// </summary>
    /// <returns>A <see cref="PostExitValidationOptions"/> instance that enforces empty standard error.</returns>
    public static PostExitValidationOptions StderrIsEmpty()
    {
        return new PostExitValidationOptions(result =>
        {
            if (result is not BufferedProcessResult bufferedResult)
                return null;

            if (string.IsNullOrWhiteSpace(bufferedResult.StandardError))
                return null;

            return "Standard error is not empty.";
        });
    }
}
