/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Builders;

/// <summary>
/// A class to fluently configure and build ProcessResourcePolicy objects.
/// </summary>
public class ProcessResourcePolicyBuilder : IProcessResourcePolicyBuilder
{
    private readonly ProcessResourcePolicy _processResourcePolicy;

    /// <summary>
    /// Instantiates the ProcessResourcePolicy Builder with default ProcessResourcePolicy values.
    /// </summary>
    public ProcessResourcePolicyBuilder()
    {
        _processResourcePolicy = ProcessResourcePolicy.Default;
    }

    /// <summary>
    /// Internally instantiates the ProcessResourcePolicy object with the specified ProcessResourcePolicy value.
    /// </summary>
    /// <param name="processResourcePolicy">The process resource policy object to use.</param>
    protected ProcessResourcePolicyBuilder(ProcessResourcePolicy processResourcePolicy)
    {
        _processResourcePolicy = processResourcePolicy;
    }

    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified ProcessorAffinity.
    /// </summary>
    /// <param name="processorAffinity">The processor affinity to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated ProcessorAffinity.</returns>
    /// <remarks>Process objects only support Processor Affinity on Windows and Linux operating systems.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if processor affinity is less than 1 or greater than 2x Processor Count.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [Pure]
    public IProcessResourcePolicyBuilder SetProcessorAffinity(nint processorAffinity)
    {
        if (processorAffinity < 1)
            throw new ArgumentOutOfRangeException(nameof(processorAffinity));

        if (processorAffinity > 2 * Environment.ProcessorCount)
            throw new ArgumentOutOfRangeException(nameof(processorAffinity));

        return new ProcessResourcePolicyBuilder(
            new(
                processorAffinity,
#pragma warning disable CA1416
                _processResourcePolicy.MinWorkingSet,
                _processResourcePolicy.MaxWorkingSet,
#pragma warning restore CA1416
                _processResourcePolicy.PriorityClass,
                _processResourcePolicy.EnablePriorityBoost
            )
        );
    }

    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified Minimum Working Set.
    /// </summary>
    /// <remarks>If <see cref="minWorkingSet"/> is higher than the configured maximum working set then the maximum working set will be overriden with the new <see cref="minWorkingSet"/> value.</remarks>
    /// <param name="minWorkingSet">The minimum working set to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated minimum working set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <see cref="minWorkingSet"/> is less than 0.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("android")]
    [Pure]
    public IProcessResourcePolicyBuilder SetMinWorkingSet(nint minWorkingSet)
    {
#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegative(minWorkingSet);
#else
        minWorkingSet = Ensure.NotNegative(minWorkingSet);
#endif
        
        if (minWorkingSet >= _processResourcePolicy.MaxWorkingSet)
            return new ProcessResourcePolicyBuilder(
                new(
#pragma warning disable CA1416
                    _processResourcePolicy.ProcessorAffinity,
#pragma warning restore CA1416
                    minWorkingSet: minWorkingSet,
                    maxWorkingSet: minWorkingSet,
                    _processResourcePolicy.PriorityClass,
                    _processResourcePolicy.EnablePriorityBoost
                )
            );

        return new ProcessResourcePolicyBuilder(
            new(
#pragma warning disable CA1416
                _processResourcePolicy.ProcessorAffinity,
#pragma warning restore CA1416
                minWorkingSet: minWorkingSet,
                _processResourcePolicy.MaxWorkingSet,
                _processResourcePolicy.PriorityClass,
                _processResourcePolicy.EnablePriorityBoost
            )
        );
    }

    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified Maximum Working Set.
    /// </summary>
    /// <param name="maxWorkingSet">The maximum working set to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated maximum working set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the <see cref="maxWorkingSet"/> is less than the min working set value or less than 1.</exception>
    [Pure]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("android")]
    public IProcessResourcePolicyBuilder SetMaxWorkingSet(nint maxWorkingSet)
    {
        //TODO: Migrate to Ensure and ArgumentOutOfRange exception static methods once fixed in Polyfill   
        if (maxWorkingSet < _processResourcePolicy.MinWorkingSet || maxWorkingSet < 1)
            throw new ArgumentOutOfRangeException(nameof(maxWorkingSet));

        if (_processResourcePolicy.MinWorkingSet > maxWorkingSet)
            throw new ArgumentOutOfRangeException(nameof(maxWorkingSet));

        return new ProcessResourcePolicyBuilder(
            new(
#pragma warning disable CA1416
                _processResourcePolicy.ProcessorAffinity,
#pragma warning restore CA1416
                _processResourcePolicy.MinWorkingSet,
                maxWorkingSet,
                _processResourcePolicy.PriorityClass,
                _processResourcePolicy.EnablePriorityBoost
            )
        );
    }

    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified Process Priority Class.
    /// </summary>
    /// <param name="processPriorityClass">The Process Priority Class to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated Process Priority Class.</returns>
    [Pure]
    public IProcessResourcePolicyBuilder SetPriorityClass(
        ProcessPriorityClass processPriorityClass
    )
    {
        return new ProcessResourcePolicyBuilder(
            new(
#pragma warning disable CA1416
                _processResourcePolicy.ProcessorAffinity,
                _processResourcePolicy.MinWorkingSet,
                _processResourcePolicy.MaxWorkingSet,
#pragma warning restore CA1416
                processPriorityClass,
                _processResourcePolicy.EnablePriorityBoost
            )
        );
    }

    /// <summary>
    /// Configures the ProcessResourcePolicyBuilder with the specified Priority Boost behaviour.
    /// </summary>
    /// <param name="enablePriorityBoost">The priority boost behaviour to be used.</param>
    /// <returns>The newly created ProcessResourcePolicyBuilder with the updated priority boost behaviour.</returns>
    [Pure]
    public IProcessResourcePolicyBuilder ConfigurePriorityBoost(bool enablePriorityBoost) =>
        new ProcessResourcePolicyBuilder(
            new(
#pragma warning disable CA1416
                _processResourcePolicy.ProcessorAffinity,
                _processResourcePolicy.MinWorkingSet,
                _processResourcePolicy.MaxWorkingSet,
#pragma warning restore CA1416
                _processResourcePolicy.PriorityClass,
                enablePriorityBoost
            )
        );

    /// <summary>
    /// Builds the configured ProcessResourcePolicy
    /// </summary>
    /// <returns>The configured ProcessResourcePolicy.</returns>
    public ProcessResourcePolicy Build() => _processResourcePolicy;
}
