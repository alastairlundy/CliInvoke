/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Helpers.Processes;

namespace CliInvoke.Helpers;

internal class ProcessWrapper : Process, IDisposable
{
    // Synchronization primitives for cancellation operations
    internal readonly SemaphoreSlim CancellationSemaphore = new(1, 1);
    
    internal readonly SemaphoreSlim ForcefulExitLock = new(1, 1);
    
    // Track if forceful exit has been attempted to prevent double invocation
    internal bool ForcefulExitAttempted = false;
    
    // Track if disposed to prevent multiple disposals
    private bool _disposed;
    
    internal ProcessWrapper(ProcessConfiguration configuration, 
        ProcessResourcePolicy? resourcePolicy)
    {
        StartInfo = configuration.ToProcessStartInfo(configuration.RedirectStandardOutput,
            configuration.RedirectStandardError);
        ProcessName = StartInfo.FileName;
        EnableRaisingEvents = true;
        Exited += OnExited;
        Started += OnStarted;
        
        HasStarted = false;
        ResourcePolicy = resourcePolicy ?? ProcessResourcePolicy.Default;
    }

    private void OnStarted(object? sender, EventArgs e)
    {
        if (!HasExited && HasStarted)
        {
            try
            {
#pragma warning disable CA1416
                this.SetResourcePolicy(ResourcePolicy);
#pragma warning restore CA1416
            }
            catch
            {
                // ignored
            }
        }
    }

    private void OnExited(object? sender, EventArgs e)
    {
        ExitTime = base.ExitTime;
    }

    internal ProcessResourcePolicy ResourcePolicy { get; set; }

    internal bool HasStarted { get; private set; }

    internal event EventHandler Started;

    internal new DateTime StartTime { get; private set; }

    internal new DateTime ExitTime { get; private set; }
    
    internal new int Id {get; private set; }
    
    internal new string ProcessName {get; private set; }

    public new bool Start()
    {
        bool result = base.Start();

        HasStarted = result;
        
        if (result)
        {
            StartTime = DateTime.UtcNow;
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            Started?.Invoke(this, EventArgs.Empty);
            Id = base.Id;
            ProcessName = base.ProcessName;
        }

        return result;
    }
    
    public new void Dispose()
    {
        if (_disposed)
            return;
        
        _disposed = true;
        
        // Dispose of SemaphoreSlim instances
        CancellationSemaphore.Dispose();
        ForcefulExitLock.Dispose();
        
        // Unsubscribe from events to prevent memory leaks
        Exited -= OnExited;
        Started -= OnStarted;
        
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}