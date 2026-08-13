/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Linq;

using CliInvoke.Core.Validation;

namespace CliInvoke.Validation;

/// <summary>
///     Represents a validator for process results. It provides functionality to validate a given
///     process result against a set of specified rules.
/// </summary>
/// <typeparam name="TProcessResult">
///     The type of the process result being validated.
///     Must be a class that inherits from the <see cref="CliInvoke.Core.ProcessResult" /> class.
/// </typeparam>
public class ProcessResultValidator<TProcessResult> : IProcessResultValidator<TProcessResult>
    where TProcessResult : ProcessResult
{
    /// <summary>
    ///     Initialises a new instance of the <see cref="ProcessResultValidator{TProcessResult}" /> class
    ///     from a set of predicate rules, each carrying no name or failure message.
    /// </summary>
    /// <param name="rules">The predicate rules to evaluate against the process result.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rules" /> is <c>null</c>.</exception>
    public ProcessResultValidator(Func<TProcessResult, bool>[] rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        ValidationRule<TProcessResult>[] mapped = new ValidationRule<TProcessResult>[rules.Length];

        for (int i = 0; i < rules.Length; i++)
            mapped[i] = new ValidationRule<TProcessResult>(rules[i]);

        Rules = mapped;
    }

    /// <summary>
    ///     Initialises a new instance of the <see cref="ProcessResultValidator{TProcessResult}" /> class
    ///     from a set of self-describing validation rules.
    /// </summary>
    /// <param name="rules">The self-describing validation rules to evaluate against the process result.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rules" /> is <c>null</c>.</exception>
    public ProcessResultValidator(ValidationRule<TProcessResult>[] rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        Rules = rules;
    }

    /// <summary>
    ///     Gets the self-describing validation rules applied by this validator.
    /// </summary>
    public ValidationRule<TProcessResult>[] Rules { get; }

    /// <inheritdoc />
    public Func<TProcessResult, bool>[] ValidationRules => Rules.Select(rule => rule.Predicate).ToArray();

    /// <summary>
    ///     Validates a <see cref="TProcessResult" /> against the configured <see cref="Rules" />.
    /// </summary>
    /// <param name="result">The <see cref="TProcessResult" /> to validate against the validation rules.</param>
    /// <returns>True if the <paramref name="result" /> passes all validation rules, false otherwise.</returns>
    public bool Validate(TProcessResult result)
    {
        foreach (ValidationRule<TProcessResult> rule in Rules)
        {
            bool ruleResult = rule.Predicate(result);

            if (!ruleResult)
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public ValidationFailure<TProcessResult>[] GetValidationFailures(TProcessResult result)
    {
        List<ValidationFailure<TProcessResult>> failures = new();

        foreach (ValidationRule<TProcessResult> rule in Rules)
        {
            if (!rule.Predicate(result))
                failures.Add(new ValidationFailure<TProcessResult>(rule, result));
        }

        return failures.ToArray();
    }
}