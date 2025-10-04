/*
    AlastairLundy.DotPrimitives 
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
public class ProcessTimeoutPolicy : IEquatable<ProcessTimeoutPolicy>
{
        
    /// <summary>
    /// 
    /// </summary>
    public ProcessTimeoutPolicy()
    {
        TimeoutThreshold = TimeSpan.FromMinutes(30);
        CancellationMode = ProcessCancellationMode.Graceful;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="timeoutThreshold"></param>
    /// <param name="cancellationMode"></param>
    public ProcessTimeoutPolicy(TimeSpan timeoutThreshold, ProcessCancellationMode cancellationMode)
    {
        TimeoutThreshold = timeoutThreshold;
        CancellationMode = cancellationMode;
    }
        
    /// <summary>
    /// 
    /// </summary>
    public static ProcessTimeoutPolicy Default { get; } = new ProcessTimeoutPolicy(TimeSpan.FromMinutes(30),
        ProcessCancellationMode.Graceful);
        
    /// <summary>
    /// Disables waiting for Process Timeout.
    /// </summary>
    public static ProcessTimeoutPolicy None { get; } = new ProcessTimeoutPolicy(TimeSpan.FromSeconds(0),
        ProcessCancellationMode.None);
        
    /// <summary>
    /// The timespan after which a Process should no longer be allowed to continue waiting to exit.
    /// </summary>
    public TimeSpan TimeoutThreshold { get; }
        
    /// <summary>
    /// 
    /// </summary>
    public ProcessCancellationMode CancellationMode { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(ProcessTimeoutPolicy? other)
    {
        if (other is null)
            return false;
            
        return TimeoutThreshold.Equals(other.TimeoutThreshold)
               && CancellationMode == other.CancellationMode;
    }

    /// <summary>
    /// /
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        if(obj is null)
            return false;
            
        if (obj is ProcessTimeoutPolicy policy)
            return Equals(policy);

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(TimeoutThreshold, (int)CancellationMode);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool Equals(ProcessTimeoutPolicy? left, ProcessTimeoutPolicy? right)
    {
        if (left is null || right is null)
            return false;
            
        return left.Equals(right);
    }
        
    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(ProcessTimeoutPolicy? left, ProcessTimeoutPolicy? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(ProcessTimeoutPolicy? left, ProcessTimeoutPolicy? right)
    {
        return Equals(left, right) == false;
    }
        
    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator >(ProcessTimeoutPolicy? left, ProcessTimeoutPolicy? right)
    {
        if (left is null || right is null)
            return false;

        return left.TimeoutThreshold > right.TimeoutThreshold;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator <(ProcessTimeoutPolicy? left, ProcessTimeoutPolicy? right)
    {
        if (left is null || right is null)
            return false;

        if (left.CancellationMode == ProcessCancellationMode.None &&
            right.CancellationMode != ProcessCancellationMode.None)
            return false;

        return left.TimeoutThreshold < right.TimeoutThreshold;
    }
        
    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator >=(ProcessTimeoutPolicy? left, ProcessTimeoutPolicy? right)
    {
        if (left is null || right is null)
            return false;

        return left.TimeoutThreshold >= right.TimeoutThreshold;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator <=(ProcessTimeoutPolicy? left, ProcessTimeoutPolicy? right)
    {
        if (left is null || right is null)
            return false;

        return left.TimeoutThreshold <= right.TimeoutThreshold;
    }
}