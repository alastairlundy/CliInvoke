/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace CliInvoke.Core.Validation;

/// <summary>
///     Describes a single validation rule that failed for a given process result.
/// </summary>
/// <typeparam name="TProcessResult">
///     The type of the process result being validated, constrained to derive from
///     <see cref="ProcessResult" />.
/// </typeparam>
public sealed class ValidationFailure<TProcessResult>
    where TProcessResult : ProcessResult
{
    /// <summary>
    ///     Initialises a new instance of the <see cref="ValidationFailure{TProcessResult}" /> class.
    /// </summary>
    /// <param name="rule">The rule that failed.</param>
    /// <param name="result">The process result that failed the rule.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="rule" /> or <paramref name="result" /> is <c>null</c>.
    /// </exception>
    public ValidationFailure(ValidationRule<TProcessResult> rule, TProcessResult result)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(result);

        Rule = rule;
        Result = result;
    }

    /// <summary>
    ///     Gets the rule that failed.
    /// </summary>
    public ValidationRule<TProcessResult> Rule { get; }

    /// <summary>
    ///     Gets the process result that failed the rule.
    /// </summary>
    public TProcessResult Result { get; }

    /// <summary>
    ///     Gets the failure message for this failure, derived from <see cref="Rule" />.
    /// </summary>
    public string FailureMessage => Rule.GetFailureMessage(Result);
}
