/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Processes.Internal.ControlAdapters;

internal static class ProcessControlAdapterFactory
{
    internal static BaseProcessControlAdapter Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsProcessControlAdapter();
        }

        return new UnixProcessControlAdapter();
    }
}