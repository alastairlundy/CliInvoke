/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Runtime.InteropServices;
using CliInvoke.Extensions;
using CliInvoke.Extensions.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Tests.Invokers;

/// <summary>
///     Pins the truncation handoff end-to-end on both entry paths the middleware deepening authorized:
///     the <see cref="CliRun"/> configuration path and the <c>UseOutputTruncation</c> middleware sugar path.
///     The cap travels on <see cref="ProcessExitConfiguration.MaxBufferedOutputBytes"/> and is read by the
///     pipeline from <c>ctx.ExitConfiguration</c>; absence of a cap preserves unbounded capture
///     (<see cref="BufferedProcessResult.WasTruncated"/> is <c>false</c>). No test references the deleted
///     <c>MiddlewareItems</c> string-key handoff.
/// </summary>
public class OutputTruncationHandoffTests
{
    // Produces ~20000 bytes of 'a' on stdout so a 1000-byte cap is unambiguously exceeded.
    private static (string Target, string Arguments) GetLargeOutputCommand()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ("powershell.exe", "-NoProfile -Command \"Write-Output('a' * 20000)\"");

        return ("/bin/bash", "-c \"printf 'a%.0s' {1..20000}\"");
    }

    [Test]
    public async Task RunBufferedAsync_WithMaxBufferedOutputBytes_TruncatesAndSetsWasTruncated()
    {
        (string target, string arguments) = GetLargeOutputCommand();

        BufferedProcessResult result = await CliRun
            .RunBufferedAsync(target, arguments, maxBufferedOutputBytes: 1000)
            .WaitAsync(TimeSpan.FromSeconds(30));

        await Assert.That(result.WasTruncated).IsTrue();
        await Assert.That(result.StandardOutput.Length).IsLessThanOrEqualTo(1000);
    }

    [Test]
    public async Task RunBufferedAsync_WithoutMaxBufferedOutputBytes_DoesNotTruncate()
    {
        (string target, string arguments) = GetLargeOutputCommand();

        BufferedProcessResult result = await CliRun
            .RunBufferedAsync(target, arguments)
            .WaitAsync(TimeSpan.FromSeconds(30));

        await Assert.That(result.WasTruncated).IsFalse();
        await Assert.That(result.StandardOutput.Length).IsGreaterThan(1000);
    }

    [Test]
    public async Task RunBufferedAsync_ExitConfigurationCap_TruncatesAndSetsWasTruncated()
    {
        (string target, string arguments) = GetLargeOutputCommand();

        ProcessConfiguration configuration = new ProcessConfiguration(target, arguments);
        ProcessExitConfiguration exit = ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(
            ProcessExitConfiguration.CreateGraceful(), 1000);

        BufferedProcessResult result = await CliRun
            .RunBufferedAsync(configuration, exit)
            .WaitAsync(TimeSpan.FromSeconds(30));

        await Assert.That(result.WasTruncated).IsTrue();
        await Assert.That(result.StandardOutput.Length).IsLessThanOrEqualTo(1000);
    }

    [Test]
    public async Task ExecuteBufferedAsync_WithOutputTruncationMiddleware_TruncatesAndSetsWasTruncated()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke(builder => builder.UseOutputTruncation(new TruncationOptions { MaxBytes = 1000 }));
        IProcessInvoker invoker = services.BuildServiceProvider().GetRequiredService<IProcessInvoker>();

        (string target, string arguments) = GetLargeOutputCommand();
        ProcessConfiguration config = new ProcessConfiguration(target, arguments);

        BufferedProcessResult result = await invoker
            .ExecuteBufferedAsync(config, ProcessExitConfiguration.CreateGraceful())
            .WaitAsync(TimeSpan.FromSeconds(30));

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.WasTruncated).IsTrue();
        await Assert.That(result.StandardOutput.Length).IsLessThanOrEqualTo(1000);
    }

    [Test]
    public async Task ExecuteBufferedAsync_WithoutTruncationMiddleware_NoCap_DoesNotTruncate()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke();
        IProcessInvoker invoker = services.BuildServiceProvider().GetRequiredService<IProcessInvoker>();

        (string target, string arguments) = GetLargeOutputCommand();
        ProcessConfiguration config = new ProcessConfiguration(target, arguments);

        BufferedProcessResult result = await invoker
            .ExecuteBufferedAsync(config, ProcessExitConfiguration.CreateGraceful())
            .WaitAsync(TimeSpan.FromSeconds(30));

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.WasTruncated).IsFalse();
        await Assert.That(result.StandardOutput.Length).IsGreaterThan(1000);
    }

    [Test]
    public async Task ExecuteBufferedAsync_WithoutTruncationMiddleware_ExitConfigurationCap_Truncates()
    {
        // T005 constraint: the configuration path serves callers without middleware. A cap set on the
        // exit configuration is honored even when no truncation middleware is registered.
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke();
        IProcessInvoker invoker = services.BuildServiceProvider().GetRequiredService<IProcessInvoker>();

        (string target, string arguments) = GetLargeOutputCommand();
        ProcessConfiguration config = new ProcessConfiguration(target, arguments);
        ProcessExitConfiguration exit = ProcessExitConfigurationCreationExtensions.WithMaxBufferedOutputBytes(
            ProcessExitConfiguration.CreateGraceful(), 1000);

        BufferedProcessResult result = await invoker
            .ExecuteBufferedAsync(config, exit)
            .WaitAsync(TimeSpan.FromSeconds(30));

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.WasTruncated).IsTrue();
        await Assert.That(result.StandardOutput.Length).IsLessThanOrEqualTo(1000);
    }
}
