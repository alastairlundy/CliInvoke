/*
    AlastairLundy.DotPrimitives 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */


using System;
using System.Diagnostics;

using AlastairLundy.CliInvoke.Core.Internal;

using AlastairLundy.CliInvoke.Core.Primitives;

using AlastairLundy.DotExtensions.Processes;

namespace AlastairLundy.CliInvoke.Core;

/// <summary>
/// 
/// </summary>
public static class ProcessSetPolicyExtensions
{
    /// <summary>
    /// Applies a ProcessResourcePolicy to a Process.
    /// </summary>
    /// <param name="process">The process to apply the policy to.</param>
    /// <param name="resourcePolicy">The process resource policy to be applied.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void SetResourcePolicy(this Process process, ProcessResourcePolicy? resourcePolicy)
    {
        if (resourcePolicy is null)
        {
            resourcePolicy = ProcessResourcePolicy.Default;
        }

        if (process.HasStarted() == false)
        {
            throw new InvalidOperationException(Resources.Exceptions_ResourcePolicy_CannotSetToNonStartedProcess);
        }

        if (process.HasStarted() && (OperatingSystem.IsWindows() || OperatingSystem.IsLinux()))
        {
            if (resourcePolicy.ProcessorAffinity is not null)
            {
                process.ProcessorAffinity = (IntPtr)resourcePolicy.ProcessorAffinity;
            }
        }

        if (OperatingSystem.IsMacOS() ||
            OperatingSystem.IsMacCatalyst() ||
            OperatingSystem.IsFreeBSD() ||
            OperatingSystem.IsWindows())
        {
            if (resourcePolicy.MinWorkingSet is not null)
            {
                process.MinWorkingSet = (nint)resourcePolicy.MinWorkingSet;
            }

            if (resourcePolicy.MaxWorkingSet is not null)
            {
                process.MaxWorkingSet = (nint)resourcePolicy.MaxWorkingSet;
            }
        }
        
        process.PriorityClass = resourcePolicy.PriorityClass;
        process.PriorityBoostEnabled = resourcePolicy.EnablePriorityBoost;
    }
}