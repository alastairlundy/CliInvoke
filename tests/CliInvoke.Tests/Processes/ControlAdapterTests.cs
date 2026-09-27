using System.Runtime.Versioning;
using CliInvoke.Processes.Internal.ControlAdapters;

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
        UnixProcessControlAdapter adapter = new UnixProcessControlAdapter();

        // The 128 + signal number exit-code convention must map to the correct
        // PosixSignal member using the platform's numbering, not Linux's.
        PosixSignal? terminatingSignal = adapter.GetTerminatingSignal(
            128 + UnixProcessControlAdapter.GetContinueSignalNumber());

        await Assert.That(terminatingSignal).IsEqualTo(PosixSignal.SIGCONT);
    }
}
