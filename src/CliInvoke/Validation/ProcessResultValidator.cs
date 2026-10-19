/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Validation;

namespace CliInvoke.Validation;

/// <summary>
///     Validates process results against a set of rules.
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

        ValidationRules = mapped;
#pragma warning disable CS0618 // Type or member is obsolete
        Rules = ValidationRules;
#pragma warning restore CS0618 // Type or member is obsolete
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
        
        ValidationRules = rules;
#pragma warning disable CS0618 // Type or member is obsolete
        Rules = ValidationRules;
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    ///     Gets the self-describing validation rules applied by this validator.
    /// </summary>
    [Obsolete(DeprecationMessages.DeprecationV4)]
    public ValidationRule<TProcessResult>[] Rules { get; private init; }

    /// <inheritdoc />
    public ValidationRule<TProcessResult>[] ValidationRules {
#pragma warning disable CS0618 // Type or member is obsolete
        get => Rules;
        private init => Rules = value;
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    ///     Validates a <typeparam name="TProcessResult">The type of ProcessResult to validate.</typeparam> <c>TProcessResult</c> against the configured <see cref="ValidationRules" />.
    /// </summary>
    /// <param name="result">The <c>TProcessResult</c> to validate against the validation rules.</param>
    /// <returns>True if the <paramref name="result" /> passes all validation rules, false otherwise.</returns>
    public bool Validate(TProcessResult result)
    {
        foreach (ValidationRule<TProcessResult> rule in ValidationRules)
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
        List<ValidationFailure<TProcessResult>> failures = [];

        foreach (ValidationRule<TProcessResult> rule in ValidationRules)
        {
            if (!rule.Predicate(result))
                failures.Add(new ValidationFailure<TProcessResult>(rule, result));
        }

        return [.. failures];
    }
}