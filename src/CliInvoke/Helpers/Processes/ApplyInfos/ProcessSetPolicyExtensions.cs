/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */



// ReSharper disable RedundantBoolCompare

namespace CliInvoke.Helpers.Processes;

/// <summary>
///
/// </summary>
internal static class ProcessSetPolicyExtensions
{
    /// <param name="process">The process to apply the policy to.</param>
    extension(ProcessWrapper process)
    {
        /// <summary>
        /// Applies a ProcessResourcePolicy to a Process.
        /// </summary>
        /// <param name="resourcePolicy">The process resource policy to be applied.</param>
        /// <exception cref="InvalidOperationException"></exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
        [SupportedOSPlatform("android")]
        internal void SetResourcePolicy(ProcessResourcePolicy? resourcePolicy)
        {
            resourcePolicy ??= ProcessResourcePolicy.Default;

            if (process.HasStarted == false)
                throw new InvalidOperationException(
                    Resources.Exceptions_ResourcePolicy_CannotSetToNonStartedProcess
                );

            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            {
                if (resourcePolicy.ProcessorAffinity is not null) 
                    process.ProcessorAffinity = (IntPtr)resourcePolicy.ProcessorAffinity;
            }

            if (OperatingSystem.IsMacOS()
                || OperatingSystem.IsMacCatalyst()
                || OperatingSystem.IsFreeBSD()
                || OperatingSystem.IsWindows()
               )
            {
                if (resourcePolicy.MinWorkingSet is not null) 
                    process.MinWorkingSet = (nint)resourcePolicy.MinWorkingSet;

                if (resourcePolicy.MaxWorkingSet is not null) 
                    process.MaxWorkingSet = (nint)resourcePolicy.MaxWorkingSet;
            }

            process.PriorityClass = resourcePolicy.PriorityClass;
            process.PriorityBoostEnabled = resourcePolicy.EnablePriorityBoost;
        }
    }
}
