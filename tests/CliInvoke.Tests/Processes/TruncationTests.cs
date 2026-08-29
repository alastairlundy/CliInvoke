/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Runtime.InteropServices;

using CliInvoke.Processes;

namespace CliInvoke.Tests.Processes;

/// <summary>
///     Verifies per-stream output truncation during buffered capture: when a per-stream cap is
///     supplied the captured output is truncated and <see cref="BufferedProcessResult.WasTruncated"/>
///     is set; when no cap is supplied the full output is captured (prior behaviour).
/// </summary>
public class TruncationTests
{
    // Produces ~20000 bytes of 'a' on stdout so a 1000-byte cap is unambiguously exceeded.
    private static (string Target, string Arguments) GetLargeOutputCommand()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ("powershell.exe", "-NoProfile -Command \"Write-Output('a' * 20000)\"");

        return ("/bin/bash", "-c \"printf 'a%.0s' {1..20000}\"");
    }

    [Test]
    public async Task CaptureBufferedResultAsync_WithCap_TruncatesAndSetsWasTruncated()
    {
        (string target, string arguments) = GetLargeOutputCommand();
        using ProcessConfiguration configuration = ProcessConfigurationFactory.Create(target, arguments);
        using ExternalProcess process = new(new FilePathResolver(), configuration,
            ProcessExitConfiguration.CreateGraceful());

        process.Start();

        BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None, 1000, 1000);

        await Assert.That(result.WasTruncated).IsTrue();
        await Assert.That(result.StandardOutput.Length).IsLessThanOrEqualTo(1000);
    }

    [Test]
    public async Task CaptureBufferedResultAsync_WithoutCap_DoesNotTruncate()
    {
        (string target, string arguments) = GetLargeOutputCommand();
        using ProcessConfiguration configuration = ProcessConfigurationFactory.Create(target, arguments);
        using ExternalProcess process = new(new FilePathResolver(), configuration,
            ProcessExitConfiguration.CreateGraceful());

        process.Start();

        BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);

        await Assert.That(result.WasTruncated).IsFalse();
        await Assert.That(result.StandardOutput.Length).IsGreaterThan(1000);
    }
}
