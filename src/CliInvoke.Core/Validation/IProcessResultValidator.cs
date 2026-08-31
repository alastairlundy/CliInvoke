/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core.Validation;

/// <summary>
///     Represents a validator interface for process results. It defines methods to obtain validation
///     rules and validate a given process result.
/// </summary>
/// <typeparam name="TProcessResult">
///     The type of the process result being validated, which must inherit from the 'ProcessResult'
///     class.
/// </typeparam>
public interface IProcessResultValidator<TProcessResult> where TProcessResult : ProcessResult
{
    /// <summary>
    ///     The self-describing validation rules to be applied to the process result. Each rule is a
    ///     <see cref="ValidationRule{TProcessResult}" /> that carries its own name and failure message,
    ///     so failures can be reported without relying on opaque predicates.
    /// </summary>
    ValidationRule<TProcessResult>[] ValidationRules { get; }

    /// <summary>
    ///     Validates the given process result by applying all specified validation rules.
    /// </summary>
    /// <param name="result">The result of the process to be validated.</param>
    /// <returns>
    ///     Returns true if all validation rules pass, otherwise false.
    /// </returns>
    bool Validate(TProcessResult result);

    /// <summary>
    ///     Validates the given process result and returns the collection of rules that failed.
    /// </summary>
    /// <param name="result">The result of the process to be validated.</param>
    /// <returns>
    ///     A collection of <see cref="ValidationFailure{TProcessResult}" /> instances, one per failed rule.
    ///     The collection is empty when all rules pass.
    /// </returns>
    ValidationFailure<TProcessResult>[] GetValidationFailures(TProcessResult result);

    /// <summary>
    ///     Determines whether a process result indicates the operation should be retried.
    /// </summary>
    /// <remarks>
    ///     The default implementation returns the inverse of <see cref="Validate"/>: a validated (successful)
    ///     result is not retried, while a failed result is. This reuses existing validators to classify
    ///     failures as retryable without requiring a separate classifier type. Implementers may override this
    ///     to distinguish retryable from terminal failures.
    /// </remarks>
    /// <param name="result">The result of the process to evaluate for retryability.</param>
    /// <returns>
    ///     <c>true</c> if the process result indicates the operation should be retried; otherwise, <c>false</c>.
    /// </returns>
    bool ShouldRetry(TProcessResult result) => !Validate(result);
}