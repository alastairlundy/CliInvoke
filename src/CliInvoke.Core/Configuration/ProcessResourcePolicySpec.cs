/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Diagnostics;

namespace CliInvoke.Core.Configuration;

/// <summary>
///     A sealed configuration seam for constructing process resource policies,
///     replacing the former ProcessResourcePolicyBuilder / IProcessResourcePolicyBuilder pair.
/// </summary>
public sealed class ProcessResourcePolicySpec
{
    private nint? _processorAffinity;
    private nint? _minWorkingSet;
    private nint? _maxWorkingSet;
    private ProcessPriorityClass _priorityClass;
    private bool _enablePriorityBoost;

    /// <summary>
    ///     Instantiates the <see cref="ProcessResourcePolicySpec" /> with default values.
    /// </summary>
    public ProcessResourcePolicySpec()
    {
#pragma warning disable CA1416
        _processorAffinity = ProcessResourcePolicy.Default.ProcessorAffinity;
#pragma warning restore CA1416

        _minWorkingSet = null;
        _maxWorkingSet = null;
        _priorityClass = ProcessPriorityClass.Normal;
        _enablePriorityBoost = false;
    }

    /// <summary>
    ///     Configures the processor affinity for the process resource policy.
    /// </summary>
    /// <param name="processorAffinity">The processor affinity to be used.</param>
    /// <returns>The current <see cref="ProcessResourcePolicySpec" /> instance.</returns>
    /// <remarks>Process objects only support Processor Affinity on Windows and Linux operating systems.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown if processor affinity is less than 1 or greater than 2x Processor Count.
    /// </exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    public ProcessResourcePolicySpec SetProcessorAffinity(nint processorAffinity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(processorAffinity, 0x0001);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(processorAffinity,
            2 * Environment.ProcessorCount);

        _processorAffinity = processorAffinity;

        return this;
    }

    /// <summary>
    ///     Configures the minimum and maximum working set for the process resource policy.
    /// </summary>
    /// <param name="minWorkingSet">The minimum working set to be used.</param>
    /// <param name="maxWorkingSet">The maximum working set to be used.</param>
    /// <returns>The current <see cref="ProcessResourcePolicySpec" /> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown if minWorkingSet is negative, or maxWorkingSet is less than minWorkingSet.
    /// </exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("freebsd")]
    public ProcessResourcePolicySpec SetWorkingSet(nint minWorkingSet, nint maxWorkingSet)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minWorkingSet);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxWorkingSet, minWorkingSet);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minWorkingSet, maxWorkingSet);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxWorkingSet, 1);

        _minWorkingSet = minWorkingSet;
        _maxWorkingSet = maxWorkingSet;

        return this;
    }

    /// <summary>
    ///     Configures the process priority class for the process resource policy.
    /// </summary>
    /// <param name="processPriorityClass">The process priority class to be used.</param>
    /// <returns>The current <see cref="ProcessResourcePolicySpec" /> instance.</returns>
    public ProcessResourcePolicySpec SetPriorityClass(
        ProcessPriorityClass processPriorityClass)
    {
        _priorityClass = processPriorityClass;

        return this;
    }

    /// <summary>
    ///     Configures the priority boost behaviour for the process resource policy.
    /// </summary>
    /// <param name="enablePriorityBoost">The priority boost behaviour to be used.</param>
    /// <returns>The current <see cref="ProcessResourcePolicySpec" /> instance.</returns>
    public ProcessResourcePolicySpec ConfigurePriorityBoost(bool enablePriorityBoost)
    {
        _enablePriorityBoost = enablePriorityBoost;

        return this;
    }

    /// <summary>
    ///     Builds the configured <see cref="ProcessResourcePolicy" />.
    /// </summary>
    /// <returns>The configured <see cref="ProcessResourcePolicy" />.</returns>
    public ProcessResourcePolicy Build() =>
        new(_processorAffinity, _minWorkingSet,
            _maxWorkingSet, _priorityClass, _enablePriorityBoost);
}
