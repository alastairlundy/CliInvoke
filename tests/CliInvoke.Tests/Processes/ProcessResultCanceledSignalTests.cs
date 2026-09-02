/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke;
using CliInvoke.Processes.Internal;
using System.Runtime.InteropServices;

namespace CliInvoke.Tests.Processes;

/// <summary>
///     Validates the <see cref="ProcessResult.Canceled"/> and
///     <see cref="ProcessResult.Signal"/> model: truth conditions,
///     orthogonality, and an OS-gated Unix SIGTERM integration.
/// </summary>
public class ProcessResultCanceledSignalTests
{
    private readonly ProcessConfiguration _configuration;

    public ProcessResultCanceledSignalTests()
    {
        _configuration = ProcessConfigurationFactory.Create(ProcessTestHelper.GetTargetFilePath(), string.Empty);
    }

    private static InvocationContext RawContext(ProcessConfiguration configuration)
        => new(configuration, ProcessExitConfiguration.CreateGraceful(), InvocationMode.Raw, CancellationToken.None);

    // ---- Mock-injected truth-condition tests ----

    [Test]
    public async Task Raw_Result_Carries_Injected_Canceled_True()
    {
        CountingExternalProcessFactory factory = new() { DefaultCanceled = true };
        ProcessInvocationPipeline pipeline = new(factory);

        ProcessResult result = await pipeline.InvokeAsync<ProcessResult>(RawContext(_configuration));

        await Assert.That(result.Canceled).IsTrue();
        await Assert.That(result.Signal).IsNull();
    }

    [Test]
    public async Task Raw_Result_Carries_Injected_Canceled_False()
    {
        CountingExternalProcessFactory factory = new() { DefaultCanceled = false };
        ProcessInvocationPipeline pipeline = new(factory);

        ProcessResult result = await pipeline.InvokeAsync<ProcessResult>(RawContext(_configuration));

        await Assert.That(result.Canceled).IsFalse();
    }

    [Test]
    public async Task Buffered_Result_Carries_Injected_Canceled_True()
    {
        CountingExternalProcessFactory factory = new() { DefaultCanceled = true };
        ProcessInvocationPipeline pipeline = new(factory);
        InvocationContext ctx = new(_configuration, ProcessExitConfiguration.CreateGraceful(),
            InvocationMode.Buffered, CancellationToken.None);

        BufferedProcessResult result = await pipeline.InvokeAsync<BufferedProcessResult>(ctx);

        await Assert.That(result.Canceled).IsTrue();
    }

    // ---- Orthogonality tests ----

    [Test]
    public async Task NonNull_Signal_Does_Not_Force_Canceled()
    {
        CountingExternalProcessFactory factory = new()
        {
            DefaultCanceled = false,
            DefaultSignal = PosixSignal.SIGTERM
        };
        ProcessInvocationPipeline pipeline = new(factory);

        ProcessResult result = await pipeline.InvokeAsync<ProcessResult>(RawContext(_configuration));

        await Assert.That(result.Signal).IsEqualTo(PosixSignal.SIGTERM);
        await Assert.That(result.Canceled).IsFalse();
    }

    [Test]
    public async Task Canceled_True_Does_Not_Require_Signal()
    {
        CountingExternalProcessFactory factory = new()
        {
            DefaultCanceled = true,
            DefaultSignal = null
        };
        ProcessInvocationPipeline pipeline = new(factory);

        ProcessResult result = await pipeline.InvokeAsync<ProcessResult>(RawContext(_configuration));

        await Assert.That(result.Canceled).IsTrue();
        await Assert.That(result.Signal).IsNull();
    }

    // ---- FireAndForget always false/null ----

    [Test]
    public async Task FireAndForget_Result_Has_Canceled_False_And_Signal_Null()
    {
        // FireAndForget ignores injected values and hard-codes canceled:false, signal:null (TK004).
        CountingExternalProcessFactory factory = new()
        {
            DefaultCanceled = true,
            DefaultSignal = PosixSignal.SIGTERM
        };
        ProcessInvocationPipeline pipeline = new(factory);
        InvocationContext ctx = new(_configuration, ProcessExitConfiguration.CreateGraceful(),
            InvocationMode.FireAndForget, CancellationToken.None);

        ProcessResult result = await pipeline.InvokeAsync<ProcessResult>(ctx);

        await Assert.That(result.Canceled).IsFalse();
        await Assert.That(result.Signal).IsNull();
    }

    // ---- OS-gated Unix integration ----

    [Test]
    public async Task Unix_SignalTrappingHelper_KilledBySigterm_ReportsSignal()
    {
        // Windows has no POSIX signals; Signal is null by design on Windows
        // (the property is supported on all platforms and simply returns null there).
        if (OperatingSystem.IsWindows())
            return;

        string markerPath = Path.Combine(Path.GetTempPath(),
            $"cliinvoke-canceled-signal-{Guid.NewGuid():N}.marker");

        ProcessWrapper process = ProcessTestHelper.CreateSignalTrappingProcess(markerPath, sleepSeconds: 5);
        process.Start();

        try
        {
            await ProcessTestHelper.WaitForConditionAsync(
                () => process.HasStarted && !process.HasExited,
                TimeSpan.FromSeconds(5),
                failureMessage: "Process did not start within 5s");

            // Process.Kill() terminates a Unix process with SIGKILL (exit code 137), not
            // SIGTERM (exit code 143), so send the expected signal explicitly.
            ProcessTestHelper.SendTerminationSignal(process.Id);

            await process.WaitForExitAsync(CancellationToken.None);

            await Assert.That(process.HasExited).IsTrue();
            await Assert.That(process.ExitCode).IsEqualTo(143);
            await Assert.That(process.Signal).IsEqualTo(PosixSignal.SIGTERM);

            // Externally killed -> not library-canceled.
            await Assert.That(process.Canceled).IsFalse();
        }
        finally
        {
            if (!process.HasExited)
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {
                    process.Kill();
                }
            }

            process.Dispose();

            if (File.Exists(markerPath))
                File.Delete(markerPath);
        }
    }
}
