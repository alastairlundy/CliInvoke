/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Runtime.InteropServices;

using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Core.Processes;
using CliInvoke.Extensions;
using CliInvoke.Extensions.Middleware.Truncation;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Tests.Invokers;

/// <summary>
///     Guards against the buffered-capture deadlock: the pipeline must start the process without
///     awaiting exit so the capture reader can drain the redirected streams while waiting for exit.
///     If the pipeline awaited exit first, a child writing more than the OS pipe buffer would block
///     and the capture reader would never start.
/// </summary>
public class BufferedCaptureNoDeadlockTests
{
    // Emits ~200 KB so the stream exceeds the default OS pipe buffer, forcing the deadlock if the
    // pipeline awaits exit before the capture reader starts.
    private static (string Target, string Arguments) GetLargeOutputCommand()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ("powershell.exe", "-NoProfile -Command \"Write-Output('a' * 200000)\"");

        return ("/bin/bash", "-c \"printf 'a%.0s' {1..200000}\"");
    }

    [Test]
    public async Task ExecuteBufferedAsync_LargeOutput_CompletesWithoutDeadlock_AndTruncates()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke(builder => builder.UseOutputTruncation(new TruncationOptions { MaxSize = 4096 }));
        IProcessInvoker invoker = services.BuildServiceProvider().GetRequiredService<IProcessInvoker>();

        (string target, string arguments) = GetLargeOutputCommand();
        using ProcessConfiguration config = ProcessConfigurationFactory.Create(target, arguments);

        // Wrap with a timeout so a regression (deadlock) fails fast instead of hanging the run.
        BufferedProcessResult result = await invoker
            .ExecuteBufferedAsync(config, ProcessExitConfiguration.CreateGraceful())
            .WaitAsync(TimeSpan.FromSeconds(30));

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.WasTruncated).IsTrue();
        await Assert.That(result.StandardOutput.Length).IsLessThanOrEqualTo(4096);
    }
}
