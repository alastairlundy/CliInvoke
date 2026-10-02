/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

#if NET11_0

using CliInvoke.Processes.Internal;

namespace CliInvoke.Tests.Processes;

/// <summary>
///     Verifies the net11.0 suspended-start scoping in <see cref="ProcessWrapper.Start"/>:
///     <c>ProcessStartInfo.StartSuspended</c> is enabled on Windows and macOS only. The
///     runtime rejects the property everywhere else with
///     <see cref="PlatformNotSupportedException"/> (Linux/FreeBSD have no create-suspended
///     OS primitive), so a non-Windows/non-macOS runner must observe it left
///     <see langword="false"/> after a start.
/// </summary>
public class StartSuspendedTests
{
    [Test]
    public async Task Start_SetsStartSuspended_OnWindowsAndMacOSOnly()
    {
        ProcessConfiguration configuration = new(ProcessTestHelper.GetTargetFilePath(), string.Empty);

        FilePathResolver resolver = new();
        ProcessWrapper process = new(configuration, resolver.ResolveFilePath(configuration.TargetFilePath));

        try
        {
            process.Start();

            bool expected = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

#pragma warning disable CA1416 // Reading the property is safe everywhere; the attribute guards its effect.
            await Assert.That(process.StartInfo.StartSuspended).IsEqualTo(expected);
#pragma warning restore CA1416
        }
        finally
        {
            if (process.HasStarted && !process.HasExited)
            {
                try { process.Kill(true); } catch { process.Kill(); }
            }

            process.Dispose();
        }
    }
}

#endif
