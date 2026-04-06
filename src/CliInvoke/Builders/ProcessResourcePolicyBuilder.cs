/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Builders;

/// <summary>
///     A class to fluently configure and build ProcessResourcePolicy objects.
/// </summary>
public class ProcessResourcePolicyBuilder : IProcessResourcePolicyBuilder
{
    private IntPtr? internalProcessorAffinity;
    private nint? internalMinWorkingSet;
    private nint? internalMaxWorkingSet;
    private ProcessPriorityClass internalPriorityClass;
    private bool internalEnablePriorityBoost;

    /// <summary>
    ///     Instantiates the ProcessResourcePolicy Builder with default ProcessResourcePolicy values.
    /// </summary>
    public ProcessResourcePolicyBuilder()
    {
#pragma warning disable CA1416
        internalProcessorAffinity = ProcessResourcePolicy.Default.ProcessorAffinity;
#pragma warning restore CA1416

        internalEnablePriorityBoost = false;
    }

    /// <summary>
    ///     Configures the ProcessResourcePolicyBuilder with the specified ProcessorAffinity.
    /// </summary>
    /// <param name="processorAffinity">The processor affinity to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated ProcessorAffinity.</returns>
    /// <remarks>Process objects only support Processor Affinity on Windows and Linux operating systems.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown if processor affinity is less than 1 or greater than 2x Processor Count.
    /// </exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    public IProcessResourcePolicyBuilder SetProcessorAffinity(nint processorAffinity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(processorAffinity, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(processorAffinity,
            2 * Environment.ProcessorCount);

        internalProcessorAffinity = processorAffinity;
        
        return this;
    }

    /// <summary>
    ///     Configures the ProcessResourcePolicyBuilder with the specified Minimum Working Set.
    /// </summary>
    /// <remarks>
    ///     If <see cref="internalMinWorkingSet" /> is higher than the configured maximum working set then the
    ///     maximum working set will be overriden with the new <see cref="internalMinWorkingSet" /> value.
    /// </remarks>
    /// <param name="minWorkingSet">The minimum working set to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated minimum working set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown if <see cref="internalMinWorkingSet" /> is less than 0.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if the <see cref="minWorkingSet"/> is greater than or equal to the maximum working set.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("freebsd")]
    public IProcessResourcePolicyBuilder SetMinWorkingSet(nint minWorkingSet)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minWorkingSet);

        if (internalMaxWorkingSet is not null) 
            ArgumentOutOfRangeException.ThrowIfGreaterThan(minWorkingSet, (nint)internalMaxWorkingSet);
        else
            internalMaxWorkingSet = minWorkingSet + 1;
        
        internalMinWorkingSet = minWorkingSet;
        
        return this;
    }

    /// <summary>
    ///     Configures the ProcessResourcePolicyBuilder with the specified Maximum Working Set.
    /// </summary>
    /// <param name="maxWorkingSet">The maximum working set to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated maximum working set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown if the <see cref="internalMaxWorkingSet" /> is less
    ///     than the min working set value or less than 1.
    /// </exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("freebsd")]
    public IProcessResourcePolicyBuilder SetMaxWorkingSet(nint maxWorkingSet)
    {
        nint minWorkingSet = internalMinWorkingSet ?? 0;

        ArgumentOutOfRangeException.ThrowIfLessThan(maxWorkingSet,
            minWorkingSet);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minWorkingSet, maxWorkingSet);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxWorkingSet, 1);

        internalMaxWorkingSet = maxWorkingSet;

        internalMinWorkingSet ??= 0;
        
        return this;
    }

    /// <summary>
    ///     Configures the ProcessResourcePolicyBuilder with the specified Process Priority Class.
    /// </summary>
    /// <param name="processPriorityClass">The Process Priority Class to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated Process Priority Class.</returns>
    public IProcessResourcePolicyBuilder SetPriorityClass(
        ProcessPriorityClass processPriorityClass)
    {
        internalPriorityClass = processPriorityClass;

        return this;
    }

    /// <summary>
    ///     Configures the ProcessResourcePolicyBuilder with the specified Priority Boost behaviour.
    /// </summary>
    /// <param name="enablePriorityBoost">The priority boost behaviour to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated priority boost behaviour.</returns>
    public IProcessResourcePolicyBuilder ConfigurePriorityBoost(bool enablePriorityBoost)
    {
        internalEnablePriorityBoost = enablePriorityBoost;

        return this;
    }

    /// <summary>
    ///     Builds the configured ProcessResourcePolicy
    /// </summary>
    /// <returns>The configured ProcessResourcePolicy.</returns>
    [Pure]
    public ProcessResourcePolicy Build() =>
        new(internalProcessorAffinity, internalMinWorkingSet,
            internalMaxWorkingSet, internalPriorityClass, internalEnablePriorityBoost);
}