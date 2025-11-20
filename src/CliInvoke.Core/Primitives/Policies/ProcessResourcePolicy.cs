/*
    AlastairLundy.DotPrimitives
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace CliInvoke.Core;

/// <summary>
/// A class that defines a Process' resource configuration.
/// </summary>
public class ProcessResourcePolicy : IEquatable<ProcessResourcePolicy>
{
    /// <summary>
    /// Instantiates the <see cref="ProcessResourcePolicy"/> with default values unless specified parameters are provided.
    /// </summary>
    /// <param name="processorAffinity">The processor affinity to be used for the Process.</param>
    /// <param name="minWorkingSet">The Minimum Working Set Size for the Process.</param>
    /// <param name="maxWorkingSet">The Maximum Working Set Size for the Process.</param>
    /// <param name="priorityClass">The priority class to assign to the Process.</param>
    /// <param name="enablePriorityBoost">Whether to enable Priority Boost if the process window enters focus.</param>
    public ProcessResourcePolicy(
        IntPtr? processorAffinity = null,
        nint? minWorkingSet = null,
        nint? maxWorkingSet = null,
        ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal,
        bool enablePriorityBoost = false
    )
    {
        if (minWorkingSet is not null)
            if (minWorkingSet < 0)
                throw new ArgumentOutOfRangeException(nameof(minWorkingSet));

        if (minWorkingSet is not null && maxWorkingSet is not null)
        {
            if (maxWorkingSet < minWorkingSet || maxWorkingSet < 1)
                throw new ArgumentOutOfRangeException(nameof(maxWorkingSet));

            if (minWorkingSet > maxWorkingSet)
                throw new ArgumentOutOfRangeException(nameof(maxWorkingSet));
        }

        if (processorAffinity is not null)
        {
#if NETSTANDARD2_0
            if (processorAffinity < (nint)1)
#else
            if (processorAffinity < 1)
#endif
                throw new ArgumentOutOfRangeException(nameof(processorAffinity));

            if (processorAffinity > (nint)2 * Environment.ProcessorCount)
                throw new ArgumentOutOfRangeException(nameof(processorAffinity));
        }

#pragma warning disable CA1416
        MinWorkingSet = minWorkingSet;
        MaxWorkingSet = maxWorkingSet;
        ProcessorAffinity = processorAffinity;
#pragma warning restore CA1416

        PriorityClass = priorityClass;
        EnablePriorityBoost = enablePriorityBoost;
    }

    /// <summary>
    /// The cores and threads to assign to the Process.
    /// </summary>
    /// <remarks>Process objects only support Processor Affinity on Windows and Linux operating systems.</remarks>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    public IntPtr? ProcessorAffinity { get; }

    /// <summary>
    /// The priority class to assign to the Process.
    /// </summary>
    public ProcessPriorityClass PriorityClass { get; }

    /// <summary>
    /// Whether to enable Priority Boost if/when the main window of the Process enters focus.
    /// </summary>
    public bool EnablePriorityBoost { get; }

    /// <summary>
    /// The Minimum Working Set size to be used for the Process.
    /// </summary>
    /// <remarks>This property is not supported on Linux-based operating systems.</remarks>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("android")]
    public nint? MinWorkingSet { get; }

    /// <summary>
    /// Maximum Working Set size to be used for the Process.
    /// </summary>
    /// <remarks>This property is not supported on Linux-based operating systems.</remarks>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("linux")]
    [UnsupportedOSPlatform("android")]
    public nint? MaxWorkingSet { get; }

    /// <summary>
    /// Creates a ProcessResourcePolicy with a default configuration.
    /// </summary>
    public static ProcessResourcePolicy Default { get; } = new ProcessResourcePolicy();

    /// <summary>
    /// Determines whether this ProcessResourcePolicy is equal to another ProcessResourcePolicy.
    /// </summary>
    /// <param name="other">The ProcessResourcePolicy to compare against.</param>
    /// <returns>True if the both Process Resource Policies are equal; false otherwise.</returns>
    public bool Equals(ProcessResourcePolicy? other)
    {
        if (other is null)
            return false;

#pragma warning disable CA1416
        return ProcessorAffinity == other.ProcessorAffinity
            && PriorityClass == other.PriorityClass
            && EnablePriorityBoost == other.EnablePriorityBoost
            && MinWorkingSet == other.MinWorkingSet
            && MaxWorkingSet == other.MaxWorkingSet;
#pragma warning restore CA1416
    }

    /// <summary>
    /// Determines whether this ProcessResourcePolicy is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns>True if the other object is a ProcessResourcePolicy and is equal to this ProcessResourcePolicy,
    /// false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (obj is ProcessResourcePolicy policy)
        {
            return Equals(policy);
        }

        return false;
    }

    /// <summary>
    /// Returns the hash code for the current ProcessResourcePolicy.
    /// </summary>
    /// <returns>The hash code for the current ProcessResourcePolicy.</returns>
    public override int GetHashCode()
    {
#pragma warning disable CA1416
        return HashCode.Combine(
            ProcessorAffinity,
            (int)PriorityClass,
            EnablePriorityBoost,
            MinWorkingSet,
            MaxWorkingSet
        );
#pragma warning restore CA1416
    }

    /// <summary>
    /// Determines if a Process Resource Policy is equal to another Process Resource Policy.
    /// </summary>
    /// <param name="left">A Process Resource Policy to be compared.</param>
    /// <param name="right">The other  Process Resource Policy to be compared.</param>
    /// <returns>True if both  Process Resource Policies are equal to each other; false otherwise.</returns>
    public static bool Equals(ProcessResourcePolicy? left, ProcessResourcePolicy? right)
    {
        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// Determines if a Process Resource Policy is equal to another Process Resource Policy.
    /// </summary>
    /// <param name="left">A Process Resource Policy to be compared.</param>
    /// <param name="right">The other  Process Resource Policy to be compared.</param>
    /// <returns>True if both  Process Resource Policies are equal to each other; false otherwise.</returns>
    public static bool operator ==(ProcessResourcePolicy? left, ProcessResourcePolicy? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines if a Process Resource Policy is not equal to another Process Resource Policy.
    /// </summary>
    /// <param name="left">A Process Resource Policy to be compared.</param>
    /// <param name="right">The other Process Resource Policy to be compared.</param>
    /// <returns>True if both Process Resource Policies are not equal to each other; false otherwise.</returns>
    public static bool operator !=(ProcessResourcePolicy? left, ProcessResourcePolicy? right) =>
        Equals(left, right) == false;
}
