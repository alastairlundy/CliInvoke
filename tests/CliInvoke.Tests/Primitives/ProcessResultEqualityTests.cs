namespace CliInvoke.Tests.Primitives;

using System.Runtime.InteropServices;

public class ProcessResultEqualityTests
{
    private static ProcessResult MakeBase()
        => new("foo.exe", 0, 1, new DateTime(2026, 1, 1, 0, 0, 0), new DateTime(2026, 1, 1, 0, 0, 1),
            canceled: false, signal: null);

    private static BufferedProcessResult MakeBuffered()
        => new("foo.exe", 0, 1, "out", "err", new DateTime(2026, 1, 1, 0, 0, 0), new DateTime(2026, 1, 1, 0, 0, 1),
            canceled: false, signal: null);

    [Test]
    public async Task ProcessResult_EqualInstances_AreSymmetric()
    {
        ProcessResult a = MakeBase();
        ProcessResult b = MakeBase();

        await Assert.That(a.Equals(b)).IsEqualTo(b.Equals(a));
        await Assert.That(a.Equals(b)).IsTrue();
    }

    [Test]
    public async Task BufferedProcessResult_EqualInstances_AreSymmetric()
    {
        BufferedProcessResult a = MakeBuffered();
        BufferedProcessResult b = MakeBuffered();

        await Assert.That(a.Equals(b)).IsEqualTo(b.Equals(a));
        await Assert.That(a.Equals(b)).IsTrue();
    }

    [Test]
    public async Task PipedProcessResult_EqualInstances_AreSymmetric()
    {
        // PipedProcessResult compares its streams by reference equality, so equal
        // instances must share the same stream objects. PipedProcessResult owns the
        // streams; dispose them after the assertion (disposing twice is a no-op).
        MemoryStream standardOutput = new();
        MemoryStream standardError = new();

        PipedProcessResult a = new("foo.exe", 0, 1, new DateTime(2026, 1, 1, 0, 0, 0),
            new DateTime(2026, 1, 1, 0, 0, 1), standardOutput, standardError, canceled: false, signal: null);
        PipedProcessResult b = new("foo.exe", 0, 1, new DateTime(2026, 1, 1, 0, 0, 0),
            new DateTime(2026, 1, 1, 0, 0, 1), standardOutput, standardError, canceled: false, signal: null);

        try
        {
            await Assert.That(a.Equals(b)).IsEqualTo(b.Equals(a));
            await Assert.That(a.Equals(b)).IsTrue();
        }
        finally
        {
            a.Dispose();
            b.Dispose();
        }
    }

    [Test]
    public async Task BufferedProcessResult_ComparedToBaseProcessResult_IsSymmetric()
    {
        // Different runtime types must agree on equality in both directions
        // (both false), satisfying the Object.Equals symmetry contract.
        BufferedProcessResult buffered = MakeBuffered();
        ProcessResult baseResult = MakeBase();

        await Assert.That(buffered.Equals(baseResult)).IsEqualTo(baseResult.Equals(buffered));
        await Assert.That(buffered.Equals(baseResult)).IsFalse();
    }

    [Test]
    public async Task PipedProcessResult_ComparedToBaseProcessResult_IsSymmetric()
    {
        PipedProcessResult piped = new("foo.exe", 0, 1, new DateTime(2026, 1, 1, 0, 0, 0),
            new DateTime(2026, 1, 1, 0, 0, 1), new MemoryStream(), new MemoryStream(), canceled: false, signal: null);
        ProcessResult baseResult = MakeBase();

        try
        {
            await Assert.That(piped.Equals(baseResult)).IsEqualTo(baseResult.Equals(piped));
            await Assert.That(piped.Equals(baseResult)).IsFalse();
        }
        finally
        {
            piped.Dispose();
        }
    }
}

[Test]
public async Task ProcessResult_CanceledDifference_AffectsEquality()
{
    ProcessResult notCanceled = new("foo.exe", 0, 1, new DateTime(2026, 1, 1, 0, 0, 0),
        new DateTime(2026, 1, 1, 0, 0, 1), canceled: false, signal: null);
    ProcessResult canceled = new("foo.exe", 0, 1, new DateTime(2026, 1, 1, 0, 0, 0),
        new DateTime(2026, 1, 1, 0, 0, 1), canceled: true, signal: null);

    await Assert.That(notCanceled.Equals(canceled)).IsFalse();
    await Assert.That(canceled.Equals(notCanceled)).IsFalse();
    await Assert.That(notCanceled.GetHashCode()).IsNotEqualTo(canceled.GetHashCode());
}

[Test]
public async Task ProcessResult_SignalDifference_AffectsEquality()
{
    ProcessResult noSignal = new("foo.exe", 0, 1, new DateTime(2026, 1, 1, 0, 0, 0),
        new DateTime(2026, 1, 1, 0, 0, 1), canceled: false, signal: null);

#pragma warning disable CA1416
    ProcessResult signaled = new("foo.exe", 0, 1, new DateTime(2026, 1, 1, 0, 0, 0),
        new DateTime(2026, 1, 1, 0, 0, 1), canceled: false, signal: PosixSignal.SIGTERM);
#pragma warning restore CA1416

    await Assert.That(noSignal.Equals(signaled)).IsFalse();
    await Assert.That(signaled.Equals(noSignal)).IsFalse();
    await Assert.That(noSignal.GetHashCode()).IsNotEqualTo(signaled.GetHashCode());
}
