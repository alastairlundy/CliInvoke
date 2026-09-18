/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

using CliInvoke.Processes.Internal;

namespace CliInvoke.Tests.Processes;

public class CommandLineLengthCheckTests
{
    [Test]
    [SupportedOSPlatform("windows")]
    public async Task Arguments_ExceedingWindowsLimit_ThrowsArgumentException()
    {
        if (!OperatingSystem.IsWindows())
            return;

        string longArgument = new string('a', 33_000);

        ProcessConfiguration configuration = new ProcessConfiguration("dotnet.exe")
        {
            Arguments = longArgument
        };

        await Assert.That(() => new ProcessWrapper(
                configuration,
                new FileInfo("dotnet.exe")))
            .Throws<ArgumentException>();
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task ArgumentList_ExceedingWindowsLimit_ThrowsArgumentException()
    {
        if (!OperatingSystem.IsWindows())
            return;

        List<string> args = new List<string>();

        for (int i = 0; i < 100; i++)
            args.Add(new string('b', 400));

        ProcessConfiguration configuration = new ProcessConfiguration("dotnet.exe")
        {
            ArgumentList = args
        };

        await Assert.That(() => new ProcessWrapper(
                configuration,
                new FileInfo("dotnet.exe")))
            .Throws<ArgumentException>();
    }
}
