/*
    AlastairLundy.CliInvoke.Core 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;

namespace AlastairLundy.CliInvoke.Core.Primitives;

/// <summary>
/// 
/// </summary>
public class ProcessExitInfo : IEquatable<ProcessExitInfo>
{
    /// <summary>
    /// 
    /// </summary>
    public ProcessExitInfo()
    {
        TimeoutPolicy = ProcessTimeoutPolicy.Default;
        ResultValidation = ProcessResultValidation.ExitCodeZero;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="timeoutPolicy"></param>
    /// <param name="resultValidation"></param>
    public ProcessExitInfo(ProcessTimeoutPolicy timeoutPolicy, ProcessResultValidation resultValidation)
    {
        TimeoutPolicy = timeoutPolicy;
        ResultValidation = resultValidation;
    }

    /// <summary>
    /// 
    /// </summary>
    public static ProcessExitInfo Default =
        new ProcessExitInfo(ProcessTimeoutPolicy.Default,
            ProcessResultValidation.ExitCodeZero);
    
    /// <summary>
    /// 
    /// </summary>
    public ProcessResultValidation ResultValidation { get; }
    
    /// <summary>
    /// 
    /// </summary>
    public ProcessTimeoutPolicy TimeoutPolicy { get; }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(ProcessExitInfo? other)
    {
        if (other is null) return false;

        return ResultValidation == other.ResultValidation &&
               TimeoutPolicy.Equals(other.TimeoutPolicy);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;

        if (obj is ProcessExitInfo exitInfo)
            return Equals(exitInfo);

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)ResultValidation, TimeoutPolicy);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool Equals(ProcessExitInfo? left, ProcessExitInfo? right)
    {
        if(left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(ProcessExitInfo? left, ProcessExitInfo? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(ProcessExitInfo? left, ProcessExitInfo? right)
    {
        return Equals(left, right) == false;
    }
}