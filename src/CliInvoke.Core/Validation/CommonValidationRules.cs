/*
    CliInvoke.Core
     
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Linq;

namespace CliInvoke.Core.Validation;

/// <summary>
/// Provides a set of predefined validation rules for processing results.
/// These rules validate instances of <typeparamref name="TProcessResult"/>,
/// which must inherit from <see cref="TProcessResult"/>.
/// </summary>
/// <typeparam name="TProcessResult">
/// The type of the process result being validated, constrained to derive from <see cref="ProcessResult"/>.
/// </typeparam>
public static class CommonValidationRules<TProcessResult>
    where TProcessResult : ProcessResult
{
    /// <summary>
    /// A validation rule that always returns true, effectively bypassing any validation logic.
    /// </summary>
    /// <returns>
    /// A function that takes an instance of <typeparamref name="TProcessResult"/> as input and always evaluates to true.
    /// </returns>
    public static Func<TProcessResult, bool> NoValidation() =>
        (_ => true);

    /// <summary>
    /// A validation rule that checks whether the process result's exit code matches the specified value.
    /// </summary>
    /// <param name="exitCode">
    /// The expected exit code that the process result must match for the validation to succeed.
    /// </param>
    /// <returns>
    /// A function that takes an instance of <typeparamref name="TProcessResult"/> as input and evaluates to true if the exit code matches; otherwise, false.
    /// </returns>
    public static Func<TProcessResult, bool> RequiresExitCode(int exitCode)
        => (result => result.ExitCode == exitCode);

    /// <summary>
    /// A validation rule that checks whether the process result's exit code is one of the specified allowed codes.
    /// </summary>
    /// <param name="exitCodes">
    /// The collection of allowed exit codes that the process result can match for the validation to succeed.
    /// </param>
    /// <returns>
    /// A function that takes an instance of <typeparamref name="TProcessResult"/> as input and evaluates to true if the exit code matches any in the provided collection; otherwise, false.
    /// </returns>
    public static Func<TProcessResult, bool> RequiresAllowedExitCodes(params int[] exitCodes)
        => (result => exitCodes.Any(code => result.ExitCode == code));

    /// <summary>
    /// A predefined validation rule that ensures the process result's exit code is zero.
    /// </summary>
    /// <typeparam name="TProcessResult">
    /// The type of the process result being validated, constrained to derive from <see cref="ProcessResult"/>.
    /// </typeparam>
    /// <returns>
    /// A function that evaluates to true if the exit code of the process result equals zero; otherwise, false.
    /// </returns>
    public static Func<TProcessResult, bool> RequiresExitCodeZero
        => RequiresExitCode(0);
}