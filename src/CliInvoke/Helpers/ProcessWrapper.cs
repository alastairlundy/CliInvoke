/*
    AlastairLundy.CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Diagnostics;

using AlastairLundy.CliInvoke.Core;
using AlastairLundy.CliInvoke.Helpers.Processes;

namespace AlastairLundy.CliInvoke.Helpers;

internal class ProcessWrapper : Process
{
    internal ProcessWrapper(ProcessResourcePolicy? resourcePolicy)
    {
       Exited += OnExited;
       Started += OnStarted;

       HasStarted = false;
       ResourcePolicy = resourcePolicy ?? ProcessResourcePolicy.Default;
    }
    
    private void OnStarted(object? sender, EventArgs e)
    {
        if (HasExited == false)
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
    
    public new bool Start()
    {
        bool result = base.Start();

        if (result)
        {
            HasStarted = true;
            StartTime = DateTime.UtcNow;
            
            Started?.Invoke(this, EventArgs.Empty);
        }
        
        return result;
    }
}