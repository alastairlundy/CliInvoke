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
/// 
/// </summary>
/// <typeparam name="TProcessResult"></typeparam>
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
    
    /// <summary>
    /// 
    /// </summary>
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