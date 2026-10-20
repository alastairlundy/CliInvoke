using System.Runtime.Versioning;

using CliInvoke.Processes.Internal.ControlAdapters;
using TUnit.Core.Exceptions;

namespace CliInvoke.Tests.Processes;

public class ControlAdapterTests
{
    [Test]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    public async Task UnixAdapter_RequireRunningAsAdmin_ThrowsPlatformNotSupportedException()
    {
        UnixProcessControlAdapter adapter = new UnixProcessControlAdapter();

        using Process dummyProcess = new Process();
        dummyProcess.StartInfo = new ProcessStartInfo("echo");

        await Assert.That(() => adapter.RequireRunningAsAdmin(dummyProcess))
            .Throws<PlatformNotSupportedException>();
    }

    [Test]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    public async Task UnixAdapter_SetUserCredential_NonNull_ThrowsPlatformNotSupportedException()
    {
        UnixProcessControlAdapter adapter = new UnixProcessControlAdapter();

        using Process dummyProcess = new Process();
        dummyProcess.StartInfo = new ProcessStartInfo("echo");

        UserCredential credential = new UserCredential(null, "testuser", null, null);

        await Assert.That(() => adapter.SetUserCredential(dummyProcess, credential))
            .Throws<PlatformNotSupportedException>();
    }

    [Test]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    public async Task UnixAdapter_SetUserCredential_Null_DoesNotThrow()
    {
        UnixProcessControlAdapter adapter = new UnixProcessControlAdapter();

        using Process dummyProcess = new Process();
        dummyProcess.StartInfo = new ProcessStartInfo("echo");

        await Assert.That(() => adapter.SetUserCredential(dummyProcess, null!))
            .ThrowsNothing();
    }

    [Test]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    public async Task UnixAdapter_SetUserCredential_NullSentinel_DoesNotThrow()
    {
        UnixProcessControlAdapter adapter = new UnixProcessControlAdapter();

        using Process dummyProcess = new Process();
        dummyProcess.StartInfo = new ProcessStartInfo("echo");

        await Assert.That(() => adapter.SetUserCredential(dummyProcess, UserCredential.Null))
            .ThrowsNothing();
    }

    [Test]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    public async Task UnixAdapter_SetUserCredential_EmptyCredential_DoesNotThrow()
    {
        UnixProcessControlAdapter adapter = new UnixProcessControlAdapter();

        using Process dummyProcess = new Process();
        dummyProcess.StartInfo = new ProcessStartInfo("echo");

        await Assert.That(() => adapter.SetUserCredential(dummyProcess, new UserCredential()))
            .ThrowsNothing();
    }

    [Test]
    public async Task WindowsAdapter_SetUserCredential_Null_DoesNotThrow()
    {
        if (!OperatingSystem.IsWindows())
            throw new SkipTestException("WindowsProcessControlAdapter is Windows-only.");

        WindowsProcessControlAdapter adapter = new WindowsProcessControlAdapter();

        using Process dummyProcess = new Process();
        dummyProcess.StartInfo = new ProcessStartInfo("echo");

#pragma warning disable CA1416
        await Assert.That(() => adapter.SetUserCredential(dummyProcess, null!))
            .ThrowsNothing();
#pragma warning restore CA1416
    }

    [Test]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    public async Task UnixAdapter_SuspendAndResumeSignalNumbers_MatchPlatformConvention()
    {
        // Linux (and Android, which shares Linux's numbering) use SIGSTOP = 19 / SIGCONT = 18;
        // the BSD family (macOS, Mac Catalyst, FreeBSD) uses SIGSTOP = 17 / SIGCONT = 19.
        // Hardcoding the Linux numbers inverted suspend and resume on macOS/FreeBSD.
        if (UnixProcessControlAdapter.UsesBsdSignalNumbers())
        {
            await Assert.That(UnixProcessControlAdapter.GetStopSignalNumber()).IsEqualTo(17);
            await Assert.That(UnixProcessControlAdapter.GetContinueSignalNumber()).IsEqualTo(19);
        }
        else
        {
            await Assert.That(UnixProcessControlAdapter.GetStopSignalNumber()).IsEqualTo(19);
            await Assert.That(UnixProcessControlAdapter.GetContinueSignalNumber()).IsEqualTo(18);
        }
    }

    [Test]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    public async Task UnixAdapter_GetTerminatingSignal_MapsPlatformSpecificSignalNumber()
    {
        if (OperatingSystem.IsWindows())
            throw new SkipTestException(
                "The Unix adapter's signal table is only populated on Unix-like platforms; " +
                "Windows routes through WindowsProcessControlAdapter instead.");

        UnixProcessControlAdapter adapter = new UnixProcessControlAdapter();

        // The 128 + signal number exit-code convention must map to the correct
        // PosixSignal member using the platform's numbering, not Linux's.
        PosixSignal? terminatingSignal = adapter.GetTerminatingSignal(
            128 + UnixProcessControlAdapter.GetContinueSignalNumber());

        await Assert.That(terminatingSignal).IsEqualTo(PosixSignal.SIGCONT);
    }

    [Test]
    public async Task UnixAdapter_MapNativeSignalNumber_ConvertsRawNumbersToMembers()
    {
        // This is the mapping the net11.0 ProcessWrapper.Signal path applies to the raw
        // positive native terminating-signal number the runtime delivers (WTERMSIG cast
        // into PosixSignal) before the SIGKILL and undefined-value filters. Signals
        // 1/2/3/15 share the same numbers across the Linux and BSD layouts, so these
        // assertions hold on every platform the table's first entries are populated on.
        UnixProcessControlAdapter adapter = new UnixProcessControlAdapter();

        // SIGTERM (15) raw -> genuine member; this is what the SIGTERM integration test
        // (Unix_SignalTrappingHelper_KilledBySigterm_ReportsSignal) asserts end to end.
        await Assert.That(adapter.MapNativeSignalNumber(15)).IsEqualTo(PosixSignal.SIGTERM);
        await Assert.That(adapter.MapNativeSignalNumber(2)).IsEqualTo(PosixSignal.SIGINT);

        // SIGKILL (9) deliberately has no table entry: Signal must stay null and callers
        // distinguish library-initiated forceful kills via ProcessResult.Canceled.
        await Assert.That(adapter.MapNativeSignalNumber(9)).IsNull();

        // Signals the BCL PosixSignal enum does not model (e.g. SIGUSR1) are unknown
        // signals and resolve to null, matching the undefined-value filter.
        await Assert.That(adapter.MapNativeSignalNumber(10)).IsNull();
    }
}
