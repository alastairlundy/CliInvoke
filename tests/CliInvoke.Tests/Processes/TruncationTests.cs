/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Runtime.InteropServices;
using System.Text;

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
        ProcessConfiguration configuration = new ProcessConfiguration(target, arguments);
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
        ProcessConfiguration configuration = new ProcessConfiguration(target, arguments);
        using ExternalProcess process = new(new FilePathResolver(), configuration,
            ProcessExitConfiguration.CreateGraceful());

        process.Start();

        BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);

        await Assert.That(result.WasTruncated).IsFalse();
        await Assert.That(result.StandardOutput.Length).IsGreaterThan(1000);
    }

    [Test]
    public async Task CaptureBufferedResultAsync_WithNegativeCap_DoesNotTruncate()
    {
        (string target, string arguments) = GetLargeOutputCommand();
        ProcessConfiguration configuration = new ProcessConfiguration(target, arguments);
        using ExternalProcess process = new(new FilePathResolver(), configuration,
            ProcessExitConfiguration.CreateGraceful());

        process.Start();

        BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None, -1, -1);

        await Assert.That(result.WasTruncated).IsFalse();
        await Assert.That(result.StandardOutput.Length).IsGreaterThan(1000);
    }

    [Test]
    public async Task CaptureBufferedResultAsync_WithZeroCap_ProducesEmptyTextAndTruncated()
    {
        (string target, string arguments) = GetLargeOutputCommand();
        ProcessConfiguration configuration = new ProcessConfiguration(target, arguments);
        using ExternalProcess process = new(new FilePathResolver(), configuration,
            ProcessExitConfiguration.CreateGraceful());

        process.Start();

        BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None, 0, 0);

        await Assert.That(result.WasTruncated).IsTrue();
        await Assert.That(result.StandardOutput).IsEmpty();
        await Assert.That(result.StandardError).IsEmpty();
    }

    [Test]
    public async Task CaptureBufferedResultAsync_WithMultibyteBoundary_NoReplacementCharacter()
    {
        // Build a payload with multibyte UTF-8 characters (e.g. Chinese characters).
        // Each character is 3 bytes in UTF-8. Use a cap that cuts mid-character to
        // verify the incremental decoder drops the split sequence cleanly.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // PowerShell approach: output multibyte characters then cap at an odd byte boundary.
            ProcessConfiguration configuration = new ProcessConfiguration(
                "powershell.exe",
                "-NoProfile -Command \"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; Write-Output '你好世界'\"");
            using ExternalProcess process = new(new FilePathResolver(), configuration,
                ProcessExitConfiguration.CreateGraceful());

            process.Start();

            // '你好世界' is 12 bytes in UTF-8. Cap at 7 bytes (cuts mid-character).
            BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None, 7, 7);

            await Assert.That(result.WasTruncated).IsTrue();
            await Assert.That(result.StandardOutput).DoesNotContain('\uFFFD');
        }
        else
        {
            // bash: printf writes raw UTF-8 bytes.
            ProcessConfiguration configuration = new ProcessConfiguration(
                "/bin/bash",
                "-c \"printf '你好世界'\"");
            using ExternalProcess process = new(new FilePathResolver(), configuration,
                ProcessExitConfiguration.CreateGraceful());

            process.Start();

            // '你好世界' is 12 bytes in UTF-8. Cap at 7 bytes (cuts mid-character).
            BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None, 7, 7);

            await Assert.That(result.WasTruncated).IsTrue();
            await Assert.That(result.StandardOutput).DoesNotContain('\uFFFD');
        }
    }

    [Test]
    public async Task CaptureBufferedResultAsync_WithMultibyteBoundary_PartialCharactersDropped()
    {
        // Verify that when the cap splits a multibyte sequence, only complete characters
        // are included in the output.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ProcessConfiguration configuration = new ProcessConfiguration(
                "powershell.exe",
                "-NoProfile -Command \"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; Write-Output '你好世界'\"");
            using ExternalProcess process = new(new FilePathResolver(), configuration,
                ProcessExitConfiguration.CreateGraceful());

            process.Start();

            // 3 bytes of '你' + 3 bytes of '好' = 6 bytes. Cap at 6 = 2 complete characters.
            BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None, 6, 6);

            await Assert.That(result.WasTruncated).IsTrue();
            await Assert.That(result.StandardOutput.Length).IsEqualTo(2);
        }
        else
        {
            ProcessConfiguration configuration = new ProcessConfiguration(
                "/bin/bash",
                "-c \"printf '你好世界'\"");
            using ExternalProcess process = new(new FilePathResolver(), configuration,
                ProcessExitConfiguration.CreateGraceful());

            process.Start();

            // 3 bytes of '你' + 3 bytes of '好' = 6 bytes. Cap at 6 = 2 complete characters.
            BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None, 6, 6);

            await Assert.That(result.WasTruncated).IsTrue();
            await Assert.That(result.StandardOutput.Length).IsEqualTo(2);
        }
    }
}
