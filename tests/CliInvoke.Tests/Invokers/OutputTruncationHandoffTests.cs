/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Extensions;
using CliInvoke.Extensions.Middleware.Truncation;
using CliInvoke.Tests.Helpers;
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
// These children are real powershell.exe spawns through the pipeline; on a loaded CI runner
// the first spawn of a run can stall past the guard below. Retry only the guard timeout so a
// genuine deadlock still fails the build after the retries are exhausted.
[Retry(2, RetryOnExceptionTypes = new[] { typeof(TimeoutException) })]
public class OutputTruncationHandoffTests
{
    // Produces ~20000 bytes of 'a' on stdout so a 1000-byte cap is unambiguously exceeded.
    private static (string Target, string Arguments) GetLargeOutputCommand()
    {
        if (OperatingSystem.IsWindows())
            return ("powershell.exe", "-NoProfile -Command \"Write-Output('a' * 20000)\"");

        return ("/bin/bash", "-c \"printf 'a%.0s' {1..20000}\"");
    }

    /// <summary>
    ///     Captures diagnostics for the child this class starts on the current OS:
    ///     <c>powershell</c>/<c>pwsh</c> on Windows, and the <c>bash</c> child that
    ///     <see cref="GetLargeOutputCommand"/> spawns on POSIX, so a guard timeout
    ///     reports the state of the process that is actually stuck.
    /// </summary>
    /// <returns>A diagnostic snapshot of the relevant child processes.</returns>
    private static string CaptureChildState()
    {
        return OperatingSystem.IsWindows()
            ? ChildProcessStateDiagnostics.CapturePowerShell()
            : ChildProcessStateDiagnostics.Capture("bash");
    }

    [Test]
    public async Task RunBufferedAsync_WithMaxBufferedOutputBytes_TruncatesAndSetsWasTruncated()
    {
        (string target, string arguments) = GetLargeOutputCommand();

        Task<BufferedProcessResult> runTask = CliRun
            .RunBufferedAsync(target, arguments, maxBufferedOutputBytes: 1000);
        Task completedTask = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(45))).ConfigureAwait(true);
        if (completedTask != runTask)
        {
            string diag = CaptureChildState();
            throw new System.TimeoutException($"Timed out after 45s. Child state:\n{diag}");
        }
        BufferedProcessResult result = await runTask.ConfigureAwait(true);

        await Assert.That(result.WasTruncated).IsTrue();
        await Assert.That(result.StandardOutput.Length).IsLessThanOrEqualTo(1000);
    }

    [Test]
    public async Task RunBufferedAsync_WithoutMaxBufferedOutputBytes_DoesNotTruncate()
    {
        (string target, string arguments) = GetLargeOutputCommand();

        Task<BufferedProcessResult> runTask = CliRun
            .RunBufferedAsync(target, arguments);
        Task completedTask = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(45))).ConfigureAwait(true);
        if (completedTask != runTask)
        {
            string diag = CaptureChildState();
            throw new System.TimeoutException($"Timed out after 45s. Child state:\n{diag}");
        }
        BufferedProcessResult result = await runTask.ConfigureAwait(true);

        await Assert.That(result.WasTruncated).IsFalse();
        await Assert.That(result.StandardOutput.Length).IsGreaterThan(1000);
    }

    [Test]
    public async Task RunBufferedAsync_ExitConfigurationCap_TruncatesAndSetsWasTruncated()
    {
        (string target, string arguments) = GetLargeOutputCommand();

        ProcessConfiguration configuration = new ProcessConfiguration(target, arguments);
        ProcessExitConfiguration exit = ProcessExitConfiguration.WithMaxBufferedOutputBytes(
            ProcessExitConfiguration.CreateGraceful(), 1000);

        Task<BufferedProcessResult> runTask = CliRun
            .RunBufferedAsync(configuration, exit);
        Task completedTask = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(45))).ConfigureAwait(true);
        if (completedTask != runTask)
        {
            string diag = CaptureChildState();
            throw new System.TimeoutException($"Timed out after 45s. Child state:\n{diag}");
        }
        BufferedProcessResult result = await runTask.ConfigureAwait(true);

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

        Task<BufferedProcessResult> runTask = invoker
            .ExecuteBufferedAsync(config, ProcessExitConfiguration.CreateGraceful());
        Task completedTask = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(45))).ConfigureAwait(true);
        if (completedTask != runTask)
        {
            string diag = CaptureChildState();
            throw new System.TimeoutException($"Timed out after 45s. Child state:\n{diag}");
        }
        BufferedProcessResult result = await runTask.ConfigureAwait(true);

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

        Task<BufferedProcessResult> runTask = invoker
            .ExecuteBufferedAsync(config, ProcessExitConfiguration.CreateGraceful());
        Task completedTask = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(45))).ConfigureAwait(true);
        if (completedTask != runTask)
        {
            string diag = CaptureChildState();
            throw new System.TimeoutException($"Timed out after 45s. Child state:\n{diag}");
        }
        BufferedProcessResult result = await runTask.ConfigureAwait(true);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.WasTruncated).IsFalse();
        await Assert.That(result.StandardOutput.Length).IsGreaterThan(1000);
    }

    [Test]
    public async Task ExecuteBufferedAsync_WithoutTruncationMiddleware_ExitConfigurationCap_Truncates()
    {
        // The configuration path serves callers without middleware. A cap set on the
        // exit configuration is honored even when no truncation middleware is registered.
        IServiceCollection services = new ServiceCollection();
        services.AddCliInvoke();
        IProcessInvoker invoker = services.BuildServiceProvider().GetRequiredService<IProcessInvoker>();

        (string target, string arguments) = GetLargeOutputCommand();
        ProcessConfiguration config = new ProcessConfiguration(target, arguments);
        ProcessExitConfiguration exit = ProcessExitConfiguration.WithMaxBufferedOutputBytes(
            ProcessExitConfiguration.CreateGraceful(), 1000);

        Task<BufferedProcessResult> runTask = invoker
            .ExecuteBufferedAsync(config, exit);
        Task completedTask = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(45))).ConfigureAwait(true);
        if (completedTask != runTask)
        {
            string diag = CaptureChildState();
            throw new System.TimeoutException($"Timed out after 45s. Child state:\n{diag}");
        }
        BufferedProcessResult result = await runTask.ConfigureAwait(true);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.WasTruncated).IsTrue();
        await Assert.That(result.StandardOutput.Length).IsLessThanOrEqualTo(1000);
    }
}
