/*
    CliInvoke.Core

    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Linq;
using System.Text.RegularExpressions;

namespace CliInvoke.Core.Validation;

/// <summary>
///     Provides a set of predefined common validation rules for processing Process results.
///     These rules validate instances of <typeparamref name="TProcessResult" />,
///     which must inherit from <see cref="ProcessResult" />.
/// </summary>
/// <typeparam name="TProcessResult">
///     The type of the process result being validated, constrained to derive from
///     <see cref="ProcessResult" />.
/// </typeparam>
public static class CommonValidationRules<TProcessResult>
    where TProcessResult : ProcessResult
{
    /// <summary>
    ///     A predefined validation rule that ensures the process result's exit code is zero.
    /// </summary>
    /// <typeparam name="TProcessResult">
    ///     The type of the process result being validated, constrained to derive from
    ///     <see cref="ProcessResult" />.
    /// </typeparam>
    /// <returns>
    ///     A function that evaluates to true if the exit code of the process result equals zero;
    ///     otherwise, false.
    /// </returns>
    public static Func<TProcessResult, bool> RequiresExitCodeZero
        => RequiresExitCode(0);

    /// <summary>
    ///     A validation rule that always returns true, effectively bypassing any validation logic.
    /// </summary>
    /// <returns>
    ///     A function that takes an instance of <typeparamref name="TProcessResult" /> as input and always
    ///     evaluates to true.
    /// </returns>
    public static Func<TProcessResult, bool> NoValidation()
    {
        return _ => true;
    }

    /// <summary>
    ///     A validation rule that checks whether the process result's exit code matches the specified
    ///     value.
    /// </summary>
    /// <param name="exitCode">
    ///     The expected exit code that the process result must match for the validation to succeed.
    /// </param>
    /// <returns>
    ///     A function that takes an instance of <typeparamref name="TProcessResult" /> as input and
    ///     evaluates to true if the exit code matches; otherwise, false.
    /// </returns>
    public static Func<TProcessResult, bool> RequiresExitCode(int exitCode)
    {
        return result => result.ExitCode == exitCode;
    }

    /// <summary>
    ///     A validation rule that checks whether the process result's exit code is one of the specified
    ///     allowed exit codes.
    /// </summary>
    /// <param name="exitCodes">
    ///     The collection of allowed exit codes that the process result can match for the validation to
    ///     succeed.
    /// </param>
    /// <returns>
    ///     A function that takes an instance of <typeparamref name="TProcessResult" /> as input and
    ///     evaluates to true if the exit code matches any in the provided collection; otherwise, false.
    /// </returns>
    public static Func<TProcessResult, bool> RequiresAllowedExitCode(params int[] exitCodes)
    {
        return result => exitCodes.Any(code => result.ExitCode == code);
    }

    /// <summary>
    ///     A validation rule that checks whether the process result's standard output matches the
    ///     supplied regular expression. Only meaningful when the result is a
    ///     <see cref="BufferedProcessResult" />; results that do not expose standard output text fail the rule.
    /// </summary>
    /// <param name="regex">
    ///     The regular expression pattern to evaluate against
    ///     <see cref="BufferedProcessResult.StandardOutput" />.
    /// </param>
    /// <returns>
    ///     A function that takes an instance of <see cref="BufferedProcessResult" /> as input and evaluates to
    ///     true if the standard output matches the pattern; otherwise, false.
    /// </returns>
    public static Func<BufferedProcessResult, bool> RequiresStandardOutputMatches(string regex)
    {
        ArgumentException.ThrowIfNullOrEmpty(regex);

        Regex compiled = new Regex(regex, RegexOptions.Compiled, TimeSpan.FromSeconds(2));

        return result => result is BufferedProcessResult buffered
                         && compiled.IsMatch(buffered.StandardOutput);
    }

    /// <summary>
    ///     A validation rule that checks whether the process result's standard error output is empty or
    ///     contains only whitespace. Only meaningful when the result is a
    ///     <see cref="BufferedProcessResult" />; results that do not expose standard error text pass the rule.
    /// </summary>
    /// <returns>
    ///     A function that takes an instance of <see cref="BufferedProcessResult" /> as input and evaluates to
    ///     true if the standard error is empty; otherwise, false.
    /// </returns>
    public static Func<BufferedProcessResult, bool> RequiresStandardErrorIsEmpty()
    {
        return result => result is not BufferedProcessResult buffered
                         || string.IsNullOrWhiteSpace(buffered.StandardError);
    }

    /// <summary>
    ///     A self-describing validation rule that ensures the process exited with a zero exit code.
    /// </summary>
    /// <returns>A <see cref="ValidationRule{TProcessResult}" /> enforcing a zero exit code.</returns>
    public static ValidationRule<TProcessResult> ExitCodeZeroRule()
        => new(
            RequiresExitCodeZero,
            nameof(RequiresExitCodeZero),
            "The process did not exit with code 0.");

    /// <summary>
    ///     A self-describing validation rule that ensures the process result's standard output matches the
    ///     supplied regular expression.
    /// </summary>
    /// <param name="regex">
    ///     The regular expression pattern to evaluate against
    ///     <see cref="BufferedProcessResult.StandardOutput" />.
    /// </param>
    /// <returns>A <see cref="ValidationRule{TProcessResult}" /> enforcing a standard output match.</returns>
    public static ValidationRule<BufferedProcessResult> StandardOutputMatchesRule(string regex)
    {
        ArgumentException.ThrowIfNullOrEmpty(regex);

        return new ValidationRule<BufferedProcessResult>(
            RequiresStandardOutputMatches(regex),
            nameof(RequiresStandardOutputMatches),
            $"Standard output did not match the expected pattern '{regex}'.");
    }

    /// <summary>
    ///     A self-describing validation rule that ensures the process result's standard error is empty.
    /// </summary>
    /// <returns>A <see cref="ValidationRule{TProcessResult}" /> enforcing empty standard error.</returns>
    public static ValidationRule<BufferedProcessResult> StandardErrorIsEmptyRule()
        => new(
            RequiresStandardErrorIsEmpty(),
            nameof(RequiresStandardErrorIsEmpty),
            "Standard error was not empty.");
}