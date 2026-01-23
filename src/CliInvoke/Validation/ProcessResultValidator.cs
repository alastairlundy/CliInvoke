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
/// Represents a validator for process results. It provides functionality to validate a given process result against a set of specified rules.
/// </summary>
/// <typeparam name="TProcessResult">
/// The type of the process result being validated.
/// Must be a class that inherits from the <see cref="CliInvoke.Core.ProcessResult"/> class.
/// </typeparam>
public class ProcessResultValidator<TProcessResult> : IProcessResultValidator<TProcessResult> where TProcessResult : ProcessResult
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="rules"></param>
    public ProcessResultValidator(Func<TProcessResult, bool>[] rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        
        ValidationRules = rules;
    }
    
    /// <inheritdoc/>
    public Func<TProcessResult, bool>[] ValidationRules { get; }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public bool Validate(TProcessResult result)
    {
        foreach (Func<TProcessResult, bool> rule in ValidationRules)
        {
            bool ruleResult = rule(result);

            if (!ruleResult)
                return false;
        }

        return true;
    }
}