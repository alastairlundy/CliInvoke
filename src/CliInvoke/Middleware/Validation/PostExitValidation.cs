/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Core.Validation;
using CliInvoke.Validation;

namespace CliInvoke.Extensions.Middleware.Validation;

/// <summary>
///     Factory helpers that build <see cref="IProcessResultValidator{ProcessResult}"/> instances
///     from CliInvoke's shared validation rules, for use with post-exit validation middleware.
/// </summary>
public static class PostExitValidation
{
    /// <summary>
    ///     Creates a validator that ensures the process exited with a zero exit code.
    /// </summary>
    /// <returns>A validator enforcing a zero exit code.</returns>
    public static IProcessResultValidator<ProcessResult> ExitCodeIsZero()
    {
        return new ProcessResultValidator<ProcessResult>(
            [CommonValidationRules<ProcessResult>.ExitCodeZeroRule()]);
    }

    /// <summary>
    ///     Creates a validator that ensures the process exited with the specified exit code.
    /// </summary>
    /// <param name="exitCode">The expected exit code.</param>
    /// <returns>A validator enforcing the expected exit code.</returns>
    public static IProcessResultValidator<ProcessResult> ExitCodeIs(int exitCode)
    {
        return new ProcessResultValidator<ProcessResult>(
        [
            new ValidationRule<ProcessResult>(
                CommonValidationRules<ProcessResult>.RequiresExitCode(exitCode),
                nameof(CommonValidationRules<ProcessResult>.RequiresExitCode),
                $"The process did not exit with code {exitCode}.")
        ]);
    }

    /// <summary>
    ///     Creates a validator that ensures the process exited with one of the allowed exit codes.
    /// </summary>
    /// <param name="exitCodes">The set of permitted exit codes.</param>
    /// <returns>A validator enforcing one of the allowed exit codes.</returns>
    public static IProcessResultValidator<ProcessResult> ExitCodeIsOneOf(params int[] exitCodes)
    {
        ArgumentNullException.ThrowIfNull(exitCodes);

        return new ProcessResultValidator<ProcessResult>(
        [
            new ValidationRule<ProcessResult>(
                CommonValidationRules<ProcessResult>.RequiresAllowedExitCode(exitCodes),
                nameof(CommonValidationRules<ProcessResult>.RequiresAllowedExitCode),
                $"The process did not exit with one of the allowed codes [{string.Join(", ", exitCodes)}].")
        ]);
    }

    /// <summary>
    ///     Creates a validator that ensures the buffered process result's standard output matches the
    ///     supplied regular expression.
    /// </summary>
    /// <param name="regex">
    ///     The regular expression pattern to evaluate against
    ///     <see cref="CliInvoke.Core.BufferedProcessResult.StandardOutput"/>.
    /// </param>
    /// <returns>A validator enforcing a standard output match.</returns>
    public static IProcessResultValidator<ProcessResult> StdoutMatches(string regex)
    {
        ArgumentException.ThrowIfNullOrEmpty(regex);

        ValidationRule<BufferedProcessResult> rule =
            CommonValidationRules<BufferedProcessResult>.StandardOutputMatchesRule(regex);

        return new ProcessResultValidator<ProcessResult>(
            [ToProcessResultRule(rule)]);
    }

    /// <summary>
    ///     Creates a validator that ensures the buffered process result's standard error is empty or
    ///     whitespace only.
    /// </summary>
    /// <returns>A validator enforcing empty standard error.</returns>
    public static IProcessResultValidator<ProcessResult> StderrIsEmpty()
    {
        ValidationRule<BufferedProcessResult> rule =
            CommonValidationRules<BufferedProcessResult>.StandardErrorIsEmptyRule();

        return new ProcessResultValidator<ProcessResult>(
            [ToProcessResultRule(rule)]);
    }

    /// <summary>
    ///     Adapts a <see cref="ValidationRule{BufferedProcessResult}"/> into a
    ///     <see cref="ValidationRule{ProcessResult}"/> so it can validate any process result, failing
    ///     non-buffered results as the inner rule dictates.
    /// </summary>
    private static ValidationRule<ProcessResult> ToProcessResultRule(ValidationRule<BufferedProcessResult> rule)
    {
        return new ValidationRule<ProcessResult>(
            result => result is BufferedProcessResult buffered && rule.Predicate(buffered),
            rule.Name,
            rule.FailureMessage,
            failureMessageFactory: null);
    }
}
