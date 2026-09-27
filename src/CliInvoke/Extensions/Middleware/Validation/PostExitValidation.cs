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
    ///     supplied regular expression. Results that do not expose buffered standard output text
    ///     (non-buffered or <c>null</c> results) fail the rule, as dictated by the inner rule.
    /// </summary>
    /// <param name="regex">
    ///     The regular expression pattern to evaluate against
    ///     <see cref="BufferedProcessResult.StandardOutput"/>.
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
    ///     whitespace only. Results that do not expose buffered standard error text (non-buffered or
    ///     <c>null</c> results) pass the rule, as dictated by the inner rule.
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
    ///     <see cref="ValidationRule{ProcessResult}"/> so it can validate any process result.
    ///     The inner rule stays the single source of truth for results that do not expose buffered
    ///     text: when the supplied result is not a <see cref="BufferedProcessResult"/> (including a
    ///     <c>null</c> result), the inner predicate is evaluated against a <c>null</c> buffered
    ///     result, exactly what the inner rules treat "a result without buffered text" as. Rules
    ///     that pass such results (e.g. standard-error-empty) therefore pass here, and rules that
    ///     require buffered text (e.g. standard-output-match) fail here.
    /// </summary>
    private static ValidationRule<ProcessResult> ToProcessResultRule(ValidationRule<BufferedProcessResult> rule)
    {
        return new ValidationRule<ProcessResult>(
            result => rule.Predicate((result as BufferedProcessResult)!),
            rule.Name,
            rule.FailureMessage,
            failureMessageFactory: null);
    }
}
