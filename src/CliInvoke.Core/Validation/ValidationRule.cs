/*
    CliInvoke.Core
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace CliInvoke.Core.Validation;

/// <summary>
///     Represents a single, self-describing validation rule applied to a process result.
/// </summary>
/// <typeparam name="TProcessResult">
///     The type of the process result being validated, constrained to derive from
///     <see cref="ProcessResult" />.
/// </typeparam>
public sealed class ValidationRule<TProcessResult>
    where TProcessResult : ProcessResult
{
    /// <summary>
    ///     Initialises a new instance of the <see cref="ValidationRule{TProcessResult}" /> class.
    /// </summary>
    /// <param name="predicate">
    ///     The predicate that evaluates to <c>true</c> when the rule passes and <c>false</c> when it fails.
    /// </param>
    /// <param name="name">An optional human-readable name for the rule, used in failure reporting.</param>
    /// <param name="failureMessage">
    ///     An optional constant message describing the failure. Ignored when <paramref name="failureMessageFactory" />
    ///     is provided.
    /// </param>
    /// <param name="failureMessageFactory">
    ///     An optional factory that produces a result-specific failure message. Takes precedence over
    ///     <paramref name="failureMessage" />.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate" /> is <c>null</c>.</exception>
    public ValidationRule(
        Func<TProcessResult, bool> predicate,
        string? name = null,
        string? failureMessage = null,
        Func<TProcessResult, string>? failureMessageFactory = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        Predicate = predicate;
        Name = name ?? "Unnamed validation rule";
        FailureMessage = failureMessage;
        FailureMessageFactory = failureMessageFactory;
    }

    /// <summary>
    ///     Gets the human-readable name of the rule, used when reporting failures.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the predicate that evaluates the process result.
    /// </summary>
    public Func<TProcessResult, bool> Predicate { get; }

    /// <summary>
    ///     Gets the constant failure message, if any.
    /// </summary>
    public string? FailureMessage { get; }

    /// <summary>
    ///     Gets the factory that produces a result-specific failure message, if any.
    /// </summary>
    public Func<TProcessResult, string>? FailureMessageFactory { get; }

    /// <summary>
    ///     Produces the failure message for a given result, preferring the factory when present.
    /// </summary>
    /// <param name="result">The process result that failed the rule.</param>
    /// <returns>A descriptive failure message.</returns>
    public string GetFailureMessage(TProcessResult result)
    {
        if (FailureMessageFactory is not null)
            return FailureMessageFactory(result);

        return FailureMessage ?? $"Validation rule '{Name}' failed.";
    }
}
