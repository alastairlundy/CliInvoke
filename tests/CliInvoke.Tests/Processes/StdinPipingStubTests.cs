/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using TUnit.Assertions.Enums;

namespace CliInvoke.Tests.Processes;

/// <summary>
///     Deterministic stub matrix for the stdin-piping contract (ledger T008): the child
///     process, its kill machinery, and all process timing are stubbed, so close-on-every
///     -exit-path, copy-before-close ordering, and the join fault rules are asserted with
///     no real processes and no wall-clock or kill-timing dependence. Real-executable
///     end-to-end proofs are covered separately (ticket 004).
/// </summary>
public class StdinPipingStubTests : IDisposable
{
    private readonly CountingExternalProcessFactory _factory = new();
    private readonly string _targetFilePath = ProcessTestHelper.GetTargetFilePath();

    /// <summary>
    ///     Disposes the stub factory, resetting its counters and piping log.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private ProcessConfiguration CreateStdinConfiguration(Stream source)
        => new(_targetFilePath, string.Empty)
        {
            RedirectStandardInput = true,
            StandardInput = new StreamWriter(source)
        };

    private static InvocationContext CreateContext(ProcessConfiguration configuration,
        InvocationMode mode, CancellationToken cancellationToken = default)
        => new(configuration, ProcessExitConfiguration.CreateGraceful(), mode, cancellationToken);

    /// <summary>
    ///     Success exit: the copy completes, the write end then closes, and the caller's
    ///     source stream is left untouched (close per D003; ordering per T008).
    /// </summary>
    [Test]
    public async Task Raw_SuccessfulCopy_ClosesWriteEndAfterCopy_LeavesSourceUntouched()
    {
        InjectableStdinSourceStream source = new("stdin payload");
        ProcessInvocationPipeline pipeline = new(_factory);

        ProcessResult result = await pipeline.InvokeAsync<ProcessResult>(
            CreateContext(CreateStdinConfiguration(source), InvocationMode.Raw));

        await Assert.That(_factory.StdinPiping.Events).IsEquivalentTo(
            new[]
            {
                StdinPipingEvent.CopyStarted,
                StdinPipingEvent.CopyCompleted,
                StdinPipingEvent.WriteEndClosed
            },
            CollectionOrdering.Matching);
        await Assert.That(_factory.StdinPiping.WriteEnd.CanWrite).IsFalse();
        await Assert.That(source.CanRead).IsTrue();
        await Assert.That(result.Canceled).IsFalse();
    }

    /// <summary>
    ///     Cancellation exit: the token cancels the copy mid-flight; the write end still
    ///     closes in the finally and the caller's source stream stays untouched (D003).
    /// </summary>
    [Test]
    public async Task Raw_CancellationMidCopy_StillClosesWriteEnd_LeavesSourceUntouched()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        InjectableStdinSourceStream source = new("stdin payload")
        {
            OnFirstRead = cancellationTokenSource.Cancel
        };
        ProcessInvocationPipeline pipeline = new(_factory);
        InvocationContext context = CreateContext(CreateStdinConfiguration(source),
            InvocationMode.Raw, cancellationTokenSource.Token);

        await Assert.That(async () => await pipeline.InvokeAsync<ProcessResult>(context))
            .Throws<OperationCanceledException>();

        await Assert.That(_factory.StdinPiping.Events).IsEquivalentTo(
            new[]
            {
                StdinPipingEvent.CopyStarted,
                StdinPipingEvent.CopyFaulted,
                StdinPipingEvent.WriteEndClosed
            },
            CollectionOrdering.Matching);
        await Assert.That(_factory.StdinPiping.WriteEnd.CanWrite).IsFalse();
        await Assert.That(source.CanRead).IsTrue();
    }

    /// <summary>
    ///     Kill exit on the StartAsync join: the kill-induced copy fault is absorbed rather
    ///     than thrown, and the write end still closes (absorb per T006; close per D003).
    /// </summary>
    [Test]
    public async Task Raw_KillInducedCopyFault_IsAbsorbedAtStartAsyncJoin_ClosesWriteEnd()
    {
        // Canceled models the wrapper already having been killed by the library's own
        // cancellation machinery; the armed write end models the pipe that kill tears down.
        _factory.DefaultCanceled = true;
        _factory.StdinPiping.ArmBrokenPipe = true;
        InjectableStdinSourceStream source = new("stdin payload");
        ProcessInvocationPipeline pipeline = new(_factory);

        ProcessResult result = await pipeline.InvokeAsync<ProcessResult>(
            CreateContext(CreateStdinConfiguration(source), InvocationMode.Raw));

        await Assert.That(_factory.StdinPiping.Events).IsEquivalentTo(
            new[]
            {
                StdinPipingEvent.CopyStarted,
                StdinPipingEvent.CopyFaulted,
                StdinPipingEvent.WriteEndClosed
            },
            CollectionOrdering.Matching);
        await Assert.That(_factory.StdinPiping.WriteEnd.CanWrite).IsFalse();
        await Assert.That(source.CanRead).IsTrue();
        await Assert.That(result.Canceled).IsTrue();
    }

    /// <summary>
    ///     Kill exit on the CaptureBufferedResultAsync join: same absorption with the
    ///     copy, wait and drain joined concurrently (absorb per T006; close per D003).
    /// </summary>
    [Test]
    public async Task Buffered_KillInducedCopyFault_IsAbsorbedAtCaptureJoin_ClosesWriteEnd()
    {
        _factory.DefaultCanceled = true;
        _factory.StdinPiping.ArmBrokenPipe = true;
        InjectableStdinSourceStream source = new("stdin payload");
        ProcessInvocationPipeline pipeline = new(_factory);

        BufferedProcessResult result =
            await pipeline.InvokeAsync<BufferedProcessResult>(
                CreateContext(CreateStdinConfiguration(source), InvocationMode.Buffered));

        await Assert.That(_factory.StdinPiping.Events).IsEquivalentTo(
            new[]
            {
                StdinPipingEvent.CopyStarted,
                StdinPipingEvent.CopyFaulted,
                StdinPipingEvent.WriteEndClosed
            },
            CollectionOrdering.Matching);
        await Assert.That(_factory.StdinPiping.WriteEnd.CanWrite).IsFalse();
        await Assert.That(source.CanRead).IsTrue();
        await Assert.That(result.Canceled).IsTrue();
    }

    /// <summary>
    ///     Genuine source-stream read error: it propagates out of the capture join instead
    ///     of being swallowed, after the write end has closed (propagate per T006;
    ///     close per D003).
    /// </summary>
    [Test]
    public async Task Buffered_SourceReadError_PropagatesAtCaptureJoin_AfterClosingWriteEnd()
    {
        InjectableStdinSourceStream source = new("stdin payload")
        {
            FaultAfterFirstRead = new IOException("genuine source-stream read failure")
        };
        ProcessInvocationPipeline pipeline = new(_factory);
        InvocationContext context = CreateContext(CreateStdinConfiguration(source),
            InvocationMode.Buffered);

        await Assert.That(async () => await pipeline.InvokeAsync<BufferedProcessResult>(context))
            .Throws<IOException>();

        await Assert.That(_factory.StdinPiping.Events).IsEquivalentTo(
            new[]
            {
                StdinPipingEvent.CopyStarted,
                StdinPipingEvent.CopyFaulted,
                StdinPipingEvent.WriteEndClosed
            },
            CollectionOrdering.Matching);
        await Assert.That(_factory.StdinPiping.WriteEnd.CanWrite).IsFalse();
        await Assert.That(source.CanRead).IsTrue();
    }
}
