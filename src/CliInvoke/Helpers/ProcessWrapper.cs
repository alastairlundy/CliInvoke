/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.ComponentModel;

using CliInvoke.Helpers.Processes;

namespace CliInvoke.Helpers;

internal class ProcessWrapper : Process
{
    // Synchronization primitive to prevent simultaneous cancellation attempts
    internal readonly SemaphoreSlim _cancellationSemaphore = new(1, 1);

    internal ProcessWrapper(ProcessConfiguration configuration,
        ProcessResourcePolicy? resourcePolicy)
    {
        StartInfo = configuration.ToProcessStartInfo();
        ProcessName = StartInfo.FileName;
        EnableRaisingEvents = true;
        Exited += OnExited;
        Started += OnStarted;

        HasStarted = false;
        ResourcePolicy = resourcePolicy ?? ProcessResourcePolicy.Default;
    }

    internal ProcessResourcePolicy ResourcePolicy { get; set; }

    internal bool HasStarted { get; private set; }

    internal new DateTime StartTime { get; private set; }

    internal new DateTime ExitTime { get; private set; }

    internal new int Id { get; private set; }

    internal new string ProcessName { get; private set; }

    private void OnStarted(object? sender, EventArgs e)
    {
        if (!HasExited && HasStarted)
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

    private void OnExited(object? sender, EventArgs e)
    {
        ExitTime = base.ExitTime;
    }

    internal event EventHandler Started;

    public new bool Start()
    {
        try
        {
            HasStarted = base.Start();
        }
        catch(Win32Exception exception)
        {
            HasStarted = false;

            throw new UnauthorizedAccessException($"The current user does not have permission to execute the file '{StartInfo.FileName}'.", exception);
        }

        if (!HasStarted)
        {
            throw new InvalidOperationException($"Process with Target File Name of '{StartInfo.FileName}' could not be started.");
        }

        if (!HasStarted) return HasStarted;
        StartTime = DateTime.UtcNow;
        Started.Invoke(this, EventArgs.Empty);
        Id = base.Id;
        ProcessName = base.ProcessName;

        return HasStarted;
    }
}