/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Core.Validation;

/// <summary>
/// Represents a validator interface for process results. It defines methods to obtain validation rules and validate a given process result.
/// </summary>
/// <typeparam name="TProcessResult">
/// The type of the process result being validated, which must inherit from the 'ProcessResult' class.
/// </typeparam>
public interface IProcessResultValidator<in TProcessResult> where TProcessResult : ProcessResult
{
    /// <summary>
    /// The validation rules to be applied to the process result. Each rule is a function
    /// that takes a process result of type <typeparamref name="TProcessResult"/> and returns a boolean.
    /// Each function represents a condition that the process result must satisfy to be considered valid.
    /// </summary>
    Func<TProcessResult, bool>[] ValidationRules { get; }

    /// <summary>
    /// Validates the given process result by applying all specified validation rules.
    /// </summary>
    /// <param name="result">The result of the process to be validated.</param>
    /// <returns>
    /// Returns true if all validation rules pass, otherwise false.
    /// </returns>
    bool Validate(TProcessResult result);
}