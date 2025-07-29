/*
    AlastairLundy.DotPrimitives 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */


using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using AlastairLundy.CliInvoke.Core.Internal;

using AlastairLundy.CliInvoke.Core.Primitives.Policies;

#if NETSTANDARD2_0
using OperatingSystem = Polyfills.OperatingSystemPolyfill;
#endif

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
        bool processHasStarted = false;

        try
        {
            processHasStarted = process.HasExited == false
                                && process.StartTime.ToUniversalTime() <= DateTime.UtcNow;
        }
        catch
        {
            processHasStarted = false;
        }
        
        if (processHasStarted && resourcePolicy != null)
        {
#if NET5_0_OR_GREATER
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
#else
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
#endif
            {
                if (resourcePolicy.ProcessorAffinity is not null)
                {
                    process.ProcessorAffinity = (IntPtr)resourcePolicy.ProcessorAffinity;
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                OperatingSystem.IsMacCatalyst() ||
                OperatingSystem.IsFreeBSD() ||
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (resourcePolicy.MinWorkingSet != null)
                {
                    process.MinWorkingSet = (nint)resourcePolicy.MinWorkingSet;
                }

                if (resourcePolicy.MaxWorkingSet != null)
                {
                    process.MaxWorkingSet = (nint)resourcePolicy.MaxWorkingSet;
                }
            }
        
            process.PriorityClass = resourcePolicy.PriorityClass;
            process.PriorityBoostEnabled = resourcePolicy.EnablePriorityBoost;
        }
        else
        {
            throw new InvalidOperationException(Resources.Exceptions_ResourcePolicy_CannotSetToNonStartedProcess);
        }
    }
}