﻿/*
    CliRunner 
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.Diagnostics;
using System.Runtime.Versioning;

namespace CliRunner;

/// <summary>
/// A class that defines a Process' resource configuration.
/// </summary>
public class ProcessResourcePolicy
{
    /// <summary>
    /// Instantiates the ProcessResourcePolicy with default values unless specified parameters are provided.
    /// </summary>
    /// <param name="processorAffinity">The processor affinity to be used for the Process.</param>
    /// <param name="minWorkingSet">The Minimum Working Set Size for the Process.</param>
    /// <param name="maxWorkingSet">The Maximum Working Set Size for the Process.</param>
    /// <param name="priorityClass">The priority class to assign to the Process.</param>
    /// <param name="enablePriorityBoost">Whether to enable Priority Boost if the process window enters focus.</param>
    public ProcessResourcePolicy(nint processorAffinity = default(nint),
        nint? minWorkingSet = null, 
        nint? maxWorkingSet = null,
        ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal,
        bool enablePriorityBoost = true)
    {
        MinWorkingSet = minWorkingSet;
        MaxWorkingSet = maxWorkingSet;
        ProcessorAffinity = processorAffinity;
        PriorityClass = priorityClass;
        EnablePriorityBoost = enablePriorityBoost;
    }

    /// <summary>
    /// The cores and threads to assign to the Process.
    /// </summary>
    /// <remarks>Process objects only support Processor Affinity on Windows and Linux operating systems.</remarks>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
#endif
    public nint ProcessorAffinity { get; }
    
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
    public nint? MinWorkingSet { get; }
    
    /// <summary>
    /// Maximum Working Set size to be used for the Process.
    /// </summary>
    public nint? MaxWorkingSet { get; }
    
    /// <summary>
    /// Creates a ProcessResourcePolicy with a default configuration.
    /// </summary>
    public static ProcessResourcePolicy Default { get; } = new ProcessResourcePolicy();
}