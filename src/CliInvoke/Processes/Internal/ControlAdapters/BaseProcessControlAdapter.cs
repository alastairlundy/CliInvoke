/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using CliInvoke.Helpers;

namespace CliInvoke.Processes.Internal.ControlAdapters;

internal abstract class BaseProcessControlAdapter
{
   internal abstract void ResumeProcess(Process process);
   
   internal abstract void SuspendProcess(Process process);

   internal abstract Task<bool> SendInterruptSignalAsync(Process process,
       CancellationReason cancellationReason, ProcessExitConfiguration exitConfiguration,
       CancellationToken cancellationToken);
}